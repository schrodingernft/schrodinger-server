using System;
using System.Collections.Generic;

namespace SchrodingerServer.ScoreRepair.Dtos;

public class UserXpInfoDto
{
    public string Id { get; set; }
    public string Address { get; set; }

    public decimal LastXp { get; set; }
    public decimal CurrentXp { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
    public bool IsRollback { get; set; }
    public List<RecordInfoDto> RecordInfos { get; set; } = new();
}

public class RecordInfoDto
{
    public string Date { get; set; }
    public decimal CurrentXp { get; set; }
    public decimal Xp { get; set; }
    public decimal Amount { get; set; }
}