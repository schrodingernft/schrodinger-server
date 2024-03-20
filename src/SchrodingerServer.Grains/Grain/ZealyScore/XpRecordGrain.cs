using Microsoft.Extensions.Logging;
using Orleans;
using SchrodingerServer.Common;
using SchrodingerServer.Grains.Grain.ZealyScore.Dtos;
using SchrodingerServer.Grains.State.ZealyScore;
using Volo.Abp.ObjectMapping;

namespace SchrodingerServer.Grains.Grain.ZealyScore;

public interface IXpRecordGrain : IGrainWithStringKey
{
    Task<GrainResultDto<XpRecordGrainDto>> CreateAsync(XpRecordGrainDto input);

    Task<GrainResultDto<XpRecordGrainDto>> HandleRecordAsync(RecordInfo input, string userId,
        string address);

    Task<GrainResultDto<XpRecordGrainDto>> GetAsync();
    Task<GrainResultDto<XpRecordGrainDto>> SetStatusToPendingAsync(string bizId);
    Task<GrainResultDto<XpRecordGrainDto>> SetFinalStatusAsync(string status);
}

public class XpRecordGrain : Grain<XpRecordState>, IXpRecordGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<XpRecordGrain> _logger;

    public XpRecordGrain(IObjectMapper objectMapper, ILogger<XpRecordGrain> logger)
    {
        _objectMapper = objectMapper;
        _logger = logger;
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

        var userXpGrain = GrainFactory.GetGrain<IZealyUserXpGrain>(input.UserId);
        // update xp
        var updateResult = await userXpGrain.UpdateXpAsync(input.CurrentXp, input.Xp, input.Amount);
        if (!updateResult.Success)
        {
            result.Success = false;
            result.Message = updateResult.Message;
            return result;
        }

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

        State = _objectMapper.Map<XpRecordGrainDto, XpRecordState>(input);
        State.Id = this.GetPrimaryKeyString();
        State.Status = ContractInvokeStatus.ToBeCreated.ToString();
        State.CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        State.UpdateTime = State.CreateTime;
        await WriteStateAsync();

        // clear record info.
        try
        {
            await userXpGrain.ClearRecordInfo(DateTime.UtcNow.ToString("yyyy-MM-dd"));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "ClearRecordInfo error, userId:{userId}", State.UserId);
        }

        return new GrainResultDto<XpRecordGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<XpRecordState, XpRecordGrainDto>(State)
        };
    }

    public async Task<GrainResultDto<XpRecordGrainDto>> HandleRecordAsync(RecordInfo input, string userId,
        string address)
    {
        if (!State.Id.IsNullOrEmpty())
        {
            var recordDto = _objectMapper.Map<XpRecordState, XpRecordGrainDto>(State);
            recordDto.Status = ContractInvokeStatus.ToBeCreated.ToString();

            return new GrainResultDto<XpRecordGrainDto>()
            {
                Success = true,
                Data = recordDto
            };
        }

        State.Id = this.GetPrimaryKeyString();
        State.Xp = input.Xp;
        State.CurrentXp = input.CurrentXp;
        State.Amount = input.Amount;
        State.UserId = userId;
        State.Address = address;
        State.Status = ContractInvokeStatus.ToBeCreated.ToString();
        State.CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        State.UpdateTime = State.CreateTime;
        await WriteStateAsync();

        // update xp
        try
        {
            var userXpGrain = GrainFactory.GetGrain<IZealyUserXpGrain>(userId);
            await userXpGrain.ClearRecordInfo(input.Date);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "ClearRecordInfo error, userId:{userId}", State.UserId);
        }

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

    public async Task<GrainResultDto<XpRecordGrainDto>> SetStatusToPendingAsync(string bizId)
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

        State.Status = status;
        State.UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        if (State.Status == ContractInvokeStatus.FinalFailed.ToString())
        {
            // rollback user xp
            var userXpGrain = GrainFactory.GetGrain<IZealyUserXpGrain>(State.UserId);
            await userXpGrain.RollbackXpAsync();
        }
        
        await WriteStateAsync();
        return new GrainResultDto<XpRecordGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<XpRecordState, XpRecordGrainDto>(State)
        };
    }
}