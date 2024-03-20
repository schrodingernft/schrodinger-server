using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchrodingerServer.Background.Services;
using SchrodingerServer.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace SchrodingerServer.Background.Workers;

public class XpScoreResultWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IXpScoreResultService _xpScoreResultService;
    private readonly ILogger<XpScoreResultService> _logger;

    public XpScoreResultWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IXpScoreResultService xpScoreResultService, ILogger<XpScoreResultService> logger,
        IOptionsSnapshot<UpdateScoreOptions> options) : base(timer,
        serviceScopeFactory)
    {
        _xpScoreResultService = xpScoreResultService;
        _logger = logger;
        timer.Period = options.Value.UpdateXpScoreResultPeriod * 60 * 1000;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        _logger.LogInformation("XpScoreResultWorker begin");
       // await _xpScoreResultService.HandleXpResultAsync();
        _logger.LogInformation("XpScoreResultWorker finish");
    }
}