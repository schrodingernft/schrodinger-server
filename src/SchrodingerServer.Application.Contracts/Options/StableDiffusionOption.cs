namespace SchrodingerServer.Options;

public class StableDiffusionOption
{
    public string SamplerIndex { get; set; } = "DPM++ 2M Karras";
    public string NagativePrompt { get; set; } = "NSFW";
    public int Step { get; set; }
    public int BatchSize { get; set; } = 2;
    public int Width { get; set; } = 1024;
    public int Height { get; set; } = 1024;
    public int NIters { get; set; } = 1;
}