using System.Collections.Generic;

namespace SchrodingerServer.Options;

public class PointTradeOptions
{
    public Dictionary<string, ChainInfo> ChainInfos { get; set; } = new();

    //key is point name
    public Dictionary<string, PointInfo> PointMapping { get; set; } = new();
    
    public ChainInfo GetChainInfo(string chainId)
    {
        return ChainInfos.TryGetValue(chainId, out var chainInfo) ? chainInfo : null;
    }
    
    public string GetActionName(string pointName)
    {
        return PointMapping.TryGetValue(pointName, out var pointInfo) ? pointInfo.ActionName : null;
    }
}

public class ChainInfo
{
    public string SchrodingerContractAddress { get; set; }

    public string ContractMethod { get; set; }
}

public class PointInfo
{
    public string ActionName { get; set; }

    public string ConditionalExp { get; set; }
    
    public decimal? Factor { get; set; }
    
    public bool NeedMultiplyPrice { get; set; }
}