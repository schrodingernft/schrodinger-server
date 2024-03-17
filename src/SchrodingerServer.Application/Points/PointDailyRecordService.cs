using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Orleans;
using SchrodingerServer.Common;
using SchrodingerServer.Grains.Grain.Points;
using SchrodingerServer.Options;
using SchrodingerServer.Users.Eto;
using SchrodingerServer.Users.Index;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace SchrodingerServer.Points;

public interface IPointDailyRecordService
{
    Task HandlePointDailyChangeAsync(string chainId, HolderDailyChangeDto dto, decimal? symbolPrice);
}

public class PointDailyRecordService : IPointDailyRecordService, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<PointDailyRecordService> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IOptionsMonitor<PointTradeOptions> _pointTradeOptions;

    public PointDailyRecordService(IClusterClient clusterClient, ILogger<PointDailyRecordService> logger,
        IObjectMapper objectMapper, IDistributedEventBus distributedEventBus,
        IOptionsMonitor<PointTradeOptions> pointTradeOptions)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _objectMapper = objectMapper;
        _distributedEventBus = distributedEventBus;
        _pointTradeOptions = pointTradeOptions;
    }

    public async Task HandlePointDailyChangeAsync(string chainId, HolderDailyChangeDto dto, decimal? symbolPrice)
    {
        if (dto == null)
        {
            return;
        }

        foreach (var (pointName, pointInfo) in _pointTradeOptions.CurrentValue.PointMapping)
        {
            if (pointInfo.ConditionalExp.IsNullOrEmpty())
            {
                continue;
            }

            var match = Regex.Match(dto.Symbol, pointInfo.ConditionalExp);

            if (!match.Success)
            {
                continue;
            }

            var input = new PointDailyRecordGrainDto()
            {
                ChainId = chainId,
                PointName = pointName,
                BizDate = dto.Date,
                Address = dto.Address,
                PointAmount = DecimalHelper.Divide(CalcPointAmount(dto, pointInfo, symbolPrice), 8)
            };
            input.Id = IdGenerateHelper.GetPointDailyRecord(chainId, input.BizDate, input.PointName, input.Address);
            var pointDailyRecordGrain = _clusterClient.GetGrain<IPointDailyRecordGrain>(input.Id);
            var result = await pointDailyRecordGrain.UpdateAsync(input);
            if (!result.Success)
            {
                _logger.LogError(
                    "Handle Point Daily Record fail, id: {id}.", input.Id);
                throw new UserFriendlyException($"Create Contract Invoke fail, id: {input.Id}.");
            }

            await _distributedEventBus.PublishAsync(
                _objectMapper.Map<PointDailyRecordGrainDto, PointDailyRecordEto>(result.Data));
        }
    }

    private decimal CalcPointAmount(HolderDailyChangeDto dto, PointInfo pointInfo, decimal? symbolPrice)
    {
        if (pointInfo.UseBalance)
        {
            return dto.Balance;
        }

        if (pointInfo.Factor == null)
        {
            return dto.ChangeAmount;
        }

        var pointAmount = (decimal)(dto.ChangeAmount * pointInfo.Factor);

        if (pointInfo.NeedMultiplyPrice && symbolPrice != null)
        {
            return (decimal)(pointAmount * symbolPrice);
        }

        return pointAmount;
    }
}