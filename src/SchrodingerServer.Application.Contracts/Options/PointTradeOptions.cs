using System.Collections.Generic;

namespace SchrodingerServer.Options;

public class PointTradeOptions
{
    public string ChainId { get; set; }
    
    public string ContractAddress { get; set; }

    public string ContractMethod { get; set; }
    
    //key is point name
    public Dictionary<string, PointInfo> PointMapping { get; set; } = new();
    
    public string GetActionName(string pointName)
    {
        return PointMapping.TryGetValue(pointName, out var pointInfo) ? pointInfo.ActionName : null;
    }
}

public class PointInfo
{
    public string ActionName { get; set; }
    
    public string ConditionalExp { get; set; }
}