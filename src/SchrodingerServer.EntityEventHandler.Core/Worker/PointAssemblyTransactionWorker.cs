using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchrodingerServer.Common;
using SchrodingerServer.EntityEventHandler.Core.Options;
using SchrodingerServer.Points;
using SchrodingerServer.Points.Provider;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Threading;

namespace SchrodingerServer.EntityEventHandler.Core.Worker;

public class PointAssemblyTransactionWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IPointAssemblyTransactionService _pointAssemblyTransactionService;
    private readonly ILogger<PointAssemblyTransactionWorker> _logger;
    private readonly IOptionsMonitor<WorkerOptions> _workerOptionsMonitor;
    private readonly IPointDispatchProvider _pointDispatchProvider;
    private readonly IAbpDistributedLock _distributedLock;

    private readonly string _lockKey = "IPointAssemblyTransactionWorker";

    public PointAssemblyTransactionWorker(AbpAsyncTimer timer,IServiceScopeFactory serviceScopeFactory,IPointAssemblyTransactionService pointAssemblyTransactionService,
        ILogger<PointAssemblyTransactionWorker> logger, IOptionsMonitor<WorkerOptions> workerOptionsMonitor,
        IAbpDistributedLock distributedLock,
        IPointDispatchProvider pointDispatchProvider) : base(timer,
    serviceScopeFactory)
    {
        _pointAssemblyTransactionService = pointAssemblyTransactionService;
        _logger = logger;
        _workerOptionsMonitor = workerOptionsMonitor;
        _pointDispatchProvider = pointDispatchProvider;
        _distributedLock = distributedLock;
        timer.Period =(int)(_workerOptionsMonitor.CurrentValue?.Workers?.GetValueOrDefault(_lockKey).Minutes * 60 * 1000);
    }
    
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await using var handle =
            await _distributedLock.TryAcquireAsync(_lockKey);
        _logger.LogInformation("Executing point assembly transaction job start");
        var bizDate = _workerOptionsMonitor.CurrentValue.BizDate;
        if (bizDate.IsNullOrEmpty())
        {
            bizDate = DateTime.UtcNow.AddDays(-1).ToString(TimeHelper.Pattern);
        }
        var isExecuted =  await _pointDispatchProvider.GetDispatchAsync(PointDispatchConstants.POINT_ASSEMBLY_TRANSACTION_PREFIX, bizDate);
        if (isExecuted)
        {
            _logger.LogInformation("PointAssemblyTransactionWorker has been executed for bizDate: {0}", bizDate);
            return;
        }
        var isBeforeExecuted =  await _pointDispatchProvider.GetDispatchAsync(PointDispatchConstants.SYNC_HOLDER_BALANCE_PREFIX, bizDate);
        if (!isBeforeExecuted)
        {
            _logger.LogInformation("SyncHolderBalanceWorker has not  executed for bizDate: {0}", bizDate);
            return;
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
        _logger.LogInformation("Executing point assembly transaction job end");
        await _pointDispatchProvider.SetDispatchAsync(PointDispatchConstants.POINT_ASSEMBLY_TRANSACTION_PREFIX, bizDate,true);
    }

}