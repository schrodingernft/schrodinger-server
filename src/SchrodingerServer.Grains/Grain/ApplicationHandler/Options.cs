namespace SchrodingerServer.Grains.Grain.ApplicationHandler;

public class ChainOptions
{
    public Dictionary<string, ChainInfo> ChainInfos { get; set; }
}

public class ChainInfo
{
    public string BaseUrl { get; set; }
    public string TokenContractAddress { get; set; }
    public string PrivateKey { get; set; }
}

public class FaucetsTransferOptions
{
    public string ChainId { get; set; }
    public int FaucetsTransferAmount { get; set; }
    public string FaucetsTransferSymbol { get; set; }
    public string ManagerAddress { get; set; }
}