using AElf;
using AElf.Client.Dto;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using SchrodingerServer.Grains.Grain.ApplicationHandler;
using SchrodingerServer.Grains.Grain.Provider;
using SchrodingerServer.Grains.State.Sync;
using Volo.Abp.ObjectMapping;

namespace SchrodingerServer.Grains.Grain.Synchronize;

public class SyncGrain : Grain<SyncState>, ISyncGrain
{
    private readonly string _targetChainId;
    private readonly string _sourceChainId;
    private readonly ILogger<SyncGrain> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IContractProvider _contractProvider;
    private readonly IOptionsMonitor<SyncTokenOptions> _syncOptions;

    public SyncGrain(ILogger<SyncGrain> logger, IContractProvider contractProvider,
        IOptionsMonitor<SyncTokenOptions> syncOptions, IObjectMapper objectMapper)
    {
        _logger = logger;
        _syncOptions = syncOptions;
        _objectMapper = objectMapper;
        _contractProvider = contractProvider;
        _targetChainId = _syncOptions.CurrentValue.TargetChainId;
        _sourceChainId = _syncOptions.CurrentValue.SourceChainId;
    }

    public async Task<GrainResultDto<SyncGrainDto>> ExecuteJobAsync(SyncJobGrainDto input)
    {
        if (string.IsNullOrEmpty(State.Id))
        {
            State.Id = input.Id;
            State.TransactionId = input.Id;
        }

        try
        {
            switch (State.Status)
            {
                case null:
                    await HandleTokenCreatingAsync();
                    break;
                case SyncJobStatus.TokenValidating:
                    await HandleTokenValidatingAsync();
                    break;
                case SyncJobStatus.WaitingIndexing:
                    await HandleMainChainIndexSideChainAsync();
                    break;
                case SyncJobStatus.WaitingSideIndexing:
                    await HandleWaitingIndexingAsync();
                    break;
                case SyncJobStatus.CrossChainTokenCreating:
                    await HandleCrossChainTokenCreatingAsync();
                    break;
                case SyncJobStatus.CrossChainTokenCreated:
                    break;
            }

            return new GrainResultDto<SyncGrainDto>
            {
                Data = _objectMapper.Map<SyncState, SyncGrainDto>(State),
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "");
            return new GrainResultDto<SyncGrainDto>
            {
                Success = false,
                Message = null
            };
        }
    }

    # region Token crossChain

    private async Task HandleTokenCreatingAsync()
    {
        var tokenSymbol = _contractProvider.ParseLogEvents<TokenCreated>(
            await _contractProvider.GetTxResultAsync(_sourceChainId, State.TransactionId)).Symbol;
        var tokenInfo = await _contractProvider.GetTokenInfoAsync(_sourceChainId, tokenSymbol);

        // check token is cross chain created
        if ((await _contractProvider.GetTokenInfoAsync(_targetChainId, tokenSymbol)).Symbol == tokenSymbol)
        {
            State.Status = SyncJobStatus.CrossChainTokenCreated;
            return;
        }

        State.Symbol = tokenInfo.Symbol;
        (State.ValidateTokenTx, State.ValidateTokenTxId) =
            await _contractProvider.SendValidateTokenExist(_sourceChainId, tokenInfo);
        State.Status = SyncJobStatus.TokenValidating;
        // State.ValidateTokenTx = await _contractProvider.GenerateRawTransactionAsync(MethodName.ValidateTokenInfoExists,
        //     new ValidateTokenInfoExistsInput
        //     {
        //         Symbol = tokenInfo.Symbol,
        //         TokenName = tokenInfo.TokenName,
        //         Decimals = tokenInfo.Decimals,
        //         IsBurnable = tokenInfo.IsBurnable,
        //         IssueChainId = tokenInfo.IssueChainId,
        //         Issuer = new Address { Value = tokenInfo.Issuer.Value },
        //         TotalSupply = tokenInfo.TotalSupply,
        //         Owner = tokenInfo.Owner,
        //         ExternalInfo = { tokenInfo.ExternalInfo.Value }
        //     }, _sourceChainId, tokenAddress);
        // State.ValidateTokenTxId =
        //     (await _contractProvider.SendTransactionAsync(_sourceChainId, State.ValidateTokenTx)).TransactionId;

        _logger.LogInformation("TransactionId {txId} update status to {status} in HandleTokenCreatingAsync.",
            State.TransactionId, State.Status);

        await WriteStateAsync();
    }

