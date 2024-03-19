using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SchrodingerServer.Adopts.dispatcher;
using SchrodingerServer.Dtos.TraitsDto;
using SchrodingerServer.Image;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace SchrodingerServer.EntityEventHandler.Core.IndexHandler;

public class DefaultImageGenerateHandler : IDistributedEventHandler<DefaultImageGenerateEto>, ITransientDependency
{
    private readonly ILogger<DefaultImageGenerateHandler> _logger;
    private readonly DefaultImageProvider _defaultImageProvider;
    private readonly IRateDistributeLimiter _rateDistributeLimiter;
    private readonly IObjectMapper _objectMapper;

    public DefaultImageGenerateHandler(ILogger<DefaultImageGenerateHandler> logger, DefaultImageProvider defaultImageProvider, 
        IRateDistributeLimiter rateDistributeLimiter, IObjectMapper objectMapper)
    {
        _logger = logger;
        _defaultImageProvider = defaultImageProvider;
        _rateDistributeLimiter = rateDistributeLimiter;
        _objectMapper = objectMapper;
    }

    public async Task HandleEventAsync(DefaultImageGenerateEto eventData)
    {
        _logger.LogInformation("HandleEventAsync DefaultImageGenerateEto  data: {data}", JsonConvert.SerializeObject(eventData));
        var imageInfo = _objectMapper.Map<GenerateImage, GenerateOpenAIImage>(eventData.GenerateImage);
        var requestId = await HandleAsync(async Task<string> () => await _defaultImageProvider.RequestGenerateImage(eventData.AdoptId,
            imageInfo));
        await _defaultImageProvider.SetRequestId(eventData.AdoptAddressId, requestId);
            
        _logger.LogInformation("HandleEventAsync DefaultImageGenerateEto end");
    }

    private async Task<T> HandleAsync<T>(Func<Task<T>> task)
    {
        var limiter = _rateDistributeLimiter.GetRateLimiterInstance("defaultImageGenerateHandler");
        await limiter.AcquireAsync();
        return await task();
    }
    
}