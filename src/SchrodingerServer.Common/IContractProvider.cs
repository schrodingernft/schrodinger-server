using AElf.Client.Dto;
using AElf.Types;
using Google.Protobuf;

namespace SchrodingerServer.Common;

public interface IContractProvider
{
    Task<(Hash transactionId, Transaction transaction)> CreateCallTransactionAsync(string chainId,
        string contractName, string methodName, IMessage param);
    
    Task<(Hash transactionId, Transaction transaction)> CreateTransactionAsync(string chainId, string senderPublicKey,
        string contractName, string methodName,
        IMessage param);
    
    Task<(Hash transactionId, Transaction transaction)> SendTransactionAsync(string chainId, string senderPublicKey,
        string toAddress, string methodName,
        string param);
    
    string ContractAddress(string chainId, string contractName);
    
    // Task SendTransactionAsync(string chainId, Transaction transaction);

    Task<T> CallTransactionAsync<T>(string chainId, Transaction transaction) where T : class;

    Task<TransactionResultDto> QueryTransactionResultAsync(string transactionId, string chainId);
}