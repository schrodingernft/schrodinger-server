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
    public List<Trait> newTraits { get; set; }
    public BaseImage baseImage { get; set; }
}


public class BaseImage
{
    public List<Trait> traits { get; set; }
}

public class Trait
{
    public string name { get; set; }
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
    public string watermark  { get; set; }
}