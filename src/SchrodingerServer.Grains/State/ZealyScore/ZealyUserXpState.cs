namespace SchrodingerServer.Grains.State.ZealyScore;

public class ZealyUserXpState
{
    public string Id { get; set; }
    public string Address { get; set; }
    public decimal LastXp { get; set; }
    public decimal Xp { get; set; }
    public long UseRepairTime { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
    public DateTime HandleXpTime { get; set; }
}