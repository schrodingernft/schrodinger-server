using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SchrodingerServer.Background.Dtos;
using SchrodingerServer.Background.Providers;
using SchrodingerServer.Common;
using SchrodingerServer.Options;
using SchrodingerServer.Users.Index;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.Background.Services;

public interface IUserRelationService
{
    Task AddUserRelationAsync();
}

public class UserRelationService : IUserRelationService, ISingletonDependency
{
    private readonly IZealyClientProxyProvider _zealyClientProxyProvider;
    private readonly ILogger<UserRelationService> _logger;
    private readonly INESTRepository<ZealyUserIndex, string> _zealyUserRepository;
    private readonly IDistributedCache<ReviewsCursorInfo> _distributedCache;
    private readonly ZealyUserOptions _options;
    private int _retryCount = 0;

    public UserRelationService(IZealyClientProxyProvider zealyClientProxyProvider,
        ILogger<UserRelationService> logger,
        INESTRepository<ZealyUserIndex, string> zealyUserRepository,
        IDistributedCache<ReviewsCursorInfo> distributedCache,
        IOptionsSnapshot<ZealyUserOptions> options
    )
    {
        _logger = logger;
        _zealyClientProxyProvider = zealyClientProxyProvider;
        _zealyUserRepository = zealyUserRepository;
        _distributedCache = distributedCache;
        _options = options.Value;
    }

    public async Task AddUserRelationAsync()
    {
        _logger.LogInformation("AddUserRelationAsync begin to execute.");
        await AddZealyUserWithRetryAsync();
        _logger.LogInformation("AddUserRelationAsync end to execute.");
    }

    private async Task AddZealyUserWithRetryAsync()
    {
        var nextCursor = string.Empty;
        var cursorInfo = await _distributedCache.GetAsync(nameof(ReviewsCursorInfo));

        if (cursorInfo != null)
        {
            nextCursor = cursorInfo.NextCursor;
        }

        _logger.LogInformation("begin to add zealy user, nextCursor:{nextCursor}", nextCursor);
        try
        {

            await AddZealyUserAsync(nextCursor);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "add zealy user error.");
            
            //todo: retry logic
        }
    }

    private async Task AddZealyUserAsync(string nextCursor)
    {
        var uri = CommonConstant.GetReviewsUri + $"?questId={_options.QuestId}&limit={_options.Limit}";
        if (!nextCursor.IsNullOrEmpty())
        {
            uri += $"cursor={nextCursor}";
        }

        var response = await _zealyClientProxyProvider.GetAsync<ReviewDto>(uri);

        if (response.NextCursor == null)
        {
            _logger.LogInformation("add zealy user finish");
            return;
        }

        // mapping index
        var users = GetUserIndices(response.Items);
        // add or update
        //await _zealyUserRepository.BulkAddOrUpdateAsync(users);

        //for test
        foreach (var user in users)
        {
            await _zealyUserRepository.AddOrUpdateAsync(user);
        }
        //

        await SetCursorInfoAsync(new ReviewsCursorInfo
        {
            NextCursor = response.NextCursor,
            UpdateTime = DateTime.UtcNow
        });

        await AddZealyUserAsync(response.NextCursor);
    }

    private List<ZealyUserIndex> GetUserIndices(List<ReviewItem> reviewItems)
    {
        var users = new List<ZealyUserIndex>();
        if (reviewItems.IsNullOrEmpty())
        {
            return users;
        }

        foreach (var item in reviewItems)
        {
            try
            {
                if (item.Tasks.Count == 0)
                {
                    _logger.LogError("user share wallet address task empty, data:{data}",
                        JsonConvert.SerializeObject(item));
                    continue;
                }

                if (item.Tasks.Count > 1)
                {
                    _logger.LogError("user share wallet address task count more than 1, data:{data}",
                        JsonConvert.SerializeObject(item));
                    continue;
                }

                var shareTask = item.Tasks.First();

                var address = GetAddress(shareTask.Value);

                var user = new ZealyUserIndex
                {
                    Id = item.User.Id,
                    Address = address,
                    CreateTime = item.Tasks.First().CreatedAt,
                    UpdateTime = DateTime.UtcNow
                };

                users.Add(user);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "map to user error, data:{data}", JsonConvert.SerializeObject(item));
                continue;
            }
        }

        return users;
    }

    private string GetAddress(string value)
    {
        if (value.IsNullOrEmpty() || !value.Trim().StartsWith("ELF_"))
        {
            throw new Exception($"invalid value address {value}");
        }

        var str = value.Trim().Split('_');

        // need check _tDVV ?
        return str[1];
    }

    private async Task SetCursorInfoAsync(ReviewsCursorInfo cursorInfo)
    {
        await _distributedCache.SetAsync(nameof(ReviewsCursorInfo), cursorInfo, new DistributedCacheEntryOptions()
        {
            AbsoluteExpiration = CommonConstant.DefaultAbsoluteExpiration
        });
    }
}