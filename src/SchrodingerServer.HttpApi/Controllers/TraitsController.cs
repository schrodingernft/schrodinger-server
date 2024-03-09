using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SchrodingerServer.Common;
using SchrodingerServer.Dtos.TraitsDto;
using SchrodingerServer.Traits;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace SchrodingerServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("traits")]
[Route("api/app/traits")]
public class ImageGenerationController : AbpController
{

    private readonly ITraitsActionProvider _traitsActionProvider;
    
    public ImageGenerationController(ITraitsActionProvider traitsActionProvider)
    {
        _traitsActionProvider = traitsActionProvider;
    }

    [HttpGet("image-generate")]
    public async Task<GenerateImageResponse?> ImageGenerationAsync(GenerateImageRequest generateImageRequest)
    {
        var domainValid = await _traitsActionProvider.ImageGenerateAsync(generateImageRequest.AdoptId);
        AssertHelper.IsTrue(domainValid, "Invalid host");
        return new GenerateImageResponse();
    }
}