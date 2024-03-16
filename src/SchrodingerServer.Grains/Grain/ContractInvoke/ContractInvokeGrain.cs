using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using SchrodingerServer.Common;
using SchrodingerServer.Common.ApplicationHandler;
using SchrodingerServer.Grains.Grain.ApplicationHandler;
using SchrodingerServer.Grains.State.ContractInvoke;
using Volo.Abp.ObjectMapping;

namespace SchrodingerServer.Grains.Grain.ContractInvoke;

public class ContractInvokeGrain : Grain<ContractInvokeState>, IContractInvokeGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly IOptionsMonitor<ChainOptions> _chainOptionsMonitor;
    private readonly ILogger<ContractInvokeGrain> _logger;
    private readonly IContractProvider _contractProvider;
    private readonly IBlockchainClientFactory<AElfClient> _blockchainClientFactory;

    public ContractInvokeGrain(IObjectMapper objectMapper, ILogger<ContractInvokeGrain> logger,
        IBlockchainClientFactory<AElfClient> blockchainClientFactory, IContractProvider contractProvider, 
        IOptionsMonitor<ChainOptions> chainOptionsMonitor)
    {
        _objectMapper = objectMapper;
        _logger = logger;
        _blockchainClientFactory = blockchainClientFactory;
        _contractProvider = contractProvider;
        _chainOptionsMonitor = chainOptionsMonitor;
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }

    public async Task<GrainResultDto<ContractInvokeGrainDto>> CreateAsync(ContractInvokeGrainDto input)
    {
        State = _objectMapper.Map<ContractInvokeGrainDto, ContractInvokeState>(input);
        if (State.Id.IsNullOrEmpty())
        {
            State.Id = input.BizId;
        }
        State.Status = ContractInvokeStatus.ToBeCreated.ToString();
        State.CreateTime = DateTime.UtcNow;
        State.UpdateTime = DateTime.UtcNow;

        await WriteStateAsync();
        
        _logger.LogInformation(
            "CreateAsync Contract bizId {bizId} created.", State.BizId);
        
        return OfContractInvokeGrainResultDto(true);
     }

    public async Task<GrainResultDto<ContractInvokeGrainDto>> ExecuteJobAsync(ContractInvokeGrainDto input)
    {
        State = _objectMapper.Map<ContractInvokeGrainDto, ContractInvokeState>(input);

        var status = EnumConverter.ConvertToEnum<ContractInvokeStatus>(State.Status);

        try
        {
            switch (status)
            {
                case ContractInvokeStatus.ToBeCreated:
                    await HandleCreatedAsync();
                    break;
                case ContractInvokeStatus.Pending:
                    await HandlePendingAsync();
                    break;
                case ContractInvokeStatus.Failed:
                    await HandleFailedAsync();
                    break;
            }

            return OfContractInvokeGrainResultDto(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during job execution and will be retried bizId:{bizId} txHash: {TxHash}",
                State.BizId, State.TransactionId);
            return OfContractInvokeGrainResultDto(false);
        }
    }

    private async Task HandleCreatedAsync()
    {
        //To Generate RawTransaction and Send Transaction
        if (!_chainOptionsMonitor.CurrentValue.ChainInfos.TryGetValue(State.ChainId, out var chainInfo))
        {
            _logger.LogError("ChainOptions chainId:{chainId} has no chain info.", State.ChainId);
            return;
        }
        
        var client = _blockchainClientFactory.GetClient(State.ChainId);
        
        var txResult = await SendTransactionAsync(State.ChainId, await GenerateRawTransaction(State.ContractMethod,
            State.Param, State.ChainId, State.ContractAddress));
        
        var oriStatus = State.Status;
        State.Sender = client.GetAddressFromPrivateKey(chainInfo.PrivateKey);
        State.TransactionId = txResult.TransactionId;
        State.Status = ContractInvokeStatus.Pending.ToString();
        
        _logger.LogInformation(
            "HandleCreatedAsync Contract bizId {bizId} txHash:{txHash} invoke status {oriStatus} to {status}",
            State.BizId, State.TransactionId, oriStatus, State.Status);
        
        await WriteStateAsync();
    }

    private async Task HandlePendingAsync()
    {
        //To Get Transaction Result
        if (State.TransactionId.IsNullOrEmpty())
        {
            await HandleFailedAsync();
            return;
        }

        var txResult = await GetTxResultAsync(State.ChainId, State.TransactionId);

        if (txResult.Status != TransactionState.Mined)
        {
            await TransactionFailedAsync(txResult);
            return;
        }

        var oriStatus = State.Status;
        State.BlockHeight = txResult.BlockNumber;
        State.Status = ContractInvokeStatus.Success.ToString();

        _logger.LogInformation(
            "HandlePendingAsync Contract bizId {bizId} txHash:{txHash} invoke status {oriStatus} to {status}",
            State.BizId, State.TransactionId, oriStatus, State.Status);
        await WriteStateAsync();
    }

    private async Task HandleFailedAsync()
    {
        //To retry and send HandleCreatedAsync
        State.Status = ContractInvokeStatus.ToBeCreated.ToString();
        State.RetryCount += 1;
        _logger.LogInformation(
            "HandleFailedAsync Contract bizId {bizId} txHash:{txHash} invoke status to {status}, retryCount:{retryCount}",
            State.BizId, State.TransactionId, State.Status, State.RetryCount);
        await WriteStateAsync();
    }
    
    private async Task TransactionFailedAsync(TransactionResultDto txResult)
    {
        if (txResult.Status is TransactionState.Mined or TransactionState.Pending)
        {
            return;
        }
        var oriStatus = State.Status;
        State.Status = ContractInvokeStatus.Failed.ToString();
        State.TransactionStatus = txResult.Status;
        // When Transaction status is not mined or pending, Transaction is judged to be failed.
        State.Message = $"Transaction failed, status: {State.Status}. error: {txResult.Error}";

        _logger.LogWarning(
            "TransactionFailedAsync Contract bizId {bizId} txHash:{txHash} invoke status {oriStatus} to {status}",
            State.BizId, State.TransactionId, oriStatus, State.Status);
        
        await WriteStateAsync();
    }
    
    private async Task<SendTransactionOutput> SendTransactionAsync(string chainId, string rawTx)
    {
        var client = _blockchainClientFactory.GetClient(chainId);
        return await client.SendTransactionAsync(new SendTransactionInput() { RawTransaction = rawTx });
    }

    private async Task<string> GenerateRawTransaction(string methodName, string param, string chainId,
        string contractAddress)
    {
        if (!_chainOptionsMonitor.CurrentValue.ChainInfos.TryGetValue(chainId, out var chainInfo)) return "";

        var client = _blockchainClientFactory.GetClient(chainId);
        var status = await client.GetChainStatusAsync();
        var height = status.BestChainHeight;
        var blockHash = status.BestChainHash;

        var from = client.GetAddressFromPrivateKey(chainInfo.PrivateKey);
        var transaction = new Transaction
        {
            From = Address.FromBase58(from),
            To = Address.FromBase58(contractAddress),
            MethodName = methodName,
            Params = ByteString.FromBase64(param),
            RefBlockNumber = height,
            RefBlockPrefix = ByteString.CopyFrom(Hash.LoadFromHex(blockHash).Value.Take(4).ToArray())
        };
        
        return client.SignTransaction(chainInfo.PrivateKey, transaction).ToByteArray().ToHex();
    }
    
    private async Task<TransactionResultDto> GetTxResultAsync(string chainId, string txId)
    {
        var client = _blockchainClientFactory.GetClient(chainId);
        return await client.GetTransactionResultAsync(txId);
    }

    private GrainResultDto<ContractInvokeGrainDto> OfContractInvokeGrainResultDto(bool success)
    {
        return new GrainResultDto<ContractInvokeGrainDto>()
        {
            Data = _objectMapper.Map<ContractInvokeState, ContractInvokeGrainDto>(State),
            Success = success
        };
    }
}