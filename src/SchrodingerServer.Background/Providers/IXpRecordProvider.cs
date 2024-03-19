using System;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using SchrodingerServer.Common;
using SchrodingerServer.Grains.Grain.ZealyScore;
using SchrodingerServer.Grains.Grain.ZealyScore.Dtos;
using SchrodingerServer.Options;
using SchrodingerServer.Zealy.Eto;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace SchrodingerServer.Background.Providers;

public interface IXpRecordProvider
{
    Task CreateRecordAsync(string userId, string address, decimal currentXp, decimal xp);
}

public class XpRecordProvider : IXpRecordProvider, ISingletonDependency
{
    private readonly ZealyScoreOptions _options;
    private readonly ILogger<XpRecordProvider> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedEventBus _distributedEventBus;

    public XpRecordProvider(
        IOptionsSnapshot<ZealyScoreOptions> options,
        ILogger<XpRecordProvider> logger,
        IClusterClient clusterClient, IObjectMapper objectMapper, IDistributedEventBus distributedEventBus)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _distributedEventBus = distributedEventBus;
        _options = options.Value;
    }

    [AutomaticRetry(Attempts = 20, DelaysInSeconds = new[] { 40 })]
    public async Task CreateRecordAsync(string userId, string address, decimal currentXp, decimal xp)
    {
        try
        {
            var recordId = $"{userId}-{DateTime.UtcNow:yyyy-MM-dd}";
            _logger.LogInformation("begin create, recordId:{recordId}", recordId);

            var recordDto = new XpRecordGrainDto
            {
                Id = recordId,
                Xp = xp,
                CurrentXp = currentXp,
                Amount = DecimalHelper.MultiplyByPowerOfTen(xp * _options.Coefficient, 8),
                BizId = string.Empty,
                Status = ContractInvokeStatus.ToBeCreated.ToString(),
                UserId = userId,
                Address = address
            };

            var recordGrain = _clusterClient.GetGrain<IXpRecordGrain>(recordId);
            var result = await recordGrain.CreateAsync(recordDto);

            if (!result.Success)
            {
                _logger.LogError(
                    "add record grain fail, message:{message}, userId:{userId}, address:{address}, xp:{xp}",
                    result.Message, userId, address, xp);
                return;
            }

            var recordEto = _objectMapper.Map<XpRecordGrainDto, XpRecordEto>(result.Data);
            await _distributedEventBus.PublishAsync(recordEto, false, false);
            _logger.LogInformation("end create record, recordId:{recordId}", recordId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "create record error, userId:{userId}, address:{address}, xp:{xp}", userId, address,
                xp);
            throw;
        }
    }
}