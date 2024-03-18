using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SchrodingerServer.Common;
using SchrodingerServer.Options;
using SchrodingerServer.Points;
using SchrodingerServer.Zealy;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.Background.Providers;

public interface ICallContractProvider
{
    Task CreateRecordAsync(ZealyUserXpIndex zealyUserXp, long useRepairTime, decimal xp);
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

    public async Task CreateRecordAsync(ZealyUserXpIndex zealyUserXp, long useRepairTime, decimal xp)
    {
        try
        {
            var recordId = $"{zealyUserXp.Id}-{DateTime.UtcNow:yyyy-MM-dd}";
            _logger.LogInformation("begin create, recordId:{recordId}", recordId);

            var record = new ZealyUserXpRecordIndex
            {
                Id = recordId,
                CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Xp = xp,
                Amount = DecimalHelper.MultiplyByPowerOfTen(xp * _options.Coefficient, 8),
                BizId = string.Empty,
                Status = ContractInvokeStatus.ToBeCreated.ToString(),
                UserId = zealyUserXp.Id,
                Address = zealyUserXp.Address,
                UseRepairTime = useRepairTime
            };
            await _zealyUserXpRecordRepository.AddOrUpdateAsync(record);
        }
        catch (Exception e)
        {
            _logger.LogError("create record error, data:{data}", JsonConvert.SerializeObject(zealyUserXp));
        }
    }
}