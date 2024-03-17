using Volo.Abp.EventBus;

namespace SchrodingerServer.Image;

[EventName("DefaultImageGenerateEto")]
public class DefaultImageGenerateEto
{
    public string AdoptId { get; set; }
    public string RequestId { get; set; }
}