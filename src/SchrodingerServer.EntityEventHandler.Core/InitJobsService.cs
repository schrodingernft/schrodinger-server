using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchrodingerServer.EntityEventHandler.Core.Options;
using SchrodingerServer.EntityEventHandler.Core.Worker;

namespace SchrodingerServer.EntityEventHandler.Core;

public class InitJobsService : BackgroundService
{
    private readonly IRecurringJobManager _recurringJobs;
    private readonly IOptionsMonitor<WorkerOptions> _workerOptionsMonitor;
    private readonly ILogger<InitJobsService> _logger;

    public InitJobsService(IRecurringJobManager recurringJobs, 
        IOptionsMonitor<WorkerOptions> workerOptionsMonitor, ILogger<InitJobsService> logger)
    {
        _recurringJobs = recurringJobs;
        _logger = logger;
        _workerOptionsMonitor = workerOptionsMonitor;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _recurringJobs.AddOrUpdate<ISyncHolderBalanceWorker>("ISyncHolderBalanceWorker",
                x => x.Invoke(), _workerOptionsMonitor.CurrentValue?.Workers?.GetValueOrDefault("ISyncHolderBalanceWorker")?.Cron ?? WorkerOptions.DefaultCron);
        }
        catch (Exception e)
        {
            _logger.LogError("An exception occurred while creating recurring jobs.", e);
        }

        return Task.CompletedTask;
    }
}