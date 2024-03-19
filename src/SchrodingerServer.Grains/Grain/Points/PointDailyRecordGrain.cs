using Orleans;
using SchrodingerServer.Grains.State.Points;
using Volo.Abp.ObjectMapping;

namespace SchrodingerServer.Grains.Grain.Points;

public interface IPointDailyRecordGrain : IGrainWithStringKey
{
    Task<GrainResultDto<PointDailyRecordGrainDto>> UpdateAsync(PointDailyRecordGrainDto input);
}

public class PointDailyRecordGrain : Grain<PointDailyRecordState>, IPointDailyRecordGrain
{
    private readonly IObjectMapper _objectMapper;

    public PointDailyRecordGrain(IObjectMapper objectMapper)
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

    public async Task<GrainResultDto<PointDailyRecordGrainDto>> UpdateAsync(PointDailyRecordGrainDto input)
    {
        var prePointAmount = State.PointAmount;
        State = _objectMapper.Map<PointDailyRecordGrainDto, PointDailyRecordState>(input);
        if (State.Id.IsNullOrEmpty())
        {
            State.Id = this.GetPrimaryKey().ToString();
        }
        if (State.CreateTime == DateTime.MinValue)
        {
            State.CreateTime =  DateTime.UtcNow;
        }
        //accumulated points amount
        State.PointAmount = prePointAmount + input.PointAmount;
        State.UpdateTime = DateTime.UtcNow;
        await WriteStateAsync();

        return new GrainResultDto<PointDailyRecordGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<PointDailyRecordState, PointDailyRecordGrainDto>(State)
        };
    }
}