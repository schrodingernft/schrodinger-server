using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RedisRateLimiting;
using SchrodingerServer.Adopts;
using SchrodingerServer.Adopts.dispatcher;
using SchrodingerServer.Dtos.TraitsDto;
using SchrodingerServer.Image;
using Volo.Abp;
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
    private readonly IAdoptImageService _adoptImageService;

    public DefaultImageGenerateHandler(ILogger<DefaultImageGenerateHandler> logger, DefaultImageProvider defaultImageProvider,
        IRateDistributeLimiter rateDistributeLimiter, IObjectMapper objectMapper, IAdoptImageService adoptImageService)
    {
        _logger = logger;
        _defaultImageProvider = defaultImageProvider;
        _rateDistributeLimiter = rateDistributeLimiter;
        _objectMapper = objectMapper;
        _adoptImageService = adoptImageService;
    }

    public async Task HandleEventAsync(DefaultImageGenerateEto eventData)
    {
        _logger.LogInformation("HandleEventAsync DefaultImageGenerateEto  data: {data}", JsonConvert.SerializeObject(eventData));
        var requestId = await _adoptImageService.GetRequestIdAsync(eventData.AdoptId);
        var hasSendRequest = await _adoptImageService.HasSendRequest(eventData.AdoptId) &&
                             !string.IsNullOrWhiteSpace(requestId);
        if (hasSendRequest) return;
        var limiter = _rateDistributeLimiter.GetRateLimiterInstance("defaultImageGenerateHandler");
        var lease = await limiter.AcquireAsync();
        if (!lease.IsAcquired)
        {
            _logger.LogInformation("limit exceeded, will requeue, {AdoptId}", eventData.AdoptId);
            throw new UserFriendlyException("limit exceeded");
        }

        var imageInfo = _objectMapper.Map<GenerateImage, GenerateOpenAIImage>(eventData.GenerateImage);
        requestId = await _defaultImageProvider.RequestGenerateImage(eventData.AdoptId, imageInfo);
        // var requestId = await HandleAsync(async Task<string>() => , eventData.AdoptId);
        _logger.LogInformation("HandleEventAsync DefaultImageGenerateEto1 end data: {data} requestId={requestId}", JsonConvert.SerializeObject(eventData), requestId);
        if ("" == requestId)
        {
            return;
        }
        await _defaultImageProvider.SetRequestId(eventData.AdoptAddressId, requestId);

        _logger.LogInformation("HandleEventAsync DefaultImageGenerateEto end data: {data} requestId={requestId}", JsonConvert.SerializeObject(eventData), requestId);
    }

    // private async Task<T> HandleAsync<T>(Func<Task<T>> task, string adoptId)
    // {
    //     var limiter = _rateDistributeLimiter.GetRateLimiterInstance("defaultImageGenerateHandler");
    //     var lease = await limiter.AcquireAsync();
    //     if (!lease.IsAcquired)
    //     {
    //         if (lease.TryGetMetadata(RateLimitMetadataName.RetryAfter.Name, out var retryAfter))
    //         {
    //             _logger.LogInformation("limit exceeded, retry after {adoptId} {retryAfter} ms", adoptId, (int)retryAfter * 1000);
    //             await Task.Delay((int)retryAfter * 1000);
    //         }
    //     }
    //
    //     return await task();
    // }
}