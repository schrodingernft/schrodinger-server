using System.Collections.Generic;

namespace SchrodingerServer.Adopts;

public class AdoptImageOptions
{
    public List<string> Images { get; set; }

    public List<string> WaterMarkImages { get; set; }

    public string ImageProvider { get; set; }
}