using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchrodingerServer.Background.Providers;
using SchrodingerServer.Common;
using SchrodingerServer.Options;
using SchrodingerServer.Points;
using SchrodingerServer.Users.Dto;
using SchrodingerServer.Zealy;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.Background.Services;

public interface IXpScoreSettleService
{
    Task BatchSettleAsync();
}

public class XpScoreSettleService : IXpScoreSettleService, ISingletonDependency
{
    private readonly ILogger<XpScoreSettleService> _logger;
    private readonly IPointSettleService _pointSettleService;
    private readonly IZealyUserXpRecordProvider _recordProvider;
    private readonly INESTRepository<ZealyUserXpRecordIndex, string> _zealyUserXpRecordRepository;
    private readonly ZealyScoreOptions _options;
    private readonly UpdateScoreOptions _updateScoreOptions;

    public XpScoreSettleService(ILogger<XpScoreSettleService> logger, IOptionsSnapshot<ZealyScoreOptions> options,
        IPointSettleService pointSettleService,
        INESTRepository<ZealyUserXpRecordIndex, string> zealyUserXpRecordRepository,
        IZealyUserXpRecordProvider recordProvider, IOptionsSnapshot<UpdateScoreOptions> updateScoreOptions)
    {
        _logger = logger;
        _pointSettleService = pointSettleService;
        _zealyUserXpRecordRepository = zealyUserXpRecordRepository;
        _recordProvider = recordProvider;
        _updateScoreOptions = updateScoreOptions.Value;
        _options = options.Value;
    }

    public async Task BatchSettleAsync()
    {
        var records = await _recordProvider.GetToCreateRecordAsync(0, _updateScoreOptions.FetchSettleCount);
        if (records.IsNullOrEmpty())
        {
            _logger.LogInformation("No record to settle");
            return;
        }

        _logger.LogInformation("need to settle records count:{count}", records.Count);
        await RecordBatchSettleAsync(0, records);
    }

    private async Task RecordBatchSettleAsync(int skipCount, List<ZealyUserXpRecordIndex> records)
    {
        var bizId = $"{Guid.NewGuid().ToString()}-{DateTime.UtcNow:yyyy-MM-dd}";
        var settleRecords = records.Skip(skipCount).Take(_updateScoreOptions.SettleCount).ToList();
        if (settleRecords.Count == 0)
        {
            return;
        }

        await BatchSettleAsync(bizId, settleRecords);

        skipCount = skipCount + _updateScoreOptions.SettleCount;
        await RecordBatchSettleAsync(skipCount, records);
    }

    private async Task BatchSettleAsync(string bizId, List<ZealyUserXpRecordIndex> records)
    {
        var points = new List<UserPointInfo>();
        var pointSettleDto = new PointSettleDto()
        {
            ChainId = _options.ChainId,
            BizId = bizId,
            PointName = _options.PointName
        };

        foreach (var record in records)
        {
            if (record.Status != ContractInvokeStatus.ToBeCreated.ToString())
            {
                _logger.LogWarning("record already handle, bizId:{bizId}", bizId);
                continue;
            }

            record.BizId = bizId;
            record.Status = ContractInvokeStatus.Pending.ToString();

            points.Add(new UserPointInfo()
            {
                Address = record.Address,
                PointAmount = record.Amount
            });
        }

        pointSettleDto.UserPointsInfos = points;
        try
        {
            await _pointSettleService.BatchSettleAsync(pointSettleDto);
        }
        catch (Exception e)
        {
            records.ForEach(t => { t.Status = ContractInvokeStatus.Failed.ToString(); });
            _logger.LogError(e, "settle error, bizId:{bizId}", bizId);
        }

        await _zealyUserXpRecordRepository.BulkAddOrUpdateAsync(records);
        _logger.LogInformation("BatchSettle finish, bizId:{bizId}", bizId);
    }
}