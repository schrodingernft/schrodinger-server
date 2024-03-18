namespace SchrodingerServer.Worker.Core.Options;

public class WorkerOptions
{
    public int SearchTimer { get; set; } = 30;
    public int ExecuteTimer { get; set; } = 15;
    public string SyncSourceChainId { get; set; } = "tDVW";
    public string SyncTargetChainId { get; set; } = "AELF";
    public long BackFillBatchSize { get; set; } = 9999;
}