using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using SchrodingerServer.Common;
using SchrodingerServer.Options;
using SchrodingerServer.Users.Index;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.Background.Providers;

public interface ICallContractProvider
{
    Task CreateAsync(ZealyUserXpIndex zealyUserXp, decimal xp);
}

public class CallContractProvider : ICallContractProvider, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly INESTRepository<ZealyUserXpRecordIndex, string> _zealyUserXpRecordRepository;
    private readonly INESTRepository<ZealyUserXpIndex, string> _zealyUserXpRepository;
    private readonly ZealyScoreOptions _options;
    private readonly ILogger<CallContractProvider> _logger;

    public CallContractProvider(INESTRepository<ZealyUserXpRecordIndex, string> zealyUserXpRecordRepository,
        IOptionsSnapshot<ZealyScoreOptions> options, INESTRepository<ZealyUserXpIndex, string> zealyUserXpRepository,
        ILogger<CallContractProvider> logger)
    {
        _zealyUserXpRecordRepository = zealyUserXpRecordRepository;
        _zealyUserXpRepository = zealyUserXpRepository;
        _logger = logger;
        _options = options.Value;
    }

    [AutomaticRetry(Attempts = 5, DelaysInSeconds = new[] { 10 })]
    public async Task CreateAsync(ZealyUserXpIndex zealyUserXp, decimal xp)
    {
        var grainId = "";
        // var grain = _clusterClient.GetGrain<object>(grainId);
        // grain.CreateAsync();

        // create success

        // update record
        var record = new ZealyUserXpRecordIndex
        {
            Id = grainId,
            CreateTime = DateTime.UtcNow,
            Xp = xp,
            Amount = xp * _options.Coefficient,
            Status = "pending",
            UserId = zealyUserXp.Id,
            Address = zealyUserXp.Address
        };

        await _zealyUserXpRecordRepository.AddOrUpdateAsync(record);
        BackgroundJob.Schedule(() => SearchAsync(record, zealyUserXp), TimeSpan.FromSeconds(10));

        _logger.LogInformation("in create: {time}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    [AutomaticRetry(Attempts = 100, DelaysInSeconds = new[] { 10 })]
    private async Task SearchAsync(ZealyUserXpRecordIndex record, ZealyUserXpIndex zealyUserXp)
    {
        // var grainId = record.Id;
        // var grain = _clusterClient.GetGrain<object>(grainId);

        // if(grainResult.success)
        //{
        //  update record, update xp
        //}
        //

        record.Status = "";//TransactionStatusType.Success.ToString();
        record.UpdateTime = DateTime.UtcNow;

        zealyUserXp.LastXp = zealyUserXp.Xp;
        zealyUserXp.Xp = record.Xp;
        zealyUserXp.UpdateTime = DateTime.UtcNow;

        await _zealyUserXpRepository.AddOrUpdateAsync(zealyUserXp);
        await _zealyUserXpRecordRepository.AddOrUpdateAsync(record);
    }
}