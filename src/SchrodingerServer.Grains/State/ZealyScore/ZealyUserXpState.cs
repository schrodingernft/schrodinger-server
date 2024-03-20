namespace SchrodingerServer.Grains.State.ZealyScore;

public class ZealyUserXpState
{
    // search record and send , es->add
    public List<RecordInfo> RecordInfos { get; set; } = new();
    public string Id { get; set; }
    public string Address { get; set; }
    public decimal LastXp { get; set; }
    public decimal CurrentXp { get; set; }
    public decimal TempXp { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
    public bool IsRollback { get; set; }
}

public class RecordInfo
{
    public string Date { get; set; }
    public decimal CurrentXp { get; set; }
    public decimal Xp { get; set; }
    public decimal Amount { get; set; }
}