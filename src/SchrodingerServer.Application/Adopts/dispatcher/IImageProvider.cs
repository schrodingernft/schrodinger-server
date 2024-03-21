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

    Task SendAIGenerationRequestAsync(string adoptAddressId, string adoptId, GenerateImage imageInfo);

    Task SetRequestIdAsync(string adoptAddress, string requestId);

    Task SetAIGeneratedImagesAsync(string adoptId, List<string> images);

    Task<List<string>> GetAIGeneratedImagesAsync(string adoptId, string adoptAddressId);

    Task<bool> RequestIdIsNotNullOrEmptyAsync(string adoptAddressId);
}

public abstract class ImageProvider : IImageProvider
{
    public abstract ProviderType Type { get; }

    protected readonly HttpClient Client = new(new HttpClientHandler
    {
        UseProxy = false
    });

    protected readonly IAdoptImageService AdoptImageService;
    protected readonly ILogger<ImageProvider> Logger;
    protected readonly IDistributedEventBus DistributedEventBus;

    protected ImageProvider(ILogger<ImageProvider> logger, IAdoptImageService adoptImageService, IDistributedEventBus distributedEventBus)
    {
        Logger = logger;
        AdoptImageService = adoptImageService;
        DistributedEventBus = distributedEventBus;
    }

    public async Task SetRequestIdAsync(string adoptAddressId, string requestId)
    {
        await AdoptImageService.SetImageGenerationIdNXAsync(adoptAddressId, requestId);
    }

    public async Task SendAIGenerationRequestAsync(string adoptAddressId, string adoptId, GenerateImage imageInfo)
    {
        await PublishAsync(adoptAddressId, adoptId, imageInfo);
    }

    public async Task SetAIGeneratedImagesAsync(string adoptId, List<string> images)
    {
        await AdoptImageService.SetImagesAsync(adoptId, images);
    }

    // public abstract Task<List<string>> GenerateImageAsync(string adoptId, GenerateImage imageInfo);
    public abstract Task PublishAsync(string requestId, string adoptId, GenerateImage imageInfo);

    public abstract Task<List<string>> GetAIGeneratedImagesAsync(string adoptId, string adoptAddressId);
    public abstract Task<bool> RequestIdIsNotNullOrEmptyAsync(string adoptAddressId);
}

public enum ProviderType
{
    AutoMatic,
    Default
}

public class AutoMaticImageProvider : ImageProvider, ISingletonDependency
{
    public override ProviderType Type { get; } = ProviderType.AutoMatic;
    private readonly TraitsOptions _traitsOptions;
    private readonly StableDiffusionOption _stableDiffusionOption;

    public AutoMaticImageProvider(ILogger<ImageProvider> logger, IAdoptImageService adoptImageService,
        IDistributedEventBus distributedEventBus, IOptionsMonitor<TraitsOptions> traitsOptions, IOptionsMonitor<StableDiffusionOption> stableDiffusionOption) : base(logger, adoptImageService, distributedEventBus)
    {
        _traitsOptions = traitsOptions.CurrentValue;
        _stableDiffusionOption = stableDiffusionOption.CurrentValue;
    }

    public async Task<List<string>> RequestGenerateImage(string adoptId, GenerateImage imageInfo)
    {
        Logger.LogInformation("GenerateImageAsyncAsync Begin. adoptId: {adoptId} ", adoptId);
        var response = await QueryImageInfoByAiAsync(adoptId, imageInfo);
        var images = new List<string>();
        Logger.LogInformation("GenerateImageAsyncAsync Finish. resp: {resp}", response.info);
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
        return new QueryAutoMaticImage()
        {
            seed = imageInfo.seed,
            sampler_index = _stableDiffusionOption.SamplerIndex,
            negative_prompt = _stableDiffusionOption.NegativePrompt,
            steps = _stableDiffusionOption.Steps,
            batch_size = _stableDiffusionOption.BatchSize,
            width = _stableDiffusionOption.Width,
            height = _stableDiffusionOption.Height,
            n_iter = _stableDiffusionOption.NIter
        };
    }

    public string GetPrompt(GenerateImage imageInfo)
    {
        var prompt = new StringBuilder(_stableDiffusionOption.Prompt);
        foreach (var trait in imageInfo.baseImage.attributes.Concat(imageInfo.newAttributes).ToList())
        {
            prompt.Append(trait.traitType);
            prompt.Append(':');
            prompt.Append(trait.value);
            prompt.Append(',');
        }

        return prompt.ToString();
    }


