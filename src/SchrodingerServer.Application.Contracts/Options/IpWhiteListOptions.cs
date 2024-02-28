using System.Collections.Generic;

namespace SchrodingerServer.Options;

public class IpWhiteListOptions
{

    public string HostHeader { get; set; } = "Host";

    public int DomainCacheSeconds { get; set; } = 1800;
    public List<string> HostWhiteList { get; set; } = new();
    public Dictionary<string, string> ByPath { get; set; } = new();
    
}