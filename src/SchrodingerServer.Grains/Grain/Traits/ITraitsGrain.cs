using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using SchrodingerServer.Options;
using Volo.Abp.ObjectMapping;

namespace SchrodingerServer.Grains.Grain.Traits;

public interface ITraitsGrain : IGrainWithStringKey
{
    Task SetStateAsync(string requestId);
    Task<string> GetState();
}

public class TraitsGrain : Grain<TraitsState>, ITraitsGrain
{
    private readonly ILogger<TraitsGrain> _logger;
    private readonly IOptionsMonitor<ChainOptions> _chainOptionsMonitor;
    private readonly IObjectMapper _objectMapper;


    public TraitsGrain( ILogger<TraitsGrain> logger, IObjectMapper objectMapper, 
        IOptionsMonitor<ChainOptions> chainOptionsMonitor)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _chainOptionsMonitor = chainOptionsMonitor;
    }


    public async Task SetStateAsync(string requestId)
    {
        State.RequestId = requestId;
        await WriteStateAsync();
    }

    public Task<string> GetState()
    {
        return Task.FromResult(State.RequestId);
    }
}