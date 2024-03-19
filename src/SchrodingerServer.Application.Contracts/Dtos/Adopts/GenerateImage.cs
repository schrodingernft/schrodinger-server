using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SchrodingerServer.Dtos.TraitsDto;

public class GenerateImageRequest
{
    [Required] public string AdoptId { get; set; }
}

public class GenerateImageResponse
{
    public GenerateImage items { get; set; }
    public AiQueryResponse images { get; set; }
}

public class ImageInfo
{
    public string Signature { get; set; }
    public string WaterMarkImage { get; set; }
    public string Image { get; set; }
}

public class ImageTrait
{
    public string Generation { get; set; }
    public TraitInfo TraitInfo { get; set; }
}

public class TraitInfo
{
    public string TraitType { get; set; }
    public string Value { get; set; }
    public string Percent { get; set; }
}

public class QueryImage
{
    public string requestId { get; set; }
}

public class GenerateImage
{
    public string seed { get; set; }
    public List<Trait> newAttributes { get; set; }
    public BaseImage baseImage { get; set; }
    
    public int numImages { get; set; }
}


public class BaseImage
{
    public List<Trait> attributes { get; set; }
}

public class Trait
{
    public string traitType { get; set; }
    public string value { get; set; }
}

public class AiQueryResponse
{
    public List<Image> images { get; set; }
}

public class Image
{
    public List<Trait> traits { get; set; }
    public string image { get; set; }
    public string waterMarkImage { get; set; }
    public string extraData { get; set; }
}

public class ImageOperation{
    public string salt { get; set; }
    public string image { get; set; }
}


public class WatermarkInput
{
    public string sourceImage  { get; set; }
    public WaterMark watermark  { get; set; }
}

public class WaterMark
{
    public string text  { get; set; }
}

public class WatermarkResponse
{
    public string processedImage  { get; set; }
    public string resized { get; set; }
}

public class IsOverLoadedResponse
{
    public bool isOverLoaded  { get; set; }
}

public class GenerateImageFromAiRes
{
    public string requestId  { get; set; }
}