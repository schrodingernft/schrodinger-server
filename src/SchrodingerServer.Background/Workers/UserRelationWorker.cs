using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SchrodingerServer.Background.Services;
using SchrodingerServer.Options;
using SchrodingerServer.Points;
using SchrodingerServer.Users;
using SchrodingerServer.Users.Dto;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace SchrodingerServer.Background.Workers;

public class UserRelationWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IUserRelationService _userRelationService;
    private readonly ILogger<UserRelationWorker> _logger;

    // test
    private readonly IPointSettleService _pointSettleService;

    public UserRelationWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IUserRelationService userRelationService, IOptionsSnapshot<ZealyUserOptions> options,
        ILogger<UserRelationWorker> logger, IPointSettleService pointSettleService) : base(timer,
        serviceScopeFactory)
    {
        _userRelationService = userRelationService;
        _logger = logger;
        _pointSettleService = pointSettleService;
       // timer.Period = options.Value.Period * 60 * 1000;
        timer.Period = options.Value.Period  * 1000;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var bizId = Guid.NewGuid() + DateTime.UtcNow.ToString("yyyy-MM-dd");

        var pointSettleDto = new PointSettleDto()
        {
            ChainId = "tDVW",
            BizId = bizId,
            PointName = "XPSGR-4",
            UserPointsInfos = new List<UserPointInfo>()
            {
                new UserPointInfo()
                {
                    Address = "2bKypZR1eiCtgSb9XxjTic1e4wGySKGkRCP5KtE3ZGzhQdJxZt",
                    PointAmount = 1.8888m
                }
            }
        };

        await _pointSettleService.BatchSettleAsync(pointSettleDto);
        _logger.LogInformation("execute UserRelationWorker, data:{data}", JsonConvert.SerializeObject(pointSettleDto));
        _logger.LogInformation("begin execute UserRelationWorker.");
        await _userRelationService.AddUserRelationAsync();
        _logger.LogInformation("finish execute UserRelationWorker.");
    }
}