using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SchrodingerServer.Common;
using SchrodingerServer.EntityEventHandler.Core.Options;
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
    private readonly IOptionsMonitor<WorkerOptions> _workerOptionsMonitor;

    public PointAssemblyTransactionWorker(IPointAssemblyTransactionService pointAssemblyTransactionService,
        ILogger<PointAssemblyTransactionWorker> logger, IOptionsMonitor<WorkerOptions> workerOptionsMonitor)
    {
        _pointAssemblyTransactionService = pointAssemblyTransactionService;
        _logger = logger;
        _workerOptionsMonitor = workerOptionsMonitor;
    }
    
    public async Task Invoke()
    {
        _logger.LogInformation("Executing point assembly transaction job start");

        var bizDate = _workerOptionsMonitor.CurrentValue.BizDate;
        if (bizDate.IsNullOrEmpty())
        {
            bizDate = DateTime.UtcNow.AddDays(-1).ToString(TimeHelper.Pattern);
        }
        var chainIds = _workerOptionsMonitor.CurrentValue.ChainIds;
        if (chainIds.IsNullOrEmpty())
        {
            _logger.LogError("PointAssemblyTransactionWorker chainIds has no config...");
            return;
        }
        foreach (var chainId in _workerOptionsMonitor.CurrentValue.ChainIds)
        {
            await _pointAssemblyTransactionService.AssembleAsync(chainId, bizDate);
        }
    }
}