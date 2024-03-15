using System.Collections.Generic;
using SchrodingerServer.Common;

namespace SchrodingerServer.Users.Index;

public class IndexerHolderDailyChanges : IndexerCommonResult<IndexerHolderDailyChanges>
{
    public long TotalRecordCount { get; set; }

    public List<HolderDailyChangeDto> DataList { get; set; }
}

public class HolderDailyChangeDto
{
    public string Address { get; set; }
    public string Symbol { get; set; }
    public string Date { get; set; }
    public long ChangeAmount { get; set; }
    public long Balance { get; set; }
}