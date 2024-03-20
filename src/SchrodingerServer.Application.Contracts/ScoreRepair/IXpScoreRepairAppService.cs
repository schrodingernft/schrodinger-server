using System.Collections.Generic;
using System.Threading.Tasks;
using SchrodingerServer.ScoreRepair.Dtos;

namespace SchrodingerServer.ScoreRepair;

public interface IXpScoreRepairAppService
{
    Task UpdateScoreRepairDataAsync(List<UpdateXpScoreRepairDataDto> input);
    Task<XpScoreRepairDataPageDto> GetXpScoreRepairDataAsync(XpScoreRepairDataRequestDto input);
    Task<UserXpInfoDto> GetUserXpAsync(string userId, string address);
    Task<XpRecordPageResultDto> GetUserRecordsAsync(string userId, int skipCount, int maxResultCount);
}