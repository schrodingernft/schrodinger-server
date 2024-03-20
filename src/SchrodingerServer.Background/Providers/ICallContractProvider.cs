using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchrodingerServer.Common;
using SchrodingerServer.Common.Options;
using SchrodingerServer.Points;
using SchrodingerServer.Users.Dto;
using SchrodingerServer.Zealy;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.Background.Providers;

public interface ICallContractProvider
{
    Task CreateAsync(ZealyUserXpIndex zealyUserXp, long useRepairTime, decimal xp);
}

public class CallContractProvider : ICallContractProvider, ISingletonDependency
{
    private readonly IPointSettleService _pointSettleService;
    private readonly INESTRepository<ZealyUserXpRecordIndex, string> _zealyUserXpRecordRepository;
    private readonly ZealyScoreOptions _options;
    private readonly ILogger<CallContractProvider> _logger;

    public CallContractProvider(INESTRepository<ZealyUserXpRecordIndex, string> zealyUserXpRecordRepository,
        IOptionsSnapshot<ZealyScoreOptions> options,
        ILogger<CallContractProvider> logger,
        IPointSettleService pointSettleService)
    {
        _zealyUserXpRecordRepository = zealyUserXpRecordRepository;
        _logger = logger;
        _pointSettleService = pointSettleService;
        _options = options.Value;
    }

    [AutomaticRetry(Attempts = 20, DelaysInSeconds = new[] { 30 })]
    public async Task CreateAsync(ZealyUserXpIndex zealyUserXp, long useRepairTime, decimal xp)
    {
        var bizId = $"{zealyUserXp.Id}-{DateTime.UtcNow:yyyy-MM-dd}";
        _logger.LogInformation("begin create, bizId:{bizId}", bizId);

        var pointSettleDto = new PointSettleDto()
        {
            ChainId = _options.ChainId,
            BizId = bizId,
            PointName = _options.PointName,
            UserPointsInfos = new List<UserPointInfo>()
            {
                new UserPointInfo()
                {
                    Address = zealyUserXp.Address,
                    PointAmount = xp * _options.Coefficient
                }
            }
        };

        await _pointSettleService.BatchSettleAsync(pointSettleDto);

        // update record
        var record = new ZealyUserXpRecordIndex
        {
            Id = bizId,
            CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Xp = xp,
            Amount =  DecimalHelper.MultiplyByPowerOfTen(xp * _options.Coefficient, 8),
            BizId = bizId,
            Status = ContractInvokeStatus.Pending.ToString(),
            UserId = zealyUserXp.Id,
            Address = zealyUserXp.Address,
            UseRepairTime = useRepairTime
        };

        await _zealyUserXpRecordRepository.AddOrUpdateAsync(record);
        _logger.LogInformation("end create, bizId:{bizId}", bizId);
    }
}