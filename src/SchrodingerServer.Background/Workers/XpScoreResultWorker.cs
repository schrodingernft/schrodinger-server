using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace SchrodingerServer.Background.Workers;

public class XpScoreResultWorker : AsyncPeriodicBackgroundWorkerBase
{
    public XpScoreResultWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory) : base(timer, serviceScopeFactory)
    {
    }

    protected override Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        throw new System.NotImplementedException();
    }
}