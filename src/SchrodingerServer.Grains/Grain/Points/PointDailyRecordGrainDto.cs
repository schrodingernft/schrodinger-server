namespace SchrodingerServer.Grains.Grain.Points;

public class PointDailyRecordGrainDto
{
    public string Id { get; set; }

    public string ChainId { get; set; }
    
    public string PointName { get; set; }
    
    public string BizDate { get; set; }
    
    public string Address { get; set; }
    
    public long PointAmount { get; set; }
    
    public DateTime CreateTime { get; set; }
}