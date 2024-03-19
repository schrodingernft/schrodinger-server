using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SchrodingerServer.Dtos.TraitsDto;
using SchrodingerServer.Image;
using SchrodingerServer.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace SchrodingerServer.Adopts.dispatcher;

public interface IImageProvider
{
    ProviderType Type { get; }

    Task PublishAsync(string requestId, string adoptId, GenerateImage imageInfo);
    
    Task SendAIGenerationRequest(string adoptAddressId, string adoptId, GenerateImage imageInfo);

    Task SetRequestId(string adoptAddress, string requestId);

    Task SetAIGeneratedImages(string adoptId, List<string> images);

    Task<List<string>> GetAIGeneratedImages(string adoptId, string adoptAddressId);
}

public abstract class ImageProvider : IImageProvider
{
    public abstract ProviderType Type { get; }
    protected readonly IAdoptImageService AdoptImageService;
    protected readonly ILogger<ImageProvider> Logger;
    protected readonly IDistributedEventBus DistributedEventBus;

    protected ImageProvider(ILogger<ImageProvider> logger, IAdoptImageService adoptImageService, IDistributedEventBus distributedEventBus)
    {
        Logger = logger;
        AdoptImageService = adoptImageService;
        DistributedEventBus = distributedEventBus;
    }

    public async Task SetRequestId(string adoptAddressId, string requestId)
    {
        await AdoptImageService.SetImageGenerationIdNXAsync(adoptAddressId, requestId);
    }
    
    public async Task SendAIGenerationRequest(string adoptAddressId, string adoptId, GenerateImage imageInfo)
    {
        await PublishAsync(adoptAddressId, adoptId, imageInfo);
    }

    public async Task SetAIGeneratedImages(string adoptId, List<string> images)
    {
        await AdoptImageService.SetImagesAsync(adoptId, images);
    }
    
    // public abstract Task<List<string>> GenerateImageAsync(string adoptId, GenerateImage imageInfo);
    public abstract Task PublishAsync(string requestId, string adoptId, GenerateImage imageInfo);
    
    public abstract Task<List<string>> GetAIGeneratedImages(string adoptId, string adoptAddressId);
}

public enum ProviderType
{
    AutoMatic,
    Default
}

public class AutoMaticImageProvider : ImageProvider, ISingletonDependency
{
    public override ProviderType Type { get; } = ProviderType.AutoMatic;
    private readonly IOptionsMonitor<TraitsOptions> _traitsOptions;
    private readonly IOptionsMonitor<StableDiffusionOption> _stableDiffusionOption;

    public AutoMaticImageProvider(ILogger<ImageProvider> logger, IAdoptImageService adoptImageService,
        IDistributedEventBus distributedEventBus, IOptionsMonitor<TraitsOptions> traitsOptions, IOptionsMonitor<StableDiffusionOption> stableDiffusionOption) : base(logger, adoptImageService, distributedEventBus)
    {
        _traitsOptions = traitsOptions;
        _stableDiffusionOption = stableDiffusionOption;
    }

    public async Task<List<string>> RequestGenerateImage(string adoptId, GenerateImage imageInfo)
    {
        Logger.LogInformation("GenerateImageAsyncAsync Begin. adoptId: {adoptId} ", adoptId);
        var response = await QueryImageInfoByAiAsync(adoptId, imageInfo);
        var images = new List<string>();
        Logger.LogInformation("GenerateImageAsyncAsync Finish. resp: {resp}", JsonConvert.SerializeObject(response));
        if (response == null || response.images == null || response.images.Count == 0)
        {
            Logger.LogInformation("AutoMaticImageProvider GetImagesAsync autoMaticResponse.images null");
            return images;
        }

        images = response.images;

        await AdoptImageService.SetImagesAsync(adoptId, images);
        return images;
    }

    private QueryAutoMaticImage GetQueryAutoMaticImage(GenerateImage imageInfo)
    {
        var traits = imageInfo.baseImage.attributes.Concat(imageInfo.newAttributes).ToList();
        var diffusedOption = _stableDiffusionOption.CurrentValue;
        return new QueryAutoMaticImage()
        {
            traits = traits,
            seed = imageInfo.seed,
            sampler_index = diffusedOption.SamplerIndex,
            nagative_prompt = diffusedOption.NagativePrompt,
            step = diffusedOption.Step,
            batch_size = diffusedOption.BatchSize,
            width = diffusedOption.Width,
            height = diffusedOption.Height,
            n_iters = diffusedOption.NIters
        };
    }

