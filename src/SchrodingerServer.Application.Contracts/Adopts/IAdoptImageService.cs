using System.Collections.Generic;
using System.Threading.Tasks;

namespace SchrodingerServer.Adopts;

public interface IAdoptImageService
{
    Task<string> GetImageGenerationIdAsync(string adoptId);

    Task SetImageGenerationIdAsync(string adoptId, string imageGenerationId);

    Task<List<string>> GetImagesAsync(string adoptId);
    Task SetImagesAsync(string adoptId,List<string> images);

    Task SetWatermarkAsync(string adoptId);
    Task<bool> HasWatermark(string adoptId);
    
    Task SetImageHashAsync(string adoptId, string hash);
}