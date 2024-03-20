namespace SchrodingerServer.Common.Options;

public class StableDiffusionOption
{
    public string SamplerIndex { get; set; } = "DPM++ 2M Karras";
    public string NagativePrompt { get; set; } = "NSFW";
    public int Step { get; set; } = 20;
    public int BatchSize { get; set; } = 2;
    public int Width { get; set; } = 512;
    public int Height { get; set; } = 512;
    public int NIters { get; set; } = 1;
}