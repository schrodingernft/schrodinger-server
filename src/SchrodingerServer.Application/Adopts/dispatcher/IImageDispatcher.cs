using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SchrodingerServer.Dtos.TraitsDto;
using SchrodingerServer.Options;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.Adopts.dispatcher;

public interface IImageDispatcher
{
    IImageProvider CurrentProvider();
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


    public IImageProvider CurrentProvider()
    {
        if (_providers.TryGetValue(_adoptImageOptions.ImageProvider, out var provider))
        {
            return provider;
        }

        _logger.LogError("Get AI Provider Failed");
        return _defaultImageProvider;
    }
}