using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SchrodingerServer.Background.Services;
using SchrodingerServer.Common;
using SchrodingerServer.Common.GraphQL;
using SchrodingerServer.Options;
using SchrodingerServer.Users.Index;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Caching;
using Volo.Abp.Threading;

namespace SchrodingerServer.Background.Workers;

public class UserRelationWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IUserRelationService _userRelationService;
    private readonly ICleanUpService _cleanUpService;
    private readonly ILogger<UserRelationWorker> _logger;
    private readonly IDistributedCache<string> _distributedCache;
    private string _key = "ZealyScoreCleanUp";

    private readonly IGraphQlHelper _graphQlHelper;

    public UserRelationWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IUserRelationService userRelationService, IOptionsSnapshot<ZealyUserOptions> options,
        ILogger<UserRelationWorker> logger, IDistributedCache<string> distributedCache,
        ICleanUpService cleanUpService, IGraphQlHelper graphQlHelper) : base(timer,
        serviceScopeFactory)
    {
        _userRelationService = userRelationService;
        _logger = logger;
        _distributedCache = distributedCache;
        _cleanUpService = cleanUpService;
        _graphQlHelper = graphQlHelper;
        timer.Period = options.Value.Period * 1000;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        _logger.LogInformation("begin execute UserRelationWorker.");
        var cache = await _distributedCache.GetAsync(_key);
        if (!cache.IsNullOrEmpty())
        {
            _logger.LogInformation("clean up task already execute.");
            return;
        }
        
        await SetCacheAsync();
        
        await _cleanUpService.CleanUpAsync();
        _logger.LogInformation("finish execute UserRelationWorker.");
    }

    private async Task SetCacheAsync()
    {
        await _distributedCache.SetAsync(_key, "scoreCleanUp", new DistributedCacheEntryOptions()
        {
            AbsoluteExpiration = DateTimeOffset.Now.AddDays(1)
        });
    }
}