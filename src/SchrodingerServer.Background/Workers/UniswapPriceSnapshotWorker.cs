using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchrodingerServer.Common;
using SchrodingerServer.Options;
using SchrodingerServer.Symbol;
using SchrodingerServer.Token;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Caching;
using Volo.Abp.Threading;

namespace SchrodingerServer.Background.Workers;

public class UniswapPriceSnapshotWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly ILogger<UniswapPriceSnapshotWorker> _logger;
    private readonly IXgrPriceService _xgrPriceService;
    private readonly UniswapV3Provider _uniSwapV3Provider;
    private readonly IDistributedCache<string> _distributedCache;
    private readonly string _prefix = "UniswapPriceSnapshot-";

    public UniswapPriceSnapshotWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
         IOptionsSnapshot<ZealyUserOptions> options,
        ILogger<UniswapPriceSnapshotWorker> logger,
        IXgrPriceService xgrPriceService,
        UniswapV3Provider uniSwapV3Provider,
        IDistributedCache<string> distributedCache) : base(timer,
        serviceScopeFactory)
    {
        _logger = logger;
        _xgrPriceService = xgrPriceService;
        timer.Period = options.Value.Period  * 1000;
        _uniSwapV3Provider = uniSwapV3Provider;
        _distributedCache = distributedCache;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        _logger.LogInformation("begin execute UniswapPriceSnapshotWorker.");
        try
        {
            var date = GetUtcDay().ToUtcSeconds();
            var dateTime = await _distributedCache.GetAsync(_prefix + date);
            if (dateTime != null)
            {
                _logger.LogInformation("UniswapPriceSnapshotWorker has been executed today.");
                return;
            }
            var tokenRes = await _uniSwapV3Provider.GetLatestUSDPriceAsync(date);
            if (tokenRes != null)
            {
                await _xgrPriceService.SaveXgrDayPriceAsync(true);
                await _distributedCache.SetAsync(_prefix+date, DateTime.UtcNow.ToUtcSeconds().ToString(),  new DistributedCacheEntryOptions()
                {
                    SlidingExpiration = TimeSpan.FromDays(2)
                });
           }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UniswapPriceSnapshotWorker error.");
            return;
        }
        _logger.LogInformation("finish execute UniswapPriceSnapshotWorker.");
    }
    
    private DateTime GetUtcDay()
    {
        DateTime nowUtc = DateTime.UtcNow;
        return new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, 0, 0, 0, DateTimeKind.Utc);
    }
}