using Volo.Abp.EventBus;

namespace SchrodingerServer.Zealy.Eto;

[EventName("XpRecordEto")]
public class XpRecordEto
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public string Address { get; set; }
    public decimal Xp { get; set; }

    // xp * coefficient
    public decimal Amount { get; set; }
    public string BizId { get; set; }
    public string Status { get; set; }
    public long CreateTime { get; set; }
    public long UpdateTime { get; set; }
}