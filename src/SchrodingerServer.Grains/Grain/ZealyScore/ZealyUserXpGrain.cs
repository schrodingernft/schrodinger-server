using Orleans;
using SchrodingerServer.Grains.State.ZealyScore;
using Volo.Abp.ObjectMapping;

namespace SchrodingerServer.Grains.Grain.ZealyScore;

public interface IZealyUserXpGrain : IGrainWithStringKey
{
    
}
    
public class ZealyUserXpGrain: Grain<ZealyUserXpState>, IZealyUserXpGrain
{
    private readonly IObjectMapper _objectMapper;

    public ZealyUserXpGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }
    
    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }
    
    public Task<GrainResultDto<ZealyUserXpGrainDto>> GetUserAsync()
    {
        return Task.FromResult(new GrainResultDto<UserGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<UserState, UserGrainDto>(State)
        });
    }
}