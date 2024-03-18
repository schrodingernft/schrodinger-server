using AElf.Indexing.Elasticsearch;
using Nest;
using SchrodingerServer.Entities;

namespace SchrodingerServer.Zealy;

public class ZealyUserXpRecordCleanUpIndex : SchrodingerEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string UserId { get; set; }
    [Keyword] public string Address { get; set; }

    public decimal Xp { get; set; }

    // xp * coefficient
    public decimal Amount { get; set; }
    public decimal RawAmount { get; set; }
    public decimal TotalAmount { get; set; }
    [Keyword] public string BizId { get; set; }
    [Keyword] public string Status { get; set; }
    public long CreateTime { get; set; }
    public long UpdateTime { get; set; }
    public long UseRepairTime { get; set; }
}