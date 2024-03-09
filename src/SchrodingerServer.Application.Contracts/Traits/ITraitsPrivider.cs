using System.Threading.Tasks;
using SchrodingerServer.Dtos.TraitsDto;

namespace SchrodingerServer.Traits;

public interface ITraitsActionProvider
{
    Task<GenerateImageResponse> ImageGenerateAsync(string adoptId);

}