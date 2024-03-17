namespace SchrodingerServer.Options;

public class UpdateScoreOptions
{
    public string RecurringCorn { get; set; } = "0 0 0 * * ?";
    
    // minutes
    public int UpdateXpScoreResultPeriod { get; set; } = 20;
}