namespace SchrodingerServer.Grains.Grain.Traits;

public class AdoptImageInfoState
{
    public string ImageGenerationId { get; set; }
    
    public List<string> Images { get; set; }
    
    public bool HasWatermark { get; set; }
}