namespace SchrodingerServer.Grains.Grain.ApplicationHandler;

public class ChainOptions
{
    public int MaxRetryCount { get; set; } = 5;

    public Dictionary<string, ChainInfo> ChainInfos { get; set; }
}

public class ChainInfo
{
    public string BaseUrl { get; set; }
    public string TokenContractAddress { get; set; }
    public string PrivateKey { get; set; }
    
    public string PointTxPublicKey { get; set; }
    public string TokenContractAddress { get; set; }
    public string CrossChainContractAddress { get; set; }
}

public class FaucetsTransferOptions
{
    public string ChainId { get; set; }
    public int FaucetsTransferAmount { get; set; } = 1;
    public string FaucetsTransferSymbol { get; set; }
    public string ManagerAddress { get; set; }
    public int SymbolDecimal { get; set; } = 8;
}

public class SyncTokenOptions
{
    public string TargetChainId { get; set; } = "AELF";
    public string SourceChainId { get; set; } = "tDVW";
}