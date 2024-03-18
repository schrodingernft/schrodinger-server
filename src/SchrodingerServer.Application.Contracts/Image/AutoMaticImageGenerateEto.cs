using System.Collections.Generic;
using SchrodingerServer.Dtos.TraitsDto;
using Volo.Abp.EventBus;

namespace SchrodingerServer.Image;

[EventName("AutoMaticImageGenerateEto")]
public class AutoMaticImageGenerateEto
{
    public string AdoptId { get; set; }
    public string RequestId { get; set; }

    public GenerateImage GenerateImage { get; set; }
}