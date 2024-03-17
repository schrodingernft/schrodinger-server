using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SchrodingerServer.Adopts.dispatcher;
using SchrodingerServer.Image;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace SchrodingerServer.EntityEventHandler.Core.IndexHandler;

public class AutoMaticImageGenerateHandler : IDistributedEventHandler<AutoMaticImageGenerateEto>, ITransientDependency
{
    private readonly ILogger<AutoMaticImageGenerateHandler> _logger;
    private readonly AutoMaticImageProvider _autoMaticImageProvider;
    private readonly IRateDistributeLimiter _rateDistributeLimiter;

    public AutoMaticImageGenerateHandler(ILogger<AutoMaticImageGenerateHandler> logger, AutoMaticImageProvider autoMaticImageProvider, IRequestLimitProvider requestLimitProvider, IRateDistributeLimiter rateDistributeLimiter)
    {
        _logger = logger;
        _autoMaticImageProvider = autoMaticImageProvider;
        _rateDistributeLimiter = rateDistributeLimiter;
    }

    public async Task HandleEventAsync(AutoMaticImageGenerateEto eventData)
    {
        _logger.LogInformation("HandleEventAsync autoMaticImageGenerateEto start, {requestId} {adoptId}", eventData.RequestId, eventData.AdoptId);
        await HandleAsync(async () => await _autoMaticImageProvider.GenerateImageAsync(eventData.RequestId, eventData.AdoptId));
        _logger.LogInformation("HandleEventAsync autoMaticImageGenerateEto end, {requestId} {adoptId}", eventData.RequestId, eventData.AdoptId);
    }

    private async Task<T> HandleAsync<T>(Func<Task<T>> task)
    {
        var limiter = _rateDistributeLimiter.GetRateLimiterInstance("autoMaticImageGenerateHandler");
        await limiter.AcquireAsync();
        // await _requestLimitProvider.RecordRequestAsync("autoMaticImageGenerateHandler-");
        return await task();
    }
}