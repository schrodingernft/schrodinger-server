using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Schrodinger;
using SchrodingerServer.EntityEventHandler.Core.Options;
using SchrodingerServer.Symbol.Index;
using SchrodingerServer.Users;
using SchrodingerServer.Users.Index;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.EntityEventHandler.Core.Worker;

public interface ISyncHolderBalanceWorker
{
    Task Invoke();
}

public class SyncHolderBalanceWorker : ISyncHolderBalanceWorker, ISingletonDependency
{
    private readonly ILogger<SyncHolderBalanceWorker> _logger;
    private readonly IHolderBalanceProvider _holderBalanceProvider;
    private readonly INESTRepository<HolderBalanceIndex, string> _holderBalanceIndexRepository;
    private readonly IOptionsMonitor<WorkerOptions> _workerOptionsMonitor;

    private const int MaxResultCount = 800;

    public SyncHolderBalanceWorker(ILogger<SyncHolderBalanceWorker> logger,
        IHolderBalanceProvider holderBalanceProvider, IOptionsMonitor<WorkerOptions> workerOptionsMonitor, INESTRepository<HolderBalanceIndex, string> holderBalanceIndexRepository)
    {
        _logger = logger;
        _holderBalanceProvider = holderBalanceProvider;
        _workerOptionsMonitor = workerOptionsMonitor;
        _holderBalanceIndexRepository = holderBalanceIndexRepository;
    }

    public async Task Invoke()
    {
        _logger.LogInformation("SyncHolderBalanceWorker start...");

        var bizDate = "";
        foreach (var chainId in _workerOptionsMonitor.CurrentValue.ChainIds)
        {
            await HandleHolderDailyChangeAsync(chainId, bizDate);
        }
    }

    private async Task HandleHolderDailyChangeAsync(string chainId, string bizDate)
    {
        _logger.LogInformation("SyncHolderBalanceWorker chainId:{chainId} start...", chainId);
        var skipCount = 0;
        List<HolderDailyChangeDto> dailyChanges;
        do
        {
            dailyChanges =
                await _holderBalanceProvider.GetHolderDailyChangeList(chainId, bizDate, skipCount, MaxResultCount);
            _logger.LogInformation(
                "GetHolderDailyChangeList chainId:{chainId} skipCount: {skipCount} bizDate:{bizDate} count: {count}",
                chainId, bizDate, skipCount, dailyChanges?.Count);
            if (dailyChanges.IsNullOrEmpty())
            {
                break;
            }

            skipCount += dailyChanges.Count;
            
            var list = new List<HolderBalanceIndex>();
            await _holderBalanceIndexRepository.BulkAddOrUpdateAsync(list);
            
            //Packaged transactions in batches
            
            //id: batchDate + actionName
            var pointNameGroup = list
                .GroupBy(balance => balance.PointName) 
                .ToDictionary(
                    group => group.Key,
                    group => group.ToList()
                );
            
            
            
            
        } while (!dailyChanges.IsNullOrEmpty());

        _logger.LogInformation("SyncHolderBalanceWorker chainId:{chainId} end...", chainId);
    }

}