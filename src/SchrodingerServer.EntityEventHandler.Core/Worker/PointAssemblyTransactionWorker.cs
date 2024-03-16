using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchrodingerServer.Points;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace SchrodingerServer.EntityEventHandler.Core.Worker;

public class PointAssemblyTransactionWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IPointAssemblyTransactionService _pointAssemblyTransactionService;

    public PointAssemblyTransactionWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory, 
        IPointAssemblyTransactionService pointAssemblyTransactionService) :
        base(timer, serviceScopeFactory)
    {
        _pointAssemblyTransactionService = pointAssemblyTransactionService;
        Timer.Period = 1000 * 86400;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        Logger.LogInformation("Executing point assembly transaction job");
        
        await _pointAssemblyTransactionService.AssembleAsync("tDVW", "20240315");
    }
}