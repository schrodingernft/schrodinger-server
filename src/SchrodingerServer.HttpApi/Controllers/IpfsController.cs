using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SchrodingerServer.Config;
using SchrodingerServer.Ipfs;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace SchrodingerServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("ConfigController")]
[Route("api/app/ipfs")]
public class IpfsController : AbpController
{
    private readonly IIpfsAppService _ipfsAppService;

    public IpfsController(IIpfsAppService ipfsAppService)
    {
        _ipfsAppService = ipfsAppService;
    }

    [HttpGet]
    public async Task<string?> TestIpfs()
    {
        return await _ipfsAppService.TestIpfs();
    }
   
}