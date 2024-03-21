using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchrodingerServer.Options;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.Adopts.dispatcher;

public interface IImageDispatcher
{
    IImageProvider GetProviderByName(string providerName);
}

public class ImageDispatcher : IImageDispatcher, ISingletonDependency
{
    private readonly AdoptImageOptions _adoptImageOptions;
    private readonly ILogger<ImageDispatcher> _logger;
    private readonly Dictionary<string, IImageProvider> _providers;
    private readonly DefaultImageProvider _defaultImageProvider;

    public ImageDispatcher(IOptionsMonitor<AdoptImageOptions> adoptImageOptions, ILogger<ImageDispatcher> logger, IEnumerable<IImageProvider> providers, DefaultImageProvider defaultImageProvider)
    {
        _adoptImageOptions = adoptImageOptions.CurrentValue;
        _logger = logger;
        _defaultImageProvider = defaultImageProvider;
        _providers = providers.ToDictionary(x => x.Type.ToString(), y => y);
    }


    private IImageProvider CurrentProvider()
    {
        if (_providers.TryGetValue(_adoptImageOptions.ImageProvider, out var provider))
        {
            return provider;
        }

        _logger.LogError("Get AI Provider Failed");
        return _defaultImageProvider;
    }

    public IImageProvider GetProviderByName(string providerName)
    {
        if (providerName.IsNullOrEmpty())
        {
            return CurrentProvider();
        }

        return _providers.TryGetValue(providerName, out var provider) ? provider : _defaultImageProvider;
    }
}