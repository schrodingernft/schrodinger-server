using System.Threading.Tasks;

namespace SchrodingerServer.Traits;

public interface ITraitsActionProvider
{
    Task<bool> ImageGenerateAsync(string adoptId);

}