using System.Collections.Generic;

namespace SchrodingerServer.Traits;

public class AdoptInfo
{
    public string Symbol { get; set; }
    public string TokenName { get; set; }
    public List<Attribute> Attributes { get; set; }
    public string Adoptor { get; set; }
    public long ImageCount { get; set; }
}

public class Attribute
{
    public string TraitType { get; set; }
    public string value { get; set; }
}