using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using SchrodingerServer.Grains.Grain.ApplicationHandler;
using Volo.Abp.ObjectMapping;
using SchrodingerServer.Grains.State.Faucets;

namespace SchrodingerServer.Grains.Grain.Faucets;

public class FaucetsTransferGrain : Grain<FaucetsState>, IFaucetsGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<FaucetsTransferGrain> _logger;
    private readonly IOptionsMonitor<ChainOptions> _chainOptions;
    private readonly IOptionsMonitor<FaucetsTransferOptions> _faucetsOptions;
    private readonly IBlockchainClientFactory<AElfClient> _blockchainClientFactory;

    public FaucetsTransferGrain(IObjectMapper objectMapper, ILogger<FaucetsTransferGrain> logger,
        IBlockchainClientFactory<AElfClient> blockchainClientFactory, IOptionsMonitor<ChainOptions> chainOptions,
        IOptionsMonitor<FaucetsTransferOptions> faucetsOptions)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _chainOptions = chainOptions;
        _faucetsOptions = faucetsOptions;
        _blockchainClientFactory = blockchainClientFactory;
    }

    public async Task<GrainResultDto<FaucetsGrainDto>> FaucetsTransfer(FaucetsTransferGrainDto input)
    {
        var result = new GrainResultDto<FaucetsGrainDto>();
        try
        {
            var address = input.Address;
            if (string.IsNullOrEmpty(State.Id))
            {
                State.Id = address;
            }

            if (State.Mined)
            {
                _logger.LogWarning(FaucetsTransferMessage.TransferRestrictionsMessage);
                result.Message = FaucetsTransferMessage.TransferRestrictionsMessage;
                result.Success = false;
                return result;
            }

            var chainId = _faucetsOptions.CurrentValue.ChainId;
            if (!string.IsNullOrEmpty(State.TransactionId))
            {
                var txResult = await _blockchainClientFactory.GetClient(chainId)
                    .GetTransactionResultAsync(State.TransactionId);
                switch (txResult.Status)
                {
                    case "PENDING":
                        _logger.LogWarning(FaucetsTransferMessage.TransferPendingMessage);
                        result.Message = FaucetsTransferMessage.TransferPendingMessage;
                        result.Success = false;
                        return result;
                    case "MINED":
                        State.Mined = true;
                        await WriteStateAsync();
                        _logger.LogWarning(FaucetsTransferMessage.TransferRestrictionsMessage);
                        result.Message = FaucetsTransferMessage.TransferRestrictionsMessage;
                        result.Success = false;
                        return result;
                }
            }

            var symbol = _faucetsOptions.CurrentValue.FaucetsTransferSymbol;
            var amount = _faucetsOptions.CurrentValue.FaucetsTransferAmount;

            if (!await CheckFaucetsBalance())
            {
                _logger.LogWarning("There is no balance in the management account!");
                result.Message = FaucetsTransferMessage.SuspendUseMessage;
                result.Success = false;
                return result;
            }

            _logger.LogInformation("Prepare to issue {amount} {symbol} to address {addr}", amount, symbol, address);

            var param = new TransferInput
            {
                Symbol = symbol,
                Amount = ((long)amount).Mul((long)Math.Pow(10, _faucetsOptions.CurrentValue.SymbolDecimal)),
                To = Address.FromBase58(address)
            };
            State.TransactionId = (await SendTransactionAsync(chainId, await GenerateRawTransaction(MethodName.Transfer,
                param, chainId, _chainOptions.CurrentValue.ChainInfos[chainId].TokenContractAddress))).TransactionId;
            State.Address = input.Address;
            State.Symbol = symbol;
            State.Amount = amount;

            await WriteStateAsync();

            result.Data = _objectMapper.Map<FaucetsState, FaucetsGrainDto>(State);
            return result;
        }
        catch (FormatException)
        {
            _logger.LogError("Invalid Address.");
            result.Message = FaucetsTransferMessage.InvalidAddressMessage;
            result.Success = false;
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Faucets transfer failed.");
            result.Message = e.Message;
            result.Success = false;
            return result;
        }
    }

    private async Task<bool> CheckFaucetsBalance()
    {
        var chainId = _faucetsOptions.CurrentValue.ChainId;
        var balance = await CallTransactionAsync<GetBalanceOutput>(chainId, await GenerateRawTransaction(
            MethodName.GetBalance, new GetBalanceInput
            {
                Symbol = _faucetsOptions.CurrentValue.FaucetsTransferSymbol,
                Owner = Address.FromBase58(_faucetsOptions.CurrentValue.ManagerAddress)
            }, chainId, _chainOptions.CurrentValue.ChainInfos[chainId].TokenContractAddress));
        return balance.Balance > Math.Pow(10, _faucetsOptions.CurrentValue.SymbolDecimal);
    }

    private async Task<bool> IsTransferMined(string chainId, string txId)
        => (await _blockchainClientFactory.GetClient(chainId).GetTransactionResultAsync(txId)).Status == "MINED";

    private async Task<T> CallTransactionAsync<T>(string chainId, string rawTx) where T : class, IMessage<T>, new()
    {
        var client = _blockchainClientFactory.GetClient(chainId);
        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto { RawTransaction = rawTx });
        var value = new T();
        value.MergeFrom(ByteArrayHelper.HexStringToByteArray(result));
        return value;
    }

    private async Task<SendTransactionOutput> SendTransactionAsync(string chainId, string rawTx)
        => await _blockchainClientFactory.GetClient(chainId).SendTransactionAsync(new() { RawTransaction = rawTx });

    private async Task<string> GenerateRawTransaction(string methodName, IMessage param, string chainId,
        string contractAddress)
    {
        if (!_chainOptions.CurrentValue.ChainInfos.TryGetValue(chainId, out var chainInfo)) return "";

        var client = _blockchainClientFactory.GetClient(chainId);
        return client.SignTransaction(chainInfo.PrivateKey, await client.GenerateTransactionAsync(
                client.GetAddressFromPrivateKey(chainInfo.PrivateKey), contractAddress, methodName, param))
            .ToByteArray().ToHex();
    }

    private async Task<TransactionResultDto> GetTxResultAsync(string chainId, string txId)
        => await _blockchainClientFactory.GetClient(chainId).GetTransactionResultAsync(txId);
}