    public async Task<QueryAutoMaticResponse> QueryImageInfoByAiAsync(string adoptId, GenerateImage imageInfo)
    {
        var queryImage = GetQueryAutoMaticImage(imageInfo);
        var jsonString = ImageProviderHelper.ConvertObjectToJsonString(queryImage);
        using var httpClient = new HttpClient();
        var requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
        httpClient.DefaultRequestHeaders.Add("accept", "*/*");
        var start = DateTime.Now;
        var response = await httpClient.PostAsync(_traitsOptions.CurrentValue.AutoMaticImageGenerateUrl, requestContent);
        var timeCost = (DateTime.Now - start).Milliseconds;
        var responseContent = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            var aiQueryResponse = JsonConvert.DeserializeObject<QueryAutoMaticResponse>(responseContent);
            Logger.LogInformation("AutoMaticImageProvider QueryImageInfoByAiAsync query success {adoptId} timeCost={timeCost}", adoptId, timeCost);
            return aiQueryResponse;
        }
        else
        {
            Logger.LogError("AutoMaticImageProvider QueryImageInfoByAiAsync query not success {adoptId} timeCost={timeCost}", adoptId, timeCost);
            return new QueryAutoMaticResponse { };
        }
    }

    public override async Task PublishAsync(string adoptAddressId, string adoptId, GenerateImage imageInfo)
    {
        await DistributedEventBus.PublishAsync(new AutoMaticImageGenerateEto() { AdoptAddressId = adoptAddressId, AdoptId = adoptId, GenerateImage = imageInfo });
    }
    
    public override async Task<List<string>> GetAIGeneratedImages(string adoptId, string adoptAddressId)
    {
        var images = await AdoptImageService.GetImagesAsync(adoptId);
        return images;
    }
}

public class DefaultImageProvider : ImageProvider, ISingletonDependency
{
    public override ProviderType Type { get; } = ProviderType.Default;
    private readonly IOptionsMonitor<TraitsOptions> _traitsOptions;

    public DefaultImageProvider(ILogger<ImageProvider> logger, IAdoptImageService adoptImageService, IOptionsMonitor<TraitsOptions> traitsOptions,
        IDistributedEventBus distributedEventBus) : base(logger, adoptImageService, distributedEventBus)
    {
        _traitsOptions = traitsOptions;
    }

    public async Task<string> RequestGenerateImage(string adoptId, GenerateOpenAIImage imageInfo)
    {
        try
        {
            using var httpClient = new HttpClient();
            var jsonString = ImageProviderHelper.ConvertObjectToJsonString(imageInfo);
            var requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
            httpClient.DefaultRequestHeaders.Add("accept", "*/*");

            var response = await httpClient.PostAsync(_traitsOptions.CurrentValue.ImageGenerateUrl, requestContent);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                Logger.LogInformation("TraitsActionProvider GenerateImageByAiAsync generate success adopt id:" + adoptId);
                // save adopt id and request id to grain
                GenerateImageFromAiRes aiQueryResponse = JsonConvert.DeserializeObject<GenerateImageFromAiRes>(responseString);
                return aiQueryResponse.requestId;
            }
            else
            {
                Logger.LogError("TraitsActionProvider GenerateImageByAiAsync generate error {adoptId}", adoptId);
            }

            return "";
        }
        catch (Exception e)
        {
            Logger.LogError(e, "TraitsActionProvider GenerateImageByAiAsync generate exception {adoptId}", adoptId);
            return "";
        }
    }
    
    public  async Task<List<string>> QueryImages(string requestId)
    {
        Logger.LogInformation("QueryImageInfoByAiAsync Begin. requestId: {requestId}", requestId);
        var aiQueryResponse = await QueryImageInfoByAiAsync(requestId);
        var images = new List<string>();
        Logger.LogInformation("QueryImageInfoByAiAsync Finish. resp: {resp}", JsonConvert.SerializeObject(aiQueryResponse));
        if (aiQueryResponse == null || aiQueryResponse.images == null || aiQueryResponse.images.Count == 0)
        {
            Logger.LogInformation("TraitsActionProvider GetImagesAsync aiQueryResponse.images null");
            return images;
        }

        images = aiQueryResponse.images.Select(imageItem => imageItem.image).ToList();
        return images;
    }

    public override async Task PublishAsync(string adoptAddressId, string adoptId, GenerateImage imageInfo)
    {
        await DistributedEventBus.PublishAsync(new DefaultImageGenerateEto() { AdoptAddressId = adoptAddressId, AdoptId = adoptId, GenerateImage = imageInfo });
    }

    private async Task<AiQueryResponse> QueryImageInfoByAiAsync(string requestId)
    {
        var queryImage = new QueryImage
        {
            requestId = requestId
        };
        var jsonString = ImageProviderHelper.ConvertObjectToJsonString(queryImage);
        using var httpClient = new HttpClient();
        var requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
        httpClient.DefaultRequestHeaders.Add("accept", "*/*");
        var response = await httpClient.PostAsync(_traitsOptions.CurrentValue.ImageQueryUrl, requestContent);
        if (response.IsSuccessStatusCode)
        {
            string responseContent = await response.Content.ReadAsStringAsync();
            AiQueryResponse aiQueryResponse = JsonConvert.DeserializeObject<AiQueryResponse>(responseContent);
            Logger.LogInformation("TraitsActionProvider QueryImageInfoByAiAsync query success {requestId}", requestId);
            return aiQueryResponse;
        }
        Logger.LogError("TraitsActionProvider QueryImageInfoByAiAsync query not success {requestId}", requestId);
        return new AiQueryResponse { };
    }

    public override async Task<List<string>> GetAIGeneratedImages(string adoptId, string adoptAddressId)
    {
        var images = await AdoptImageService.GetImagesAsync(adoptId);
        if (images.IsNullOrEmpty())
        {
            var requestId = await AdoptImageService.GetRequestIdAsync(adoptAddressId);
            if (string.IsNullOrEmpty(requestId))
            {
                return images;
            }
            images = await QueryImages(requestId);
            
            if (!images.IsNullOrEmpty())
            {
                await SetAIGeneratedImages(adoptId, images);
            }
        }

        return images;
    }
}