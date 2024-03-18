namespace SchrodingerServer.Grains.Grain.Synchronize;

public class SyncJobStatus
{
    public const string TokenValidating = "TokenValidating";
    public const string CrossChainTokenCreating = "CrossChainTokenCreating";
    public const string CrossChainTokenCreated = "CrossChainTokenCreated";
    public const string WaitingIndexing = "WaitingIndexing";
    public const string WaitingSideIndexing = "WaitingSideIndexing";
    public const string Failed = "Failed";
}