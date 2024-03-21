namespace SchrodingerServer.Worker.Core.Options;

public class IndexBlockHeightOptions
{
    public int SearchTimer { get; set; } = 3;
    public string TargetChainId { get; set; } = "AELF";
    public string SourceChainId { get; set; } = "tDVW";
    public string IndexBlockHeightGrainId { get; set; } = "IndexBlockHeight";
}