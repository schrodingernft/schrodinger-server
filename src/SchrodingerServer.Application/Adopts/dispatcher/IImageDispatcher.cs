using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SchrodingerServer.Dtos.TraitsDto;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.Adopts.dispatcher;

public interface IImageDispatcher
{
    Task<ImageGenerationIdDto> GetImageGenerationIdAsync(string aelfAddress, GenerateImage imageInfo, string adoptId);
}

public class ImageGenerationIdDto
{
    public string ImageGenerationId { get; set; }
    public bool Exist { get; set; }
}

public class ImageDispatcher : IImageDispatcher, ISingletonDependency
{
    private readonly AdoptImageOptions _adoptImageOptions;
    private readonly IAdoptImageService _adoptImageService;
    private readonly ILogger<ImageDispatcher> _logger;
    private readonly Dictionary<string, IImageProvider> _providers;

    public ImageDispatcher(AdoptImageOptions adoptImageOptions, IAdoptImageService adoptImageService, ILogger<ImageDispatcher> logger, IEnumerable<IImageProvider> providers)
    {
        _adoptImageOptions = adoptImageOptions;
        _adoptImageService = adoptImageService;
        _logger = logger;
        _providers = providers.ToDictionary(x => x.Type.ToString(), y => y);
    }

    public async Task<ImageGenerationIdDto> GetImageGenerationIdAsync(string aelfAddress, GenerateImage imageInfo, string adoptId)
    {
        var adoptAddress = ImageProviderHelper.JoinAdoptIdAndAelfAddress(adoptId, aelfAddress);
        var imageGenerationId = await _adoptImageService.GetImageGenerationIdAsync(adoptAddress);
        var imageGenerationIdDto = new ImageGenerationIdDto
        {
            ImageGenerationId = imageGenerationId,
            Exist = !string.IsNullOrEmpty(imageGenerationId)
        };
        if (imageGenerationIdDto.Exist)
        {
            return imageGenerationIdDto;
        }


        _logger.LogInformation("GenerateImageByAiAsync Begin. imageInfo: {info} adoptId: {adoptId} ", JsonConvert.SerializeObject(imageInfo), adoptId);
        if (!_providers.TryGetValue(_adoptImageOptions.ImageProvider, out var provider))
        {
            throw new UserFriendlyException("wrong type of image provider configuration");
        }

        await provider.GetRequestIdAsync(adoptAddress, imageInfo, adoptId);
        return imageGenerationIdDto;
    }
}