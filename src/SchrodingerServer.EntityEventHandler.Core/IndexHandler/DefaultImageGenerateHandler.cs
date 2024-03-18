using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SchrodingerServer.Adopts.dispatcher;
using SchrodingerServer.Image;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace SchrodingerServer.EntityEventHandler.Core.IndexHandler;

public class DefaultImageGenerateHandler : IDistributedEventHandler<DefaultImageGenerateEto>, ITransientDependency
{
    private readonly ILogger<DefaultImageGenerateHandler> _logger;
    private readonly DefaultImageProvider _defaultImageProvider;
    private readonly IRateDistributeLimiter _rateDistributeLimiter;

    public DefaultImageGenerateHandler(ILogger<DefaultImageGenerateHandler> logger, DefaultImageProvider defaultImageProvider, IRateDistributeLimiter rateDistributeLimiter)
    {
        _logger = logger;
        _defaultImageProvider = defaultImageProvider;
        _rateDistributeLimiter = rateDistributeLimiter;
    }

    public async Task HandleEventAsync(DefaultImageGenerateEto eventData)
    {
        _logger.LogInformation("HandleEventAsync DefaultImageGenerateEto  data: {data}", JsonConvert.SerializeObject(eventData));
        var requestId = await HandleAsync(async Task<string> () => await _defaultImageProvider.RequestGenerateImage(eventData.AdoptId,
            eventData.GenerateImage));
        await _defaultImageProvider.SetRequestId(eventData.AdoptAddressId, requestId);
            
        _logger.LogInformation("HandleEventAsync DefaultImageGenerateEto end");
    }

    private async Task<T> HandleAsync<T>(Func<Task<T>> task)
    {
        var limiter = _rateDistributeLimiter.GetRateLimiterInstance("defaultImageGenerateHandler");
        await limiter.AcquireAsync();
        // await _requestLimitProvider.RecordRequestAsync("defaultImageGenerateHandler-");
        return await task();
    }
    
}