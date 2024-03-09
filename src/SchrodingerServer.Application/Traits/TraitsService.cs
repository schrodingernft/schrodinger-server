using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using SchrodingerServer.Grains.Grain.Traits;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace SchrodingerServer.Traits;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class TraitsService : SchrodingerServerAppService, ITraitsService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<TraitsService> _logger;

    public TraitsService(IClusterClient clusterClient, ILogger<TraitsService> logger)
    {
        _clusterClient = clusterClient;
        _logger = logger;
    }


    public async Task<string> GetRequestAsync(string adoptId)
    {
        try
        {
            var grain = _clusterClient.GetGrain<ITraitsGrain>(adoptId);
            return await grain.GetState();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetRequestExistAsync Exception adoptId:{adoptId}", adoptId);
            return string.Empty;
        }
    }

    public async Task SetRequestAsync(string adoptId, string requestId)
    {
        try
        {
            var grain = _clusterClient.GetGrain<ITraitsGrain>(adoptId);
            await grain.SetStateAsync(requestId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SetRequestAsync Exception adoptId:{adoptId}, requestId:{requestId}", adoptId, requestId);
        }
    }
}