    public async Task<QueryAutoMaticResponse> QueryImageInfoByAiAsync(string adoptId, GenerateImage imageInfo)
    {
        var queryImage = GetQueryAutoMaticImage(imageInfo);
        queryImage.prompt = GetPrompt(imageInfo);
        var jsonString = ImageProviderHelper.ConvertObjectToJsonString(queryImage);
        var requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
        Client.DefaultRequestHeaders.Add("accept", "*/*"); 
        var start = DateTime.Now;
        var response = await Client.PostAsync(_traitsOptions.AutoMaticImageGenerateUrl, requestContent);
        var timeCost = (DateTime.Now - start).TotalMilliseconds;
        var responseContent = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            var aiQueryResponse = JsonConvert.DeserializeObject<QueryAutoMaticResponse>(responseContent);
            var images = aiQueryResponse.images.Select(image => "data:image/webp;base64," + image).ToList();

            Logger.LogInformation("AutoMaticImageProvider QueryImageInfoByAiAsync query success {adoptId} requestContent={requestContent} timeCost={timeCost}", adoptId, jsonString, timeCost);
            return new QueryAutoMaticResponse() { images = images, info = aiQueryResponse.info };
        }
        else
        {
            Logger.LogError("AutoMaticImageProvider QueryImageInfoByAiAsync query failed {adoptId} timeCost={timeCost}", adoptId, timeCost);
            return new QueryAutoMaticResponse { };
        }
    }

    public override async Task PublishAsync(string adoptAddressId, string adoptId, GenerateImage imageInfo)
    {
        await DistributedEventBus.PublishAsync(new AutoMaticImageGenerateEto() { AdoptAddressId = adoptAddressId, AdoptId = adoptId, GenerateImage = imageInfo });
    }

    public override async Task<List<string>> GetAIGeneratedImagesAsync(string adoptId, string adoptAddressId)
    {
        var images = await AdoptImageService.GetImagesAsync(adoptId);
        return images;
    }

    public override Task<bool> RequestIdIsNotNullOrEmptyAsync(string adoptAddressId)
    {
        return Task.FromResult(true);
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
            var jsonString = ImageProviderHelper.ConvertObjectToJsonString(imageInfo);
            var requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
            Client.DefaultRequestHeaders.Add("accept", "*/*");

            var response = await Client.PostAsync(_traitsOptions.CurrentValue.ImageGenerateUrl + "/" + adoptId, requestContent);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var aiQueryResponse = JsonConvert.DeserializeObject<GenerateImageFromAiRes>(responseString);
                Logger.LogInformation("TraitsActionProvider GenerateImageByAiAsync generate success adopt id:" + adoptId + " requestId:" + aiQueryResponse.requestId);
                return aiQueryResponse.requestId;
            }
            else
            {
                Logger.LogError("TraitsActionProvider GenerateImageByAiAsync generate error {adoptId} response{response}", adoptId, responseString);
            }

            return "";
        }
        catch (Exception e)
        {
            Logger.LogError(e, "TraitsActionProvider GenerateImageByAiAsync generate exception {adoptId}", adoptId);
            return "";
        }
    }

    public async Task<List<string>> QueryImages(string requestId)
    {
        Logger.LogInformation("QueryImageInfoByAiAsync Begin. requestId: {requestId}", requestId);
        var aiQueryResponse = await GetImagesByAiAsync(requestId);
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

    private async Task<AiQueryResponse> GetImagesByAiAsync(string requestId)
    {
        Client.DefaultRequestHeaders.Add("accept", "*/*");
        var start = DateTime.Now;
        var response = await Client.GetAsync(_traitsOptions.CurrentValue.ImageQueryUrl + "/" + requestId);
        var timeCost = (DateTime.Now - start).TotalMilliseconds;
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var aiQueryResponse = JsonConvert.DeserializeObject<AiQueryResponse>(responseContent);
            Logger.LogInformation("TraitsActionProvider QueryImageInfoByAiAsync query success {requestId} timeCost={timeCost}", requestId, timeCost);
            return aiQueryResponse;
        }

        Logger.LogError("TraitsActionProvider QueryImageInfoByAiAsync query not success {requestId}", requestId);
        return new AiQueryResponse { };
    }

    public override async Task<List<string>> GetAIGeneratedImagesAsync(string adoptId, string adoptAddressId)
    {
        var images = await AdoptImageService.GetImagesAsync(adoptId);
        if (images.IsNullOrEmpty())
        {
            var requestId = await AdoptImageService.GetRequestIdAsync(adoptAddressId);
            Logger.LogInformation("GetImagesAsync requestId: {adoptId} {requestId}", adoptId, requestId);
            if (string.IsNullOrEmpty(requestId))
            {
                return images;
            }

            images = await QueryImages(requestId);

            if (!images.IsNullOrEmpty())
            {
                await SetAIGeneratedImagesAsync(adoptId, images);
            }
        }

        return images;
    }

    public override async Task<bool> RequestIdIsNotNullOrEmptyAsync(string adoptAddressId)
    {
        var requestId = await AdoptImageService.GetRequestIdAsync(adoptAddressId);
        return !string.IsNullOrEmpty(requestId);
    }
}