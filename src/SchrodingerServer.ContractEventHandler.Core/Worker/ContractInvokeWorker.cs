using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchrodingerServer.ContractEventHandler.Core.Application;
using SchrodingerServer.ContractEventHandler.Core.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace SchrodingerServer.ContractEventHandler.Core.Worker;

public class ContractInvokeWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IContractInvokeService _contractInvokeService;

    public ContractInvokeWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IContractInvokeService contractInvokeService,
        IOptionsMonitor<ContractSyncOptions> contractSyncOptionsMonitor) :
        base(timer, serviceScopeFactory)
    {
        _contractInvokeService = contractInvokeService;
        Timer.Period = 1000 * contractSyncOptionsMonitor.CurrentValue.Sync;
        contractSyncOptionsMonitor.OnChange((_, _) =>
        {
            Timer.Period = 1000 * contractSyncOptionsMonitor.CurrentValue.Sync;
        });
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        Logger.LogInformation("Executing contract invoke job");
        var bizIds = await _contractInvokeService.SearchUnfinishedTransactionsAsync();
        var tasks = new List<Task>();
        foreach (var bizId in bizIds)
        {
            tasks.Add(Task.Run(() => { _contractInvokeService.ExecuteJobAsync(bizId); }));
        }

        await Task.WhenAll(tasks);
    }
}