using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using SchrodingerServer.Common;
using SchrodingerServer.Grains.Grain.Synchronize;
using SchrodingerServer.Worker.Core.Options;
using SchrodingerServer.Worker.Core.Provider;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace SchrodingerServer.Worker.Core.Worker;

public class SyncWorker : AsyncPeriodicBackgroundWorkerBase
{
    private long _latestSubscribeHeight;
    private readonly ILogger<SyncWorker> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IIndexerProvider _indexerProvider;
    private readonly IOptionsMonitor<WorkerOptions> _options;

    public SyncWorker(AbpAsyncTimer timer, IOptionsMonitor<WorkerOptions> workerOptions, ILogger<SyncWorker> logger,
        IServiceScopeFactory serviceScopeFactory, IIndexerProvider indexerProvider, IClusterClient clusterClient) :
        base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _options = workerOptions;
        _clusterClient = clusterClient;
        _indexerProvider = indexerProvider;
        Timer.Period = 1000 * _options.CurrentValue.SearchTimer;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await ExecuteSearchAsync();
        // Query and execution need to ensure serialization
        await ExecuteSyncAsync();
    }

    private async Task ExecuteSearchAsync()
    {
        if (_latestSubscribeHeight == 0) await SearchWorkerInitializing();

        var chainId = _options.CurrentValue.SyncSourceChainId;
        var blockLatestHeight = await _indexerProvider.GetIndexBlockHeightAsync(chainId);
        if (blockLatestHeight <= _latestSubscribeHeight)
        {
            _logger.LogDebug("[Search] {chain} confirmed height hasn't been updated yet, will try later.", chainId);
            return;
        }

        var batchSize = _options.CurrentValue.BackFillBatchSize;
        var jobs = new List<string>();

        for (var from = _latestSubscribeHeight + 1; from <= blockLatestHeight; from += batchSize)
        {
            _logger.LogDebug("[Search] Next search window start {from}", from);
            var confirms = await _indexerProvider.SubscribeConfirmedAsync(chainId,
                Math.Min(from + batchSize - 1, blockLatestHeight), from);
            confirms = confirms.Where(s => !string.IsNullOrEmpty(s)).ToList();
            if (confirms.Count > 0) jobs.AddRange(confirms);
        }

        if (jobs.Count > 0) await AddOrUpdateConfirmedEventsAsync(jobs);

        await UpdateSubscribeHeightAsync(blockLatestHeight);
    }

    private async Task ExecuteSyncAsync()
    {
        var grainClient = _clusterClient.GetGrain<ISyncPendingGrain>(GenerateSyncPendingListGrainId());
        var pendingList = await grainClient.GetSyncPendingListAsync();
        var tasks = pendingList.Select(t =>
        {
            var jobGrain = _clusterClient.GetGrain<ISyncGrain>(t);
            return jobGrain.ExecuteJobAsync(new SyncJobGrainDto { Id = t });
        });

        // var jobGrain =
        //     _clusterClient.GetGrain<ISyncGrain>("bd9c5cf383629c5eed5de3cf91ad3285ed867f1f64f22f1d8511c26680114ba9");
        // var tasks = jobGrain.ExecuteJobAsync(new SyncJobGrainDto
        //     { Id = "bd9c5cf383629c5eed5de3cf91ad3285ed867f1f64f22f1d8511c26680114ba9" });
        //
        // var jobGrain =
        //     _clusterClient.GetGrain<ISyncGrain>("2b228df180515f26556d2694b9fbcc7ade746bdad60c82a1ff95b2989033c010");
        // var tasks = jobGrain.ExecuteJobAsync(new SyncJobGrainDto
        //     { Id = "2b228df180515f26556d2694b9fbcc7ade746bdad60c82a1ff95b2989033c010" });

        var tasksResults = await Task.WhenAll(tasks);
        var finishedJobs = new List<string>();
        var failedJobs = new List<string>();

        foreach (var result in tasksResults)
        {
            switch (result.Success)
            {
                case true when result.Data.Status == SyncJobStatus.CrossChainTokenCreated:
                    finishedJobs.Add(result.Data.TransactionId);
                    break;
                case false:
                    failedJobs.Add(result.Data.TransactionId);
                    break;
            }
        }

        var deleteSyncPendingList = finishedJobs.Concat(failedJobs).ToList();
        if (deleteSyncPendingList.Count > 0) await grainClient.DeleteSyncPendingList(deleteSyncPendingList);
    }

    private async Task UpdateSubscribeHeightAsync(long height)
    {
        _latestSubscribeHeight = height;
        await _clusterClient.GetGrain<ISubscribeGrain>(GenerateSubscribeHeightGrainId())
            .SetSubscribeHeightAsync(height);
    }

    private async Task AddOrUpdateConfirmedEventsAsync(List<string> events)
        => await _clusterClient.GetGrain<ISyncPendingGrain>(GenerateSyncPendingListGrainId())
            .AddOrUpdateSyncPendingList(events);

    private async Task SearchWorkerInitializing() => _latestSubscribeHeight = await _clusterClient
        .GetGrain<ISubscribeGrain>(GenerateSubscribeHeightGrainId()).GetSubscribeHeightAsync();

    private string GenerateSubscribeHeightGrainId() => GuidHelper.UniqGuid("SubscribeHeight").ToString();
    private string GenerateSyncPendingListGrainId() => GuidHelper.UniqGuid("SyncPendingList").ToString();
}