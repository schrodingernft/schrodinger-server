namespace SchrodingerServer.Options;

public class UpdateScoreOptions
{
    public string RecurringCorn { get; set; } = "0 0 0 * * ?";
    public string PriceRecurringCorn { get; set; } = "0 0 0 * * ?";
    
    // minutes
    public int UpdateXpScoreResultPeriod { get; set; } = 20;
    public int FetchPendingCount { get; set; } = 300;
}