namespace SchrodingerServer.Grains.Grain.ZealyScore.Dtos;

public class ZealyUserXpGrainDto
{
    public string Id { get; set; }
    public string Address { get; set; }

    public decimal LastXp { get; set; }
    public decimal CurrentXp { get; set; }
    public decimal SendAmount { get; set; }
    public decimal SendXp { get; set; }
    public decimal LastSendAmount { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
    public DateTime HandleXpTime { get; set; }
}