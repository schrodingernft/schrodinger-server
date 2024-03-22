using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Types;
using Google.Protobuf;
using Schrodinger;
using SchrodingerServer.Common;
using Xunit;

namespace SchrodingerServer;

public class ContractTest
{
    [Fact]
    public async Task Test()
    {
        
        var privateKey = "baf36c247544e5ba36c0c8ae73ae5eddd0b60c816f9c871c6dfcd44924a86d97";
        var publicKey = "04726cca6c368ea2b9e0f81ad19db37b8374f0b17078cd20cefa766040235491b72e6920c622d5cfd357dbeb7ebf20bbcb232724eb71c1d31c6f6e1d0c5e4a5412";
        var toAddress = "Ccc5pNs71BMbgDr2ZwpNqtegfkHkBsTJ57HBZ6gw3HNH6pb9S";
        var methodName = "BatchSettle";
        
        var client = new AElfClient("https://tdvw-test-node.aelf.io");
        var status = await client.GetChainStatusAsync();
        var height = status.BestChainHeight;
        var blockHash = status.BestChainHash;
        
        var userPoints = new UserPoints
        {
            UserAddress = Address.FromBase58("2GmpGegBTsjDmoVxu1n4nZvxezyc9GKVQCzWYm193iivncv7GU"),
            UserPoints_ = DecimalHelper.ConvertToLong(123m, 8)
        };
        
        var batchSettleInput = new BatchSettleInput()
        {
            ActionName = "Trade",
            UserPointsList = { userPoints }
        };
        
        var rawTransaction = client.SignTransaction(privateKey, await client.GenerateTransactionAsync(
                client.GetAddressFromPrivateKey(privateKey), toAddress, methodName, batchSettleInput))
            .ToByteArray().ToHex();
        
        
        
        
        var rawTransactionResult = await client.SendTransactionAsync(new SendTransactionInput()
        {
            RawTransaction = rawTransaction
        });
        
        Console.WriteLine(rawTransactionResult);
        
    }
    
    [Fact]
    public async Task Test_T()
    {
        
        var privateKey = "baf36c247544e5ba36c0c8ae73ae5eddd0b60c816f9c871c6dfcd44924a86d97";
        var senderPublicKey = "04726cca6c368ea2b9e0f81ad19db37b8374f0b17078cd20cefa766040235491b72e6920c622d5cfd357dbeb7ebf20bbcb232724eb71c1d31c6f6e1d0c5e4a5412";
        var toAddress = "Ccc5pNs71BMbgDr2ZwpNqtegfkHkBsTJ57HBZ6gw3HNH6pb9S";
        var methodName = "BatchSettle";
        
        var client = new AElfClient("https://tdvw-test-node.aelf.io");
        var status = await client.GetChainStatusAsync();
        var height = status.BestChainHeight;
        var blockHash = status.BestChainHash;
        
        var userPoints = new UserPoints
        {
            UserAddress = Address.FromBase58("2GmpGegBTsjDmoVxu1n4nZvxezyc9GKVQCzWYm193iivncv7GU"),
            UserPoints_ = DecimalHelper.ConvertToLong(123m, 8)
        };
        
        var batchSettleInput = new BatchSettleInput()
        {
            ActionName = "Trade",
            UserPointsList = { userPoints }
        };

        var json = JsonFormatter.Default.Format(batchSettleInput);
        Console.WriteLine(json);

        var str = "CgpTR1JIb2xkaW5nEioKIgogp4KHU8R+7jSr9stCm3aI5wj+IeIzpYuaGRgld/1pXwQQgNiO4W8=";
        // create raw transaction
        var transaction = new Transaction
        {
            From = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(senderPublicKey)),
            To = Address.FromBase58(toAddress),
            MethodName = methodName,
            Params = ByteString.FromBase64(str),
            RefBlockNumber = height,
            RefBlockPrefix = ByteString.CopyFrom(Hash.LoadFromHex(blockHash).Value.Take(4).ToArray())
        };


        var rawTransaction = client.SignTransaction(privateKey, transaction).ToByteArray().ToHex();;
        
        
        
        var rawTransactionResult = await client.SendTransactionAsync(new SendTransactionInput()
        {
            RawTransaction = rawTransaction
        });
        
        Console.WriteLine(rawTransactionResult.TransactionId);
        
    }
    
    [Fact]
    public async Task Test_Reg()
    {
        //"ConditionalExp" : "^(?!.*SGR-1$).*"
        string[] tests = { "HelloSGR-2", "SGRTEST-1234", "TestSGR-1", "SampleText"};
        var pattern = "SGRTEST-(?!1$)[0-9]+";
        foreach (var test in tests)
        {
            var match = Regex.Match(test, pattern);
            Console.WriteLine($"'{test}' does NOT end with 'SGR-1': {match.Success}");
        }

        var pattern2 = "^SGRTEST-1$";
        string[] tests2 = { "HelloSGR-2", "SGR-1234", "SGRTEST-1", "111SGRTEST-1"};

        foreach (var test in tests2)
        {
            var match = Regex.Match(test, pattern2);
            Console.WriteLine($"'{test}' end with 'SGRTEST-1': {match.Success}");
        }

    }
    
    [Fact]
    public void Base64string_Test()
    {
        var base64String = "CgpTR1JIb2xkaW5nEisKIgogAkajTTdgOVW167GHyLJd2EJyuroKNyi86bispUjmttMQ5t2y5aYREisKIgog8tBswciTAGgDYRKKrR/IAvaCLQzEog0iXxip186RlIgQyvnZvuoMEi0KIgogc8MWaFYs0vsR9H5jlukVsGePKLP/PYNdt0RIPuawTRYQ09/69NX92DYSLQoiCiAyihkI9Hcp53WFaHLqTgZv9algUanlYKlZseUn4bigZRDDtL616aiFAxIrCiIKIDxoBDBv4tbJu3XjPDVoSj4C3i9iO/XxNLgFJRBvNUIWEP3U/frBVhIrCiIKIL9pBHyHWXLCTcogEaHjrO9tgBYh6KBgK2Droj3A3B5xEP3U/frBVhIrCiIKIE4Pq2BxyJkaJhK/Q3EBeCOnI2kmvVEpCYv/Ap4OvHUBEPOu2bLTCBIuCiIKIJ9K/Qi+SBVTdjfPOchGZM7fXDl7yliRF8E3jL8qY0+BEPmy+NiRsfn5MxIrCiIKIPxiLjfugU8tCNG67Gcz4ESoeQ+qlw53hHgSlBqvJMHhEP3U/frBVhIrCiIKIBBsD8txpHF2JtcaMWlrfpGRc3lcCDsEpAFt9tLWuOtwENTd+Z+KTg==";

        var byteString = ByteString.FromBase64(base64String);
        
        BatchSettleInput batchSettleInput = BatchSettleInput.Parser.ParseFrom(byteString);

        Console.WriteLine($"batchSettleInput: {batchSettleInput}");
        
        
        var date = getUTCDay();
        var dateStr = date.AddDays(-1).ToString("yyyyMMdd");
        
        Console.WriteLine($"date: {date}");

    }
    
    
    private DateTime getUTCDay()
    {
        DateTime nowUtc = DateTime.UtcNow;
        return new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, 0, 0, 0, DateTimeKind.Utc);
    }
}