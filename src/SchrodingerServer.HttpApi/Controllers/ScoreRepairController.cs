using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchrodingerServer.ScoreRepair;
using SchrodingerServer.ScoreRepair.Dtos;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace SchrodingerServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("XpScoreRepair")]
[Route("api/app/repair")]
public class ScoreRepairController : AbpControllerBase
{
    private readonly IXpScoreRepairAppService _repairAppService;

    public ScoreRepairController(IXpScoreRepairAppService repairAppService)
    {
        _repairAppService = repairAppService;
    }

    [Authorize(Roles = "admin")]
    [HttpPost("xp-score")]
    public async Task UpdateScoreRepairDataAsync(List<UpdateXpScoreRepairDataDto> input)
    {
        await _repairAppService.UpdateScoreRepairDataAsync(input);
    }

    [HttpGet("xp-infos")]
    public async Task<XpScoreRepairDataPageDto> GetXpScoreRepairDataAsync(XpScoreRepairDataRequestDto input)
    {
        return await _repairAppService.GetXpScoreRepairDataAsync(input);
    }
}