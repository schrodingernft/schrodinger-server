using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SchrodingerServer.Adopts;
using SchrodingerServer.Adopts.dispatcher;
using SchrodingerServer.EntityEventHandler.Core.Reporter;
using SchrodingerServer.Image;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace SchrodingerServer.EntityEventHandler.Core.IndexHandler;

public class AutoMaticImageGenerateHandler : IDistributedEventHandler<AutoMaticImageGenerateEto>, ITransientDependency
{
    private const string ResourceName = "autoMaticImageGenerateHandler";
    private readonly ILogger<AutoMaticImageGenerateHandler> _logger;
    private readonly AutoMaticImageProvider _autoMaticImageProvider;
    private readonly IRateDistributeLimiter _rateDistributeLimiter;
    private readonly IHandlerReporter _handlerReporter;

    public AutoMaticImageGenerateHandler(ILogger<AutoMaticImageGenerateHandler> logger, AutoMaticImageProvider autoMaticImageProvider, IRateDistributeLimiter rateDistributeLimiter, IHandlerReporter handlerReporter)
    {
        _logger = logger;
        _autoMaticImageProvider = autoMaticImageProvider;
        _rateDistributeLimiter = rateDistributeLimiter;
        _handlerReporter = handlerReporter;
    }

    public async Task HandleEventAsync(AutoMaticImageGenerateEto eventData)
    {
        _handlerReporter.RecordAiImageHandle(ResourceName);
        _logger.LogInformation("HandleEventAsync autoMaticImageGenerateEto start, data: {data}", JsonConvert.SerializeObject(eventData));
        if (await _autoMaticImageProvider.RequestIdIsNotNullOrEmptyAsync(eventData.AdoptAddressId)) // already generated
        {
            return;
        }

        var limiter = _rateDistributeLimiter.GetRateLimiterInstance("autoMaticImageGenerateHandler");
        var lease = await limiter.AcquireAsync();
        if (!lease.IsAcquired)
        {
            _handlerReporter.RecordAiImageLimitExceed(ResourceName);
            _logger.LogInformation("limit exceeded, will requeue, {AdoptId}", eventData.AdoptId);
            throw new UserFriendlyException("limit exceeded");
        }

        _handlerReporter.RecordAiImageGen(ResourceName);
        var images = await _autoMaticImageProvider.RequestGenerateImage(eventData.AdoptId, eventData.GenerateImage);
        await _autoMaticImageProvider.SetAIGeneratedImagesAsync(eventData.AdoptId, images);
        await _autoMaticImageProvider.SetRequestIdAsync(eventData.AdoptAddressId, eventData.AdoptId);
        _logger.LogInformation("HandleEventAsync autoMaticImageGenerateEto end");
    }
}