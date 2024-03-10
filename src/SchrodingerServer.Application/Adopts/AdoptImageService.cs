using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using SchrodingerServer.Grains.Grain.Traits;
using SchrodingerServer.Traits;

namespace SchrodingerServer.Adopts;

public class AdoptImageService : IAdoptImageService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<AdoptImageService> _logger;

    public AdoptImageService(IClusterClient clusterClient, ILogger<AdoptImageService> logger)
    {
        _clusterClient = clusterClient;
        _logger = logger;
    }
    
    public async Task<string> GetImageGenerationIdAsync(string adoptId)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IAdoptImageInfoGrain>(adoptId);
            return await grain.GetImageGenerationIdAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetRequestExistAsync Exception adoptId:{AdoptId}", adoptId);
            return string.Empty;
        }
    }

    public async Task SetImageGenerationIdAsync(string adoptId, string imageGenerationId)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IAdoptImageInfoGrain>(adoptId);
            await grain.SetImageGenerationIdAsync(imageGenerationId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SetRequestAsync Exception adoptId:{AdoptId}, imageGenerationId:{ImageGenerationId}", adoptId, imageGenerationId);
        }
    }

    public async Task<List<string>> GetImagesAsync(string adoptId)
    {
        var grain = _clusterClient.GetGrain<IAdoptImageInfoGrain>(adoptId);
        return await grain.GetImagesAsync();
    }

    public async Task SetImagesAsync(string adoptId, List<string> images)
    {
        var grain = _clusterClient.GetGrain<IAdoptImageInfoGrain>(adoptId);
        await grain.SetImagesAsync(images);
    }
}