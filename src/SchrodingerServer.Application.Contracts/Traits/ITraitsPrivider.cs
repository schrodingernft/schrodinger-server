using System.Threading.Tasks;
using SchrodingerServer.Dtos.TraitsDto;
using SchrodingerServer.Image;

namespace SchrodingerServer.Traits;

public interface ITraitsActionProvider
{
    Task<GenerateImageResponse> ImageGenerateAsync(string adoptId);

    Task<GetImageResponse> GetImageAsync(string adoptId);
}