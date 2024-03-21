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
    private readonly IAdoptImageService _adoptImageService;
    private readonly IHandlerReporter _handlerReporter;

    public AutoMaticImageGenerateHandler(ILogger<AutoMaticImageGenerateHandler> logger, AutoMaticImageProvider autoMaticImageProvider, IRateDistributeLimiter rateDistributeLimiter, IAdoptImageService adoptImageService,
        IHandlerReporter handlerReporter)
    {
        _logger = logger;
        _autoMaticImageProvider = autoMaticImageProvider;
        _rateDistributeLimiter = rateDistributeLimiter;
        _adoptImageService = adoptImageService;
        _handlerReporter = handlerReporter;
    }

    public async Task HandleEventAsync(AutoMaticImageGenerateEto eventData)
    {
        _handlerReporter.RecordAiImageHandleAsync(ResourceName);
        _logger.LogInformation("HandleEventAsync autoMaticImageGenerateEto start, data: {data}", JsonConvert.SerializeObject(eventData));
        var hasSendRequest = await _adoptImageService.HasSendRequest(eventData.AdoptId);
        if (hasSendRequest) return;
        var limiter = _rateDistributeLimiter.GetRateLimiterInstance("autoMaticImageGenerateHandler");
        var lease = await limiter.AcquireAsync();
        if (!lease.IsAcquired)
        {
            _handlerReporter.RecordAiImageLimitExceedAsync(ResourceName);
            _logger.LogInformation("limit exceeded, will requeue, {AdoptId}", eventData.AdoptId);
            throw new UserFriendlyException("limit exceeded");
        }

        _handlerReporter.RecordAiImageGenAsync(ResourceName);
        var images = await _autoMaticImageProvider.RequestGenerateImage(eventData.AdoptId, eventData.GenerateImage);
        await _autoMaticImageProvider.SetAIGeneratedImages(eventData.AdoptId, images);
        await _autoMaticImageProvider.SetRequestId(eventData.AdoptAddressId, eventData.AdoptId);
        _logger.LogInformation("HandleEventAsync autoMaticImageGenerateEto end");
    }
}