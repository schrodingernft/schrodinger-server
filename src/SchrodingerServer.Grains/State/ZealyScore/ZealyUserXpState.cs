namespace SchrodingerServer.Grains.State.ZealyScore;

public class ZealyUserXpState
{
    public string Id { get; set; }
    public string Address { get; set; }
    public decimal LastXp { get; set; }
    public decimal CurrentXp { get; set; }
    public decimal SendXp { get; set; }
    public decimal LastSendXp { get; set; }
    public decimal SendAmount { get; set; }
    public decimal LastSendAmount { get; set; }
    public decimal TempXp { get; set; }
    public decimal TempSendXp { get; set; }
    public decimal TempSendAmount { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
    public bool IsRollback { get; set; }
}