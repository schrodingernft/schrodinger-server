namespace SchrodingerServer.Worker.Core.Options;

public class WorkerOptions
{
    public int SearchTimer { get; set; } = 10;
    public string SyncSourceChainId { get; set; } = "tDVW";
    public long BackFillBatchSize { get; set; } = 9999;
    public long SubscribeStartHeight { get; set; } = 108700000;
    public int MaximumNumberPerTask { get; set; } = 10;
    public string SubscribeStartHeightGrainId { get; set; } = "StartHeight";
    public string SyncPendingListGrainId { get; set; } = "SyncPendingList";
}