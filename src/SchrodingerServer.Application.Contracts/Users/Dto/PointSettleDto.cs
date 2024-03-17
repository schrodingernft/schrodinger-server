using System.Collections.Generic;

namespace SchrodingerServer.Users.Dto;

public class PointSettleDto
{
    public string ChainId { get; set; }
    public string BizId { get; set; }

    public string PointName { get; set; }

    public List<UserPointInfo> UserPointsInfos { get; set; }
}

public class UserPointInfo
{
    public string Address { get; set; }

    public decimal PointAmount { get; set; }
}