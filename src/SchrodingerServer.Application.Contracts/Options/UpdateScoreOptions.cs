namespace SchrodingerServer.Options;

public class UpdateScoreOptions
{
    public string RecurringCorn { get; set; } = "0 0 0 * * ?";
    
    // minutes
    public int UpdateXpScoreResultPeriod { get; set; } = 20;
    
    // minutes
    public int SettleXpScorePeriod { get; set; } = 3;
    public int FetchPendingCount { get; set; } = 300;
    public int FetchSettleCount { get; set; } = 400;
    public int SettleCount { get; set; } = 20;
}