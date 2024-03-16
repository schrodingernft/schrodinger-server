using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SchrodingerServer.Points;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.EntityEventHandler.Core.Worker;

public interface IPointAssemblyTransactionWorker
{
    Task Invoke();
}

public class PointAssemblyTransactionWorker : IPointAssemblyTransactionWorker, ISingletonDependency
{
    private readonly IPointAssemblyTransactionService _pointAssemblyTransactionService;
    private readonly ILogger<PointAssemblyTransactionWorker> _logger;

    public PointAssemblyTransactionWorker(IPointAssemblyTransactionService pointAssemblyTransactionService,
        ILogger<PointAssemblyTransactionWorker> logger)
    {
        _pointAssemblyTransactionService = pointAssemblyTransactionService;
        _logger = logger;
    }


    public async Task Invoke()
    {
        _logger.LogInformation("Executing point assembly transaction job start");

        await _pointAssemblyTransactionService.AssembleAsync("tDVW", "20240315");
    }
}