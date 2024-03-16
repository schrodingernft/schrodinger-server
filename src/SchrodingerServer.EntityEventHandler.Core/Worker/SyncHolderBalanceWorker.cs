using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchrodingerServer.Common;
using SchrodingerServer.EntityEventHandler.Core.Options;
using SchrodingerServer.Points;
using SchrodingerServer.Symbol.Provider;
using SchrodingerServer.Users;
using SchrodingerServer.Users.Index;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace SchrodingerServer.EntityEventHandler.Core.Worker;

public interface ISyncHolderBalanceWorker
{
    Task Invoke();
}

public class SyncHolderBalanceWorker : ISyncHolderBalanceWorker, ISingletonDependency
{
    private const int MaxResultCount = 800;

    private readonly ILogger<SyncHolderBalanceWorker> _logger;
    private readonly IHolderBalanceProvider _holderBalanceProvider;
    private readonly INESTRepository<HolderBalanceIndex, string> _holderBalanceIndexRepository;
    private readonly IOptionsMonitor<WorkerOptions> _workerOptionsMonitor;
    private readonly IObjectMapper _objectMapper;
    private readonly IPointDailyRecordService _pointDailyRecordService;
    private readonly ISymbolDayPriceProvider _symbolDayPriceProvider;
    
    public SyncHolderBalanceWorker(ILogger<SyncHolderBalanceWorker> logger,
        IHolderBalanceProvider holderBalanceProvider, IOptionsMonitor<WorkerOptions> workerOptionsMonitor,
        INESTRepository<HolderBalanceIndex, string> holderBalanceIndexRepository, IObjectMapper objectMapper,
        IPointDailyRecordService pointDailyRecordService, 
        ISymbolDayPriceProvider symbolDayPriceProvider)
    {
        _logger = logger;
        _holderBalanceProvider = holderBalanceProvider;
        _workerOptionsMonitor = workerOptionsMonitor;
        _holderBalanceIndexRepository = holderBalanceIndexRepository;
        _objectMapper = objectMapper;
        _pointDailyRecordService = pointDailyRecordService;
        _symbolDayPriceProvider = symbolDayPriceProvider;
    }

    public async Task Invoke()
    {
        _logger.LogInformation("SyncHolderBalanceWorker start...");

        var bizDate = _workerOptionsMonitor.CurrentValue.BizDate;
        if (bizDate.IsNullOrEmpty())
        {
            //TODO use block time
            bizDate = DateTime.UtcNow.ToString(TimeHelper.Pattern);
        }

        //TODO control repeat execute

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
                await _holderBalanceProvider.GetHolderDailyChangeListAsync(chainId, bizDate, skipCount, MaxResultCount);
            _logger.LogInformation(
                "GetHolderDailyChangeList chainId:{chainId} skipCount: {skipCount} bizDate:{bizDate} count: {count}",
                chainId, bizDate, skipCount, dailyChanges?.Count);
            if (dailyChanges.IsNullOrEmpty())
            {
                break;
            }

            var symbols = dailyChanges.Select(item => item.Symbol).ToHashSet();

            var symbolPriceDict = await _symbolDayPriceProvider.GetSymbolPricesAsync(bizDate, symbols.ToList());
            
            //get pre date balance and add change
            var saveList = new List<HolderBalanceIndex>();
            foreach (var item in dailyChanges)
            {
                var symbolPrice = symbolPriceDict.TryGetValue(item.Symbol, out var price) ? price : (decimal?)null;
                await _pointDailyRecordService.HandlePointDailyChangeAsync(chainId, item, symbolPrice);

                var holderBalance = _objectMapper.Map<HolderDailyChangeDto, HolderBalanceIndex>(item);

                var preHolderBalanceDict = await _holderBalanceProvider.GetPreHolderBalanceAsync(chainId, bizDate,
                    new List<string>
                    {
                        item.Address
                    });
                if (preHolderBalanceDict.TryGetValue(item.Address, out var preHolderBalance))
                {
                    holderBalance.Balance = item.ChangeAmount + preHolderBalance.Balance;
                }
                else
                {
                    holderBalance.Balance = item.ChangeAmount;
                }

                saveList.Add(holderBalance);
            }

            //update bizDate holder balance
            await _holderBalanceIndexRepository.BulkAddOrUpdateAsync(saveList);

            skipCount += dailyChanges.Count;
            
        } while (!dailyChanges.IsNullOrEmpty());

        _logger.LogInformation("SyncHolderBalanceWorker chainId:{chainId} end...", chainId);
    }
}