    private async Task HandleTokenValidatingAsync()
    {
        var txResult = await _contractProvider.GetTxResultAsync(_sourceChainId, State.ValidateTokenTxId);
        if (!await CheckTxStatusAsync(txResult)) return;
        if (txResult.BlockNumber == 0) return;

        State.ValidateTokenHeight = txResult.BlockNumber;
        State.Status = SyncJobStatus.WaitingIndexing;

        _logger.LogInformation("TxHash id {txHash} update status to {status} in HandleTokenValidatingAsync.",
            State.TransactionId, State.Status);

        await WriteStateAsync();
    }


    private async Task HandleMainChainIndexSideChainAsync()
    {
        // Check MainChain Index SideChain
        // First, the main chain must index to the transaction height of the side chain.
        var indexHeight = await _contractProvider.GetSideChainIndexHeightAsync(_targetChainId, _sourceChainId);
        if (indexHeight < State.ValidateTokenHeight)
        {
            _logger.LogInformation(
                "[Synchronize] Block is not recorded, now index height {indexHeight}, expected height:{ValidateHeight}",
                indexHeight, State.ValidateTokenHeight);
            return;
        }

        // Then record the number of main chain heights. Only the side chain bidirectional index can continue to cross the chain.
        State.MainChainIndexHeight = await _contractProvider.GetBlockLatestHeightAsync(_targetChainId);
        State.Status = SyncJobStatus.WaitingSideIndexing;

        _logger.LogInformation("TxHash id {txHash} update status to {status} in HandleMainChainIndexSideChainAsync.",
            State.TransactionId, State.Status);

        await WriteStateAsync();
    }

    private async Task HandleWaitingIndexingAsync()
    {
        var indexHeight = await _contractProvider.GetIndexHeightAsync(_sourceChainId);
        if (indexHeight < State.MainChainIndexHeight)
        {
            _logger.LogInformation(
                "[Synchronize] The height of the main chain has not been indexed yet, now index height {indexHeight}, expected height:{mainHeight}",
                indexHeight, State.MainChainIndexHeight);
            return;
        }

        var merklePath = await _contractProvider.GetMerklePathAsync(_sourceChainId, State.ValidateTokenTxId);
        if (merklePath == null) return;

        var crossChainMerkleProof =
            await _contractProvider.GetCrossChainMerkleProofContextAsync(_sourceChainId, State.ValidateTokenHeight);
        var createTokenParams = new CrossChainCreateTokenInput
        {
            FromChainId = ChainHelper.ConvertBase58ToChainId(_sourceChainId),
            ParentChainHeight = crossChainMerkleProof.BoundParentChainHeight,
            TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(State.ValidateTokenTx)),
            MerklePath = new MerklePath()
        };

        foreach (var node in merklePath.MerklePathNodes)
        {
            createTokenParams.MerklePath.MerklePathNodes.Add(new MerklePathNode
            {
                Hash = new Hash { Value = Hash.LoadFromHex(node.Hash).Value },
                IsLeftChildNode = node.IsLeftChildNode
            });
        }

        foreach (var node in crossChainMerkleProof.MerklePathFromParentChain.MerklePathNodes)
        {
            createTokenParams.MerklePath.MerklePathNodes.Add(new MerklePathNode
            {
                Hash = new Hash { Value = node.Hash.Value },
                IsLeftChildNode = node.IsLeftChildNode
            });
        }

        var txId = await _contractProvider.CrossChainCreateToken(_targetChainId, createTokenParams);
        _logger.LogInformation("CrossChainCreateTokenTxId {TxId}", txId);

        State.CrossChainCreateTokenTxId = txId;
        State.Status = SyncJobStatus.CrossChainTokenCreating;

        _logger.LogInformation("TxHash id {txHash} update status to {status} in HandleWaitingIndexingAsync.",
            State.TransactionId, State.Status);

        await WriteStateAsync();
    }

    private async Task HandleCrossChainTokenCreatingAsync()
    {
        var txResult = await _contractProvider.GetTxResultAsync(_targetChainId, State.CrossChainCreateTokenTxId);
        if (!await CheckTxStatusAsync(txResult)) return;

        State.Status = SyncJobStatus.CrossChainTokenCreated;

        _logger.LogInformation("TxHash id {txHash} update status to {status} in HandleCrossChainTokenCreatingAsync.",
            State.TransactionId, State.Status);

        await WriteStateAsync();
    }


    private async Task<bool> CheckTxStatusAsync(TransactionResultDto txResult)
    {
        if (txResult.Status == TransactionState.Mined) return true;

        if (txResult.Status == TransactionState.Pending) return false;

        // When Transaction status is not mined or pending, Transaction is judged to be failed.
        State.Message = $"Transaction failed, status: {State.Status}. error: {txResult.Error}";
        State.Status = SyncJobStatus.Failed;

        await WriteStateAsync();
        _logger.LogWarning("Transaction failed, TxHash id {txHash} update status to {status}.",
            State.TransactionId, State.Status);

        return false;
    }

    #endregion
}