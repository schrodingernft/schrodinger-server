using AElf;
using AElf.Client.Dto;
using AElf.Client.Proto;
using AElf.Client.Service;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using SchrodingerServer.Grains.Grain.ApplicationHandler;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.Grains.Grain.Provider;

public interface IContractProvider
{
    public Task<long> GetBlockLatestHeightAsync(string chainId);
    public Task<TransactionResultDto> GetTxResultAsync(string chainId, string transactionId);
    public T ParseLogEvents<T>(TransactionResultDto txResult) where T : class, IMessage<T>, new();
    public Task<T> CallTransactionAsync<T>(string chainId, string rawTx) where T : class, IMessage<T>, new();
    public Task<SendTransactionOutput> SendTransactionAsync(string chainId, string rawTx);
    public Task<MerklePathDto> GetMerklePathAsync(string chainId, string txId);
    public Task<long> GetIndexHeightAsync(string chainId);
    public Task<long> GetSideChainIndexHeightAsync(string chainId, string sourceChainId);
    public Task<CrossChainMerkleProofContext> GetCrossChainMerkleProofContextAsync(string chainId, long blockHeight);
    public Task<TokenInfo> GetTokenInfoAsync(string chainId, string symbol);
    public Task<(string, string)> SendValidateTokenExist(string chainId, TokenInfo tokenInfo);
    public Task<string> CrossChainCreateToken(string chainId, CrossChainCreateTokenInput createTokenParams);

    public Task<string> GenerateRawTransactionAsync(string methodName, IMessage param, string chainId,
        string contractAddress);
}

public class ContractProvider : IContractProvider, ISingletonDependency
{
    private readonly IOptionsMonitor<ChainOptions> _chainOptions;
    private readonly IBlockchainClientFactory<AElfClient> _blockchainClientFactory;

    public ContractProvider(IBlockchainClientFactory<AElfClient> blockchainClientFactory,
        IOptionsMonitor<ChainOptions> chainOptions)
    {
        _blockchainClientFactory = blockchainClientFactory;
        _chainOptions = chainOptions;
    }

    public async Task<long> GetBlockLatestHeightAsync(string chainId)
        => await _blockchainClientFactory.GetClient(chainId).GetBlockHeightAsync();

    public async Task<TransactionResultDto> GetTxResultAsync(string chainId, string transactionId)
        => await _blockchainClientFactory.GetClient(chainId).GetTransactionResultAsync(transactionId);

    public async Task<T> CallTransactionAsync<T>(string chainId, string rawTx) where T : class, IMessage<T>, new()
    {
        var client = _blockchainClientFactory.GetClient(chainId);
        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto { RawTransaction = rawTx });
        var value = new T();
        value.MergeFrom(ByteArrayHelper.HexStringToByteArray(result));
        return value;
    }

    public async Task<SendTransactionOutput> SendTransactionAsync(string chainId, string rawTx)
        => await _blockchainClientFactory.GetClient(chainId)
            .SendTransactionAsync(new SendTransactionInput { RawTransaction = rawTx });


    public async Task<string> GenerateRawTransactionAsync(string methodName, IMessage param, string chainId,
        string contractAddress)
    {
        if (!_chainOptions.CurrentValue.ChainInfos.TryGetValue(chainId, out var chainInfo)) return "";
        var client = _blockchainClientFactory.GetClient(chainId);
        return client.SignTransaction(chainInfo.PrivateKey, await client.GenerateTransactionAsync(
                client.GetAddressFromPrivateKey(chainInfo.PrivateKey), contractAddress, methodName, param))
            .ToByteArray().ToHex();
    }

    public T ParseLogEvents<T>(TransactionResultDto txResult) where T : class, IMessage<T>, new()
    {
        var log = txResult.Logs.FirstOrDefault(l => l.Name == typeof(T).Name);
        var transactionLogEvent = new T();
        if (log == null) return transactionLogEvent;

        var logEvent = new LogEvent
        {
            Indexed = { log.Indexed.Select(ByteString.FromBase64) },
            NonIndexed = ByteString.FromBase64(log.NonIndexed)
        };
        transactionLogEvent.MergeFrom(logEvent.NonIndexed);
        foreach (var indexed in logEvent.Indexed)
        {
            transactionLogEvent.MergeFrom(indexed);
        }

        return transactionLogEvent;
    }

    public async Task<MerklePathDto> GetMerklePathAsync(string chainId, string txId)
        => await _blockchainClientFactory.GetClient(chainId).GetMerklePathByTransactionIdAsync(txId);

    public async Task<long> GetIndexHeightAsync(string chainId)
    {
        var chainInfo = _chainOptions.CurrentValue.ChainInfos[chainId];
        var client = _blockchainClientFactory.GetClient(chainId);

        return Int64Value.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(await client.ExecuteTransactionAsync(
            new ExecuteTransactionDto
            {
                RawTransaction = client.SignTransaction(chainInfo.PrivateKey, await client.GenerateTransactionAsync(
                        client.GetAddressFromPrivateKey(chainInfo.PrivateKey),
                        chainInfo.CrossChainContractAddress, MethodName.GetParentChainHeight, new Empty()))
                    .ToByteArray()
                    .ToHex()
            }))).Value;
    }

    public async Task<long> GetSideChainIndexHeightAsync(string chainId, string sourceChainId)
    {
        var chainInfo = _chainOptions.CurrentValue.ChainInfos[chainId];
        var client = _blockchainClientFactory.GetClient(chainId);

        return Int64Value.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(await client.ExecuteTransactionAsync(
            new ExecuteTransactionDto
            {
                RawTransaction = client.SignTransaction(chainInfo.PrivateKey,
                        await client.GenerateTransactionAsync(client.GetAddressFromPrivateKey(chainInfo.PrivateKey),
                            chainInfo.CrossChainContractAddress, MethodName.GetSideChainHeight,
                            new Int32Value { Value = ChainHelper.ConvertBase58ToChainId(sourceChainId) }))
                    .ToByteArray().ToHex()
            }))).Value;
    }

    public async Task<CrossChainMerkleProofContext> GetCrossChainMerkleProofContextAsync(string chainId,
        long blockHeight)
    {
        var chainInfo = _chainOptions.CurrentValue.ChainInfos[chainId];
        var client = _blockchainClientFactory.GetClient(chainId);

        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = client.SignTransaction(chainInfo.PrivateKey,
                    await client.GenerateTransactionAsync(client.GetAddressFromPrivateKey(chainInfo.PrivateKey),
                        chainInfo.CrossChainContractAddress, MethodName.GetBoundParentChainHeightAndMerklePathByHeight,
                        new Int64Value { Value = blockHeight }))
                .ToByteArray().ToHex()
        });
        return CrossChainMerkleProofContext.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result));
    }

    public async Task<TokenInfo> GetTokenInfoAsync(string chainId, string symbol)
        => await CallTransactionAsync<TokenInfo>(chainId, await GenerateRawTransactionAsync(MethodName.GetTokenInfo,
            new GetTokenInfoInput { Symbol = symbol }, chainId,
            _chainOptions.CurrentValue.ChainInfos[chainId].TokenContractAddress));

    public async Task<(string, string)> SendValidateTokenExist(string chainId, TokenInfo tokenInfo)
    {
        var validateTokenTx = await GenerateRawTransactionAsync(MethodName.ValidateTokenInfoExists,
            new ValidateTokenInfoExistsInput
            {
                Symbol = tokenInfo.Symbol,
                TokenName = tokenInfo.TokenName,
                Decimals = tokenInfo.Decimals,
                IsBurnable = tokenInfo.IsBurnable,
                IssueChainId = tokenInfo.IssueChainId,
                Issuer = new AElf.Types.Address { Value = tokenInfo.Issuer.Value },
                TotalSupply = tokenInfo.TotalSupply,
                Owner = tokenInfo.Owner,
                ExternalInfo = { tokenInfo.ExternalInfo.Value }
            }, chainId, _chainOptions.CurrentValue.ChainInfos[chainId].TokenContractAddress);
        var validateTokenTxId = (await SendTransactionAsync(chainId, validateTokenTx)).TransactionId;

        return (validateTokenTx, validateTokenTxId);
    }

    public async Task<string> CrossChainCreateToken(string chainId, CrossChainCreateTokenInput createTokenParams)
        => (await SendTransactionAsync(chainId,
            await GenerateRawTransactionAsync(MethodName.CrossChainCreateToken, createTokenParams,
                chainId, _chainOptions.CurrentValue.ChainInfos[chainId].TokenContractAddress))).TransactionId;
}