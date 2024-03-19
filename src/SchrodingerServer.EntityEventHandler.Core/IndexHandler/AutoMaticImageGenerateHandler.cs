using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
        _logger.LogInformation("HandleEventAsync autoMaticImageGenerateEto start, data: {data}", JsonConvert.SerializeObject(eventData));
        var images =  await HandleAsync(async Task<List<string>> () => await _autoMaticImageProvider.RequestGenerateImage(eventData.AdoptId,
            eventData.GenerateImage));
        await _autoMaticImageProvider.SetAIGeneratedImages(eventData.AdoptId, images);
        await _autoMaticImageProvider.SetRequestId(eventData.AdoptAddressId, eventData.AdoptId); 
        _logger.LogInformation("HandleEventAsync autoMaticImageGenerateEto end");
    }

    private async Task<T> HandleAsync<T>(Func<Task<T>> task)
    {
        var limiter = _rateDistributeLimiter.GetRateLimiterInstance("autoMaticImageGenerateHandler");
        await limiter.AcquireAsync();
        // await _requestLimitProvider.RecordRequestAsync("autoMaticImageGenerateHandler-");
        return await task();
    }
}