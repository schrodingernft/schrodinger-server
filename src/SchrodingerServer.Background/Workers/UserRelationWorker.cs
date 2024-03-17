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
using SchrodingerServer.Users.Dto;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace SchrodingerServer.Background.Workers;

public class UserRelationWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IUserRelationService _userRelationService;
    private readonly ILogger<UserRelationWorker> _logger;
    private bool Start = false;
    private readonly IContractInvokeService _contractInvokeService;

    // test
    private readonly IPointSettleService _pointSettleService;

    public UserRelationWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IUserRelationService userRelationService, IOptionsSnapshot<ZealyUserOptions> options,
        ILogger<UserRelationWorker> logger, IPointSettleService pointSettleService, IContractInvokeService contractInvokeService) : base(timer,
        serviceScopeFactory)
    {
        _userRelationService = userRelationService;
        _logger = logger;
        _pointSettleService = pointSettleService;
        _contractInvokeService = contractInvokeService;
        timer.Period = options.Value.Period * 1000;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        
        if (Start)
        {
            return;
        }
        await _contractInvokeService.ExecuteJobAsync("f572fb6a-9044-462c-aca1-28fa49d00611-2024-03-17");
        Start = true;
        
        _logger.LogInformation("begin execute UserRelationWorker.");
        await _userRelationService.AddUserRelationAsync();
        _logger.LogInformation("finish execute UserRelationWorker.");
    }
}