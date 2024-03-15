using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using SchrodingerServer.Entities;

namespace SchrodingerServer.Symbol.Index;

public class HolderBalanceIndex : SchrodingerEntity<string>, IIndexBuild
{
    [Keyword] public string PointName { get; set; }
    
    [Keyword] public string Address { get; set; }
    
    [Keyword] public string BizDate { get; set; }
    
    public long Balance { get; set; }
    
    public DateTime ChangeTime { get; set; }
    
    //the bizId of contract invoke
    [Keyword] public string BizId { get; set; }
}