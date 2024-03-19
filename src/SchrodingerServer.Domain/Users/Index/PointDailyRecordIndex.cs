using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using SchrodingerServer.Entities;

namespace SchrodingerServer.Users.Index;

public class PointDailyRecordIndex : SchrodingerEntity<string>, IIndexBuild
{
    [Keyword] public string ChainId { get; set; }
    
    [Keyword] public string PointName { get; set; }
    
    [Keyword] public string BizDate { get; set; }
    
    [Keyword] public string Address { get; set; }
    
    public long PointAmount { get; set; }
    
    public DateTime CreateTime { get; set; }
}