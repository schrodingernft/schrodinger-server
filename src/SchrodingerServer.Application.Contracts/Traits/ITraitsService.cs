using System.Threading.Tasks;

namespace SchrodingerServer.Traits;

public interface ITraitsService
{
    Task<string> GetRequestAsync(string adoptId);

    Task SetRequestAsync(string adoptId, string requestId);
}