using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using SchrodingerServer.Options;
using Volo.Abp.ObjectMapping;

namespace SchrodingerServer.Grains.Grain.Traits;

public interface IAdoptImageInfoGrain : IGrainWithStringKey
{
    Task SetImageGenerationIdAsync(string imageGenerationId);
    Task SetImagesAsync(List<string> images);
    Task<string> GetImageGenerationIdAsync();
    Task<List<string>> GetImagesAsync();
}

public class AdoptImageInfoGrain : Grain<AdoptImageInfoState>, IAdoptImageInfoGrain
{
    private readonly ILogger<AdoptImageInfoGrain> _logger;
    private readonly IOptionsMonitor<ChainOptions> _chainOptionsMonitor;
    private readonly IObjectMapper _objectMapper;


    public AdoptImageInfoGrain( ILogger<AdoptImageInfoGrain> logger, IObjectMapper objectMapper, 
        IOptionsMonitor<ChainOptions> chainOptionsMonitor)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _chainOptionsMonitor = chainOptionsMonitor;
    }


    public async Task SetImageGenerationIdAsync(string imageGenerationId)
    {
        State.ImageGenerationId = imageGenerationId;
        await WriteStateAsync();
    }

    public async Task SetImagesAsync(List<string> images)
    {
        State.Images = images;
        await WriteStateAsync();
    }

    public Task<string> GetImageGenerationIdAsync()
    {
        return Task.FromResult(State.ImageGenerationId);
    }

    public Task<List<string>> GetImagesAsync()
    {
        return Task.FromResult(State.Images);
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }

    public override async Task OnActivateAsync()
    {
        await base.ReadStateAsync();
        await base.OnActivateAsync();
    }
}