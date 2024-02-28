using System;
using System.Threading.Tasks;
using DnsClient.Internal;
using Microsoft.Extensions.Logging;
using Orleans;
using SchrodingerServer.Common;
using SchrodingerServer.Grains.Grain.Users;
using SchrodingerServer.PointServer;
using Volo.Abp.Application.Services;

namespace SchrodingerServer.Users;

public class UserActionProvider : ApplicationService, IUserActionProvider
{
    private readonly ILogger<UserActionProvider> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IPointServerProvider _pointServerProvider;

    public UserActionProvider(IClusterClient clusterClient, IPointServerProvider pointServerProvider,
        ILogger<UserActionProvider> logger)
    {
        _clusterClient = clusterClient;
        _pointServerProvider = pointServerProvider;
        _logger = logger;
    }

    public async Task<bool> CheckDomainAsync(string domain)
    {
        try
        {
            return await _pointServerProvider.CheckDomainAsync(domain);
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