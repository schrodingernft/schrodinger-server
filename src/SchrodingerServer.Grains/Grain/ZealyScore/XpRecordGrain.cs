using Orleans;
using SchrodingerServer.Common;
using SchrodingerServer.Grains.Grain.ZealyScore.Dtos;
using SchrodingerServer.Grains.State.ZealyScore;
using Volo.Abp.ObjectMapping;

namespace SchrodingerServer.Grains.Grain.ZealyScore;

public interface IXpRecordGrain : IGrainWithStringKey
{
    Task<GrainResultDto<XpRecordGrainDto>> CreateAsync(XpRecordGrainDto input);
    Task<GrainResultDto<XpRecordGrainDto>> GetAsync();
    Task<GrainResultDto<XpRecordGrainDto>> SettleAsync(string bizId);
    Task<GrainResultDto<XpRecordGrainDto>> SetFinalStatusAsync(string status);
}

public class XpRecordGrain : Grain<XpRecordState>, IXpRecordGrain
{
    private readonly IObjectMapper _objectMapper;

    public XpRecordGrain(IObjectMapper objectMapper)
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

    public async Task<GrainResultDto<XpRecordGrainDto>> CreateAsync(XpRecordGrainDto input)
    {
        var result = new GrainResultDto<XpRecordGrainDto>();
        if (!State.Id.IsNullOrEmpty() && State.Status == ContractInvokeStatus.ToBeCreated.ToString())
        {
            return new GrainResultDto<XpRecordGrainDto>()
            {
                Success = true,
                Data = _objectMapper.Map<XpRecordState, XpRecordGrainDto>(State)
            };
        }

        if (!State.Id.IsNullOrEmpty() && State.Status != ContractInvokeStatus.ToBeCreated.ToString())
        {
            result.Success = false;
            result.Message = "record already exist.";
            return result;
        }

        var userXpGrain = GrainFactory.GetGrain<IZealyUserXpGrain>(input.UserId);
        var updateResult = await userXpGrain.UpdateXpAsync(input.CurrentXp, input.Xp, input.Amount);
        if (!updateResult.Success)
        {
            result.Success = false;
            result.Message = updateResult.Message;
            return result;
        }

        State = _objectMapper.Map<XpRecordGrainDto, XpRecordState>(input);
        State.Id = this.GetPrimaryKeyString();
        State.Status = ContractInvokeStatus.ToBeCreated.ToString();
        State.CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        State.UpdateTime = State.CreateTime;

        await WriteStateAsync();

        return new GrainResultDto<XpRecordGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<XpRecordState, XpRecordGrainDto>(State)
        };
    }

    public Task<GrainResultDto<XpRecordGrainDto>> GetAsync()
    {
        var result = new GrainResultDto<XpRecordGrainDto>();
        if (State.Id.IsNullOrEmpty())
        {
            result.Success = false;
            result.Message = "record not exist.";
            return Task.FromResult(result);
        }

        return Task.FromResult(new GrainResultDto<XpRecordGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<XpRecordState, XpRecordGrainDto>(State)
        });
    }

    public async Task<GrainResultDto<XpRecordGrainDto>> SettleAsync(string bizId)
    {
        var result = new GrainResultDto<XpRecordGrainDto>();
        if (State.Id.IsNullOrEmpty())
        {
            result.Success = false;
            result.Message = "record not exist.";
            return result;
        }

        if (State.Status == ContractInvokeStatus.Pending.ToString())
        {
            return new GrainResultDto<XpRecordGrainDto>()
            {
                Success = true,
                Data = _objectMapper.Map<XpRecordState, XpRecordGrainDto>(State)
            };
        }

        if (State.Status != ContractInvokeStatus.ToBeCreated.ToString())
        {
            result.Success = false;
            result.Message = "record status is not ToBeCreated.";
            return result;
        }

        State.Status = ContractInvokeStatus.Pending.ToString();
        State.BizId = bizId;
        State.UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await WriteStateAsync();

        return new GrainResultDto<XpRecordGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<XpRecordState, XpRecordGrainDto>(State)
        };
    }

    public async Task<GrainResultDto<XpRecordGrainDto>> SetFinalStatusAsync(string status)
    {
        var result = new GrainResultDto<XpRecordGrainDto>();
        if (State.Id.IsNullOrEmpty())
        {
            result.Success = false;
            result.Message = "record not exist.";
            return result;
        }

        if (State.Status == ContractInvokeStatus.ToBeCreated.ToString() ||
            status == ContractInvokeStatus.ToBeCreated.ToString() ||
            status == ContractInvokeStatus.Pending.ToString())
        {
            result.Success = false;
            result.Message = "status can not change.";
            return result;
        }

        if (State.Status == ContractInvokeStatus.Success.ToString() ||
            State.Status == ContractInvokeStatus.FinalFailed.ToString())
        {
            return new GrainResultDto<XpRecordGrainDto>()
            {
                Success = true,
                Data = _objectMapper.Map<XpRecordState, XpRecordGrainDto>(State)
            };
        }

        if (State.Status == ContractInvokeStatus.FinalFailed.ToString())
        {
            // rollback user xp
            var userXpGrain = GrainFactory.GetGrain<IZealyUserXpGrain>(State.UserId);
            await userXpGrain.RollbackXpAsync();
        }

        State.Status = status;
        State.UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await WriteStateAsync();
        return new GrainResultDto<XpRecordGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<XpRecordState, XpRecordGrainDto>(State)
        };
    }
}