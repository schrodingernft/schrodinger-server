using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchrodingerServer.Common;
using SchrodingerServer.EntityEventHandler.Core.Options;
using SchrodingerServer.Options;
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
    private readonly IOptionsMonitor<PointTradeOptions> _pointTradeOptions;

    private readonly IObjectMapper _objectMapper;
    private readonly IPointDailyRecordService _pointDailyRecordService;
    private readonly ISymbolDayPriceProvider _symbolDayPriceProvider;

    public SyncHolderBalanceWorker(ILogger<SyncHolderBalanceWorker> logger,
        IHolderBalanceProvider holderBalanceProvider, IOptionsMonitor<WorkerOptions> workerOptionsMonitor,
        INESTRepository<HolderBalanceIndex, string> holderBalanceIndexRepository, IObjectMapper objectMapper,
        IPointDailyRecordService pointDailyRecordService,
        ISymbolDayPriceProvider symbolDayPriceProvider,
        IOptionsMonitor<PointTradeOptions> pointTradeOptions)
    {
        _logger = logger;
        _holderBalanceProvider = holderBalanceProvider;
        _workerOptionsMonitor = workerOptionsMonitor;
        _holderBalanceIndexRepository = holderBalanceIndexRepository;
        _objectMapper = objectMapper;
        _pointDailyRecordService = pointDailyRecordService;
        _symbolDayPriceProvider = symbolDayPriceProvider;
        _pointTradeOptions = pointTradeOptions;
    }

    public async Task Invoke()
    {
        _logger.LogInformation("SyncHolderBalanceWorker start...");

        var bizDate = _workerOptionsMonitor.CurrentValue.BizDate;
        if (bizDate.IsNullOrEmpty())
        {
            //TODO use block time
            bizDate = DateTime.UtcNow.AddDays(-1).ToString(TimeHelper.Pattern);
        }

        //TODO control repeat execute
        var chainIds = _workerOptionsMonitor.CurrentValue.ChainIds;
        if (chainIds.IsNullOrEmpty())
        {
            _logger.LogError("SyncHolderBalanceWorker chainIds has no config...");
            return;
        }

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
        var priceBizDate = TimeHelper.GetDateStrAddDays(bizDate, -1);
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
            symbols.Add(_pointTradeOptions.CurrentValue.BaseCoin);
            
            var symbolPriceDict = await _symbolDayPriceProvider.GetSymbolPricesAsync(priceBizDate, symbols.ToList());

            //get user latest date balance and add change
            var saveList = new List<HolderBalanceIndex>();
            foreach (var item in dailyChanges)
            {
                var symbolPrice = DecimalHelper.GetValueFromDict(symbolPriceDict, item.Symbol,
                    _pointTradeOptions.CurrentValue.BaseCoin);

                var holderBalance = _objectMapper.Map<HolderDailyChangeDto, HolderBalanceIndex>(item);
                holderBalance.Id = IdGenerateHelper.GetHolderBalanceId(holderBalance.ChainId, holderBalance.BizDate,
                    holderBalance.Symbol, holderBalance.Address);
                var preHolderBalance = await GetPreHolderBalanceAsync(chainId, bizDate, item.Address);

                //save real balance
                holderBalance.Balance = preHolderBalance + item.ChangeAmount;
                item.Balance = holderBalance.Balance;
                await _pointDailyRecordService.HandlePointDailyChangeAsync(chainId, item, symbolPrice);
                saveList.Add(holderBalance);
            }

            //update bizDate holder balance
            await _holderBalanceIndexRepository.BulkAddOrUpdateAsync(saveList);

            skipCount += dailyChanges.Count;
        } while (!dailyChanges.IsNullOrEmpty());

        _logger.LogInformation("SyncHolderBalanceWorker chainId:{chainId} end...", chainId);
    }

    private async Task<long> GetPreHolderBalanceAsync(string chainId, string bizDate, string address)
    {
        var preHolderBalanceIndex = await _holderBalanceProvider.GetPreHolderBalanceAsync(chainId, bizDate, address);
        return preHolderBalanceIndex.Balance;
    }
}