using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SchrodingerServer.Adopts.dispatcher;
using SchrodingerServer.Image;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace SchrodingerServer.EntityEventHandler.Core.IndexHandler;

public class DefaultImageGenerateHandler : IDistributedEventHandler<DefaultImageGenerateEto>, ITransientDependency
{
    private readonly ILogger<DefaultImageGenerateHandler> _logger;
    private readonly DefaultImageProvider _defaultImageProvider;
    private readonly IRequestLimitProvider _requestLimitProvider;

    public DefaultImageGenerateHandler(ILogger<DefaultImageGenerateHandler> logger, DefaultImageProvider defaultImageProvider, IRequestLimitProvider requestLimitProvider)
    {
        _logger = logger;
        _defaultImageProvider = defaultImageProvider;
        _requestLimitProvider = requestLimitProvider;
    }

    public async Task HandleEventAsync(DefaultImageGenerateEto eventData)
    {
        _logger.LogInformation("HandleEventAsync DefaultImageGenerateEto start, {requestId} {adoptId}", eventData.RequestId, eventData.AdoptId);
        await HandleAsync(async () => await _defaultImageProvider.GenerateImageAsync(eventData.RequestId, eventData.AdoptId));
        _logger.LogInformation("HandleEventAsync DefaultImageGenerateEto end, {requestId} {adoptId}", eventData.RequestId, eventData.AdoptId);
    }

    private async Task<T> HandleAsync<T>(Func<Task<T>> task)
    {
        await _requestLimitProvider.RecordRequestAsync("defaultImageGenerateHandler-");
        return await task();
    }
}