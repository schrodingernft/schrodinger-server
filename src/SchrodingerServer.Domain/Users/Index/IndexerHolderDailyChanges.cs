using System;
using System.Collections.Generic;

namespace SchrodingerServer.Users.Index;


public class IndexerHolderDailyChangeDto
{
    public IndexerHolderDailyChanges GetSchrodingerHolderDailyChangeList { get; set; }
}

public class IndexerHolderDailyChanges
{
    public long TotalCount { get; set; }

    public List<HolderDailyChangeDto> Data { get; set; } = new();
}

public class HolderDailyChangeDto
{
    public string Address { get; set; }
    public string Symbol { get; set; }
    public string Date { get; set; }
    public long ChangeAmount { get; set; }
    public long Balance { get; set; }
}

public class IndexerHolderPointsSumsDto
{
    public IndexerHolderPointsSums GetPointsSumByAction { get; set; }
}
public class IndexerHolderPointsSums
{
    public long TotalCount { get; set; }

    public List<HolderPointsSumBDto> Data { get; set; } = new();
}
public class HolderPointsSumBDto
{
    public string Address { get; set; }
    public string PointsName { get; set; }
    public long Amount { get; set; }
}

public class RankingDetailIndexerQueryDto
{
    public RankingDetailIndexerListDto GetPointsSumByAction { get; set; }
}

public class RankingDetailIndexerListDto
{
    public List<RankingDetailIndexerDto> Data { get; set; }
    public long TotalRecordCount { get; set; }
}

public class RankingDetailIndexerDto
{
    public string Id { get; set; }
    public string Address { get; set; }
    public string Domain { get; set; }
    public string DappId { get; set; }
    public string PointsName { get; set; }
    public string ActionName { get; set; }
    public decimal Amount { get; set; }
    public string SymbolName { get; set; }

    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}