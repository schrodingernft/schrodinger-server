using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SchrodingerServer.Common;
using SchrodingerServer.Dtos.TraitsDto;
using SchrodingerServer.Image;
using SchrodingerServer.Traits;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace SchrodingerServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("traits")]
[Route("api/app/schrodinger")]
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
        return await _traitsActionProvider.ImageGenerateAsync(generateImageRequest.AdoptId);
    }
    [HttpGet("image")]
    public async Task<GetImageResponse> GetImageAsync(GetImageRequest getImageRequest)
    {
        return await _traitsActionProvider.GetImageAsync(getImageRequest.AdoptId);
    }
}
