namespace SchrodingerServer.Worker.Core.Options;

public class WorkerOptions
{
    public int SearchTimer { get; set; } = 10;
    public string SyncSourceChainId { get; set; } = "tDVW";
    public long BackFillBatchSize { get; set; } = 9999;
}