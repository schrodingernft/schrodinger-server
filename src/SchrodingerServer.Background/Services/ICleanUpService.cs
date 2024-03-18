using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Hangfire;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SchrodingerServer.Background.Dtos;
using SchrodingerServer.Background.Providers;
using SchrodingerServer.Common;
using SchrodingerServer.Options;
using SchrodingerServer.Zealy;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.Background.Services;

public interface ICleanUpService
{
    Task CleanUpAsync();
}

public class CleanUpService : ICleanUpService, ISingletonDependency
{
    private readonly ILogger<ZealyScoreService> _logger;
    private readonly IUserRelationService _userRelationService;
    private readonly IZealyProvider _zealyProvider;
    private readonly IZealyClientProvider _zealyClientProxyProvider;
    private readonly INESTRepository<ZealyUserXpIndex, string> _zealyUserXpRepository;
    private readonly ICallContractProvider _contractProvider;
    private readonly ZealyScoreOptions _options;
    private List<ZealyUserXpIndex> _zealyUserXps = new();
    private List<ZealyXpScoreIndex> _zealyXpScores = new();

    public CleanUpService(ILogger<ZealyScoreService> logger)
    {
        _logger = logger;
    }

    public async Task CleanUpAsync()
    {
        _logger.LogInformation("begin clean up zealy score recurring job");
        await HandleUserScoreAsync();
        _logger.LogInformation("finish clean up zealy score recurring job");
    }

    private async Task GetUserXpsAsync(List<ZealyUserXpIndex> userIndices,
        int skipCount, int maxResultCount)
    {
        var userXps =
            await _zealyProvider.GetUserXpsAsync(skipCount, maxResultCount);
        userIndices.AddRange(userXps);

        if (userXps.IsNullOrEmpty() || userXps.Count < maxResultCount)
        {
            return;
        }

        skipCount += maxResultCount;
        await GetUserXpsAsync(userIndices, skipCount, maxResultCount);
    }

    private async Task HandleUserScoreAsync()
    {
        await GetUserXpsAsync(_zealyUserXps, 0, _options.FetchCount);

        foreach (var user in _zealyUserXps)
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

    private async Task HandleUserScoreAsync(ZealyUserXpIndex user)
    {
        var response = await GetZealyUserAsync(user.Id);
        if (response == null)
        {
            return;
        }

        var xp = response.Xp;
        // user.Xp = xp;
        // user.LastXp = 0m;
        _logger.LogInformation(
            "calculate xp, userId:{userId}, responseXp:{responseXp}, userXp:{userXp}", user.Id, response.Xp, user.Xp);
        
        if (xp > 0)
        {
            BackgroundJob.Enqueue(() => _contractProvider.CreateAsync(user));
        }

        user.HandleXpTime = DateTime.UtcNow;
        await _zealyUserXpRepository.AddOrUpdateAsync(user);
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