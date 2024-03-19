using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SchrodingerServer.Background.Dtos;
using SchrodingerServer.Background.Providers;
using SchrodingerServer.Common;
using SchrodingerServer.Options;
using SchrodingerServer.Zealy;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.Background.Services;

public interface IZealyScoreService
{
    Task UpdateScoreAsync();
}

public class ZealyScoreService : IZealyScoreService, ISingletonDependency
{
    private readonly ILogger<ZealyScoreService> _logger;
    private readonly IUserRelationService _userRelationService;
    private readonly IZealyProvider _zealyProvider;

    //private readonly IZealyClientProxyProvider _zealyClientProxyProvider;
    private readonly IZealyClientProvider _zealyClientProxyProvider;
    private readonly IXpRecordProvider _xpRecordProvider;
    private readonly ZealyScoreOptions _options;
    private List<ZealyXpScoreIndex> _zealyXpScores = new();
    private readonly IDistributedCache<UpdateScoreInfo> _distributedCache;
    private readonly IBalanceProvider _balanceProvider;
    private const string _updateScorePrefix = "UpdateZealyScoreInfo";

    public ZealyScoreService(ILogger<ZealyScoreService> logger,
        IUserRelationService userRelationService,
        IZealyProvider zealyProvider,
        IZealyClientProvider zealyClientProxyProvider,
        IXpRecordProvider xpRecordProvider,
        IOptionsSnapshot<ZealyScoreOptions> options,
        IDistributedCache<UpdateScoreInfo> distributedCache, IBalanceProvider balanceProvider)
    {
        _logger = logger;
        _userRelationService = userRelationService;
        _zealyProvider = zealyProvider;
        _zealyClientProxyProvider = zealyClientProxyProvider;
        _xpRecordProvider = xpRecordProvider;
        _distributedCache = distributedCache;
        _balanceProvider = balanceProvider;
        _options = options.Value;
    }

    public async Task UpdateScoreAsync()
    {
        try
        {
            var jobIsStart = await CheckJobAsync();
            if (!jobIsStart)
            {
                _logger.LogWarning("update zealy score recurring job is started");
                return;
            }

            _logger.LogInformation("begin update zealy score recurring job");
            // update user
            await _userRelationService.AddUserRelationAsync();

            // wait es synchronization
            await Task.Delay(1000);

            await HandleUserScoreAsync();
            _logger.LogInformation("finish update zealy score recurring job");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "update zealy score error");
        }
    }

    private async Task<bool> CheckJobAsync()
    {
        var key = $"{_updateScorePrefix}:{DateTime.UtcNow:yyyy-MM-dd}";
        var cache = await _distributedCache.GetAsync(key);
        if (cache != null)
        {
            return false;
        }

        await _distributedCache.SetAsync(key, new UpdateScoreInfo()
        {
            UpdateTime = DateTime.UtcNow
        }, new DistributedCacheEntryOptions()
        {
            AbsoluteExpiration = DateTimeOffset.Now.AddHours(6)
        });

        return true;
    }

    private async Task GetUsersAsync(List<ZealyUserIndex> userIndices,
        int skipCount, int maxResultCount)
    {
        var users =
            await _zealyProvider.GetUsersAsync(skipCount, maxResultCount);
        userIndices.AddRange(users);

        if (users.IsNullOrEmpty() || users.Count < maxResultCount)
        {
            return;
        }

        skipCount += maxResultCount;
        await GetUsersAsync(userIndices, skipCount, maxResultCount);
    }

    private async Task GetXpScoresAsync(List<ZealyXpScoreIndex> xpScoreIndices,
        int skipCount, int maxResultCount)
    {
        var xpScores =
            await _zealyProvider.GetXpScoresAsync(skipCount, maxResultCount);
        xpScoreIndices.AddRange(xpScores);

        if (xpScores.IsNullOrEmpty() || xpScores.Count < maxResultCount)
        {
            return;
        }

        skipCount += maxResultCount;
        await GetXpScoresAsync(xpScoreIndices, skipCount, maxResultCount);
    }

    private async Task HandleUserScoreAsync()
    {
        var users = new List<ZealyUserIndex>();
        await GetUsersAsync(users, 0, _options.FetchCount);
        await GetXpScoresAsync(_zealyXpScores, 0, _options.FetchCount);

        foreach (var user in users)
        {
            try
            {
                await HandleUserScoreAsync(user);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "handle user score error, userInfo:{userInfo}", JsonConvert.SerializeObject(user));
                continue;
            }
        }
    }

    private async Task HandleUserScoreAsync(ZealyUserIndex user)
    {
        var userDto = await GetZealyUserAsync(user.Id);
        if (userDto == null)
        {
            return;
        }

        var xp = 0m;
        var userXpScore = _zealyXpScores.FirstOrDefault(t => t.Id == user.Id);

        // call contract can limit zealy request.
        //need to handle in hangfire, call contract fail retry.
        var pointOutput = await _balanceProvider.GetPointsBalanceOutputAsync(user.Address);
        var userXpAmount = pointOutput.Balance;
        if (userXpAmount == 0)
        {
            xp = userXpScore == null ? userDto.Xp : userXpScore.ActualScore;
            _logger.LogInformation(
                "calculate xp, userId:{userId}, responseXp:{responseXp}, userXp:{userXp},  xp:{xp}",
                user.Id, userDto.Xp, userXpAmount, xp);
        }
        else
        {
            var repairScore = 0m;
            if (userXpScore != null)
            {
                repairScore = userXpScore.ActualScore - userXpScore.RawScore;
            }

            xp = userDto.Xp + repairScore - DecimalHelper.Divide(userXpAmount, 8);
            _logger.LogInformation(
                "calculate xp, userId:{userId}, responseXp:{responseXp}, userXp:{userXp}, xp:{xp}",
                user.Id, userDto.Xp, userXpAmount, xp);
        }

        if (xp > 0)
        {
            BackgroundJob.Enqueue(() => _xpRecordProvider.CreateRecordAsync(user.Id, user.Address, xp));
        }
    }

    private async Task<ZealyUserDto> GetZealyUserAsync(string userId)
    {
        try
        {
            var uri = CommonConstant.GetUserUri + $"/{userId}";
            _logger.LogInformation("get user info, uri:{uri}", uri);
            return await _zealyClientProxyProvider.GetAsync<ZealyUserDto>(uri);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "get user score from zealy error, userId:{userId}", userId);
            return null;
        }
    }
}