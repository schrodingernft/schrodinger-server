using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using SchrodingerServer.Common;
using SchrodingerServer.Grains.Grain.Users;
using SchrodingerServer.Options;
using SchrodingerServer.PointServer;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;

namespace SchrodingerServer.Users;

public class UserActionProvider : ApplicationService, IUserActionProvider
{
    private readonly ILogger<UserActionProvider> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IPointServerProvider _pointServerProvider;
    private readonly IDistributedCache<string> _checkDomainCache;
    private readonly IOptionsMonitor<IpWhiteListOptions> _ipWhiteListOptions;

    public UserActionProvider(IClusterClient clusterClient, IPointServerProvider pointServerProvider,
        ILogger<UserActionProvider> logger, IDistributedCache<string> checkDomainCache, IOptionsMonitor<IpWhiteListOptions> ipWhiteListOptions)
    {
        _clusterClient = clusterClient;
        _pointServerProvider = pointServerProvider;
        _logger = logger;
        _checkDomainCache = checkDomainCache;
        _ipWhiteListOptions = ipWhiteListOptions;
    }

    public async Task<bool> CheckDomainAsync(string domain)
    {
        try
        {
            var cacheResult = await _checkDomainCache.GetOrAddAsync("DomainCheck:" + domain,
                async () => (await _pointServerProvider.CheckDomainAsync(domain)).ToString(),
                () => new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(_ipWhiteListOptions.CurrentValue.DomainCacheSeconds)
                });
            return bool.TryParse(cacheResult, out var resultValue) && resultValue;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Check domain error");
            return false;
        }
    }

    public async Task<DateTime?> GetActionTimeAsync(ActionType actionType)
    {
        if (CurrentUser is not { IsAuthenticated: true }) return null;
        var userId = CurrentUser.Id ?? Guid.Empty;
        if (userId == Guid.Empty) return null;

        var userActionGrain = _clusterClient.GetGrain<IUserActionGrain>(userId);
        return await userActionGrain.GetActionTime(actionType);
    }

    public async Task<UserActionGrainDto> AddActionAsync(ActionType actionType)
    {
        if (CurrentUser is not { IsAuthenticated: true }) return null;
        var userId = CurrentUser.Id ?? Guid.Empty;
        if (userId == Guid.Empty) return null;

        var userActionGrain = _clusterClient.GetGrain<IUserActionGrain>(userId);
        var res = await userActionGrain.AddActionAsync(actionType);
        AssertHelper.IsTrue(res?.Success ?? false, "Query action time failed");
        return res!.Data;
    }
}