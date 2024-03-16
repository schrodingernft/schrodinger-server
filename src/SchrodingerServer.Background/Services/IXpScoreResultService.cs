using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.Background.Services;

public interface IXpScoreResultService
{
    Task HandleXpResultAsync();
}

public class XpScoreResultService : IXpScoreResultService, ISingletonDependency
{
    
    public Task HandleXpResultAsync()
    {
        throw new System.NotImplementedException();
    }
}