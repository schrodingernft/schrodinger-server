using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Hangfire;
using Microsoft.Extensions.Logging;
using SchrodingerServer.Background.Dtos;
using SchrodingerServer.Background.Providers;
using SchrodingerServer.Common;
using SchrodingerServer.Users.Index;
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
    private readonly ZealyClientProxyProvider _zealyClientProxyProvider;
    private readonly INESTRepository<ZealyUserXpIndex, string> _zealyUserXpRepository;
    private readonly ICallContractProvider _contractProvider;
    private List<ZealyUserXpIndex> _zealyUserXps = new();

    public ZealyScoreService(ILogger<ZealyScoreService> logger, IUserRelationService userRelationService,
        IZealyProvider zealyProvider, ZealyClientProxyProvider zealyClientProxyProvider,
        INESTRepository<ZealyUserXpIndex, string> zealyUserXpRepository, ICallContractProvider contractProvider)
    {
        _logger = logger;
        _userRelationService = userRelationService;
        _zealyProvider = zealyProvider;
        _zealyClientProxyProvider = zealyClientProxyProvider;
        _zealyUserXpRepository = zealyUserXpRepository;
        _contractProvider = contractProvider;
    }

    public async Task UpdateScoreAsync()
    {
        _logger.LogInformation("update zealy score recurring job execute, time:{time}",
            DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        // update user
        await _userRelationService.AddUserRelationAsync();

        // wait es synchronization
        await Task.Delay(1000);

        await HandleUserScoreAsync();
        // ...

        Console.WriteLine("ZealyScoreService execute...");
    }

    private async Task GetUsersAsync(List<ZealyUserIndex> userIndices,
        int skipCount, int maxResultCount)
    {
        var users =
            await _zealyProvider.GetUsersAsync(skipCount, maxResultCount);
        userIndices.AddRange(users);

        if (users.Count < maxResultCount)
        {
            return;
        }

        skipCount += maxResultCount;
        await GetUsersAsync(userIndices, skipCount, maxResultCount);
    }

    private async Task GetUserXpsAsync(List<ZealyUserXpIndex> userIndices,
        int skipCount, int maxResultCount)
    {
        var users =
            await _zealyProvider.GetUserXpsAsync(skipCount, maxResultCount);
        userIndices.AddRange(users);

        if (users.Count < maxResultCount)
        {
            return;
        }

        skipCount += maxResultCount;
        await GetUserXpsAsync(userIndices, skipCount, maxResultCount);
    }

    private async Task HandleUserScoreAsync()
    {
        var users = new List<ZealyUserIndex>();
        await GetUsersAsync(users, 0, 1000);
        await GetUserXpsAsync(_zealyUserXps, 0, 1000);

        //List<Task> tasks = new List<Task>();
        foreach (var user in users)
        {
            await HandleUserScoreAsync(user);
        }
    }

    private async Task HandleUserScoreAsync(ZealyUserIndex user)
    {
        // get total score from user
        var uri = CommonConstant.GetUserUri + $"/{user.Id}";
        var response = await _zealyClientProxyProvider.GetAsync<ZealyUserDto>(uri);

        var userXp = _zealyUserXps.FirstOrDefault(t => t.Id == user.Id);

        var xp = 0m;
        if (userXp == null)
        {
            userXp = new ZealyUserXpIndex()
            {
                Id = user.Id,
                Address = user.Address,
                CreateTime = DateTime.UtcNow
            };

            xp = response.Xp;
        }
        else
        {
            xp = response.Xp - userXp.Xp;
        }


        if (xp > 0)
        {
            // contract xp
            BackgroundJob.Enqueue(() => _contractProvider.CreateAsync(userXp, xp));
        }
        else
        {
            userXp.Xp = userXp.Xp == 0 ? response.Xp : userXp.Xp;
            userXp.LastXp = userXp.Xp;
        }

        userXp.HandleXpTime = DateTime.UtcNow;
        await _zealyUserXpRepository.AddOrUpdateAsync(userXp);
    }
}