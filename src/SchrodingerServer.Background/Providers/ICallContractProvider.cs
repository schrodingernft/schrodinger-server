using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchrodingerServer.Options;
using SchrodingerServer.Points;
using SchrodingerServer.Users.Dto;
using SchrodingerServer.Zealy;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.Background.Providers;

public interface ICallContractProvider
{
    Task CreateAsync(ZealyUserXpIndex zealyUserXp, ZealyXpScoreIndex xpScore, decimal xp);
}

public class CallContractProvider : ICallContractProvider, ISingletonDependency
{
    private readonly IPointSettleService _pointSettleService;
    private readonly INESTRepository<ZealyUserXpRecordIndex, string> _zealyUserXpRecordRepository;
    private readonly INESTRepository<ZealyUserXpIndex, string> _zealyUserXpRepository;
    private readonly ZealyScoreOptions _options;
    private readonly ILogger<CallContractProvider> _logger;

    public CallContractProvider(INESTRepository<ZealyUserXpRecordIndex, string> zealyUserXpRecordRepository,
        IOptionsSnapshot<ZealyScoreOptions> options, INESTRepository<ZealyUserXpIndex, string> zealyUserXpRepository,
        ILogger<CallContractProvider> logger, IPointSettleService pointSettleService)
    {
        _zealyUserXpRecordRepository = zealyUserXpRecordRepository;
        _zealyUserXpRepository = zealyUserXpRepository;
        _logger = logger;
        _pointSettleService = pointSettleService;
        _options = options.Value;
    }

    [AutomaticRetry(Attempts = 5, DelaysInSeconds = new[] { 10 })]
    public async Task CreateAsync(ZealyUserXpIndex zealyUserXp, ZealyXpScoreIndex xpScore, decimal xp)
    {
        //var bizId = Guid.NewGuid() + "-" + DateTime.UtcNow.ToString("yyyy-MM-dd");
        var bizId = $"{zealyUserXp.Id}-{DateTime.UtcNow:yyyy-MM-dd}";

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

        var recordId = $"{bizId}:{zealyUserXp.Id}";

        // update record
        var record = new ZealyUserXpRecordIndex
        {
            Id = recordId,
            CreateTime = DateTime.UtcNow,
            Xp = xp,
            Amount = xp * _options.Coefficient,
            Status = "pending",
            UserId = zealyUserXp.Id,
            Address = zealyUserXp.Address
        };

        await _zealyUserXpRecordRepository.AddOrUpdateAsync(record);
        // BackgroundJob.Schedule(() => SearchAsync(record, zealyUserXp, xpScore), TimeSpan.FromSeconds(150));

        _logger.LogInformation("in create: {time}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    [AutomaticRetry(Attempts = 10, DelaysInSeconds = new[] { 20 })]
    private async Task SearchAsync(ZealyUserXpRecordIndex record, ZealyUserXpIndex zealyUserXp, ZealyXpScoreIndex xpScore)
    {
        // todo: get transaction status and update record

        if (xpScore != null)
        {
            // ...
            zealyUserXp.UseRepairTime = xpScore.UpdateTime;
        }
        record.Status = ""; //TransactionStatusType.Success.ToString();
        record.UpdateTime = DateTime.UtcNow;

        zealyUserXp.LastXp = zealyUserXp.Xp;
        zealyUserXp.Xp = record.Xp;
        zealyUserXp.UpdateTime = DateTime.UtcNow;

        await _zealyUserXpRepository.AddOrUpdateAsync(zealyUserXp);
        await _zealyUserXpRecordRepository.AddOrUpdateAsync(record);
    }
}