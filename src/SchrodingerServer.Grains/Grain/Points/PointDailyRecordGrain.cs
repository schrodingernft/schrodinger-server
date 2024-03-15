using Orleans;
using SchrodingerServer.Grains.State.Points;

namespace SchrodingerServer.Grains.Grain.Points;

public interface IPointDailyRecordGrain : IGrainWithStringKey
{
    Task<GrainResultDto<PointDailyRecordGrainDto>> CreateAsync(PointDailyRecordGrainDto input);
}

public class PointDailyRecordGrain : Grain<PointDailyRecordState>, IPointDailyRecordGrain
{
    
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
    
    public Task<GrainResultDto<PointDailyRecordGrainDto>> CreateAsync(PointDailyRecordGrainDto input)
    {
        throw new NotImplementedException();
    }
}