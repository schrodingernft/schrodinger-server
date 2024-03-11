using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AElf;
using AElf.Cryptography;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Schrodinger;
using SchrodingerServer.Adopts.provider;
using SchrodingerServer.Dtos.Adopts;
using SchrodingerServer.Dtos.TraitsDto;
using SchrodingerServer.Options;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;

namespace SchrodingerServer.Adopts;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class AdoptApplicationService : ApplicationService, IAdoptApplicationService
{
    private readonly ILogger<AdoptApplicationService> _logger;
    private readonly IOptionsMonitor<TraitsOptions> _traitsOptions;
    private readonly IAdoptImageService _adoptImageService;
    private readonly AdoptImageOptions _adoptImageOptions;
    private readonly ChainOptions _chainOptions;
    private readonly IAdoptGraphQLProvider _adoptGraphQlProvider;

    public AdoptApplicationService(ILogger<AdoptApplicationService> logger, IOptionsMonitor<TraitsOptions> traitsOption,
        IAdoptImageService adoptImageService, IOptionsMonitor<AdoptImageOptions> adoptImageOptions,
        IOptionsMonitor<ChainOptions> chainOptions, IAdoptGraphQLProvider adoptGraphQlProvider)
    {
        _logger = logger;
        _traitsOptions = traitsOption;
        _adoptImageService = adoptImageService;
        _adoptGraphQlProvider = adoptGraphQlProvider;
        _chainOptions = chainOptions.CurrentValue;
        _adoptImageOptions = adoptImageOptions.CurrentValue;
    }


    public async Task<GetAdoptImageInfoOutput> GetAdoptImageInfoAsync(string adoptId)
    {
        var output = new GetAdoptImageInfoOutput();
        
        // query traits from indexer
        var adoptInfo = await QueryAdoptInfoAsync(adoptId);
        
        // query from grain if adopt id and request id not exist generate image and save  adopt id and request id to grain if exist query result from ai interface
        //TODO need to use adoptId and Address insteadof adoptId
        var imageGenerationId = await _adoptImageService.GetImageGenerationIdAsync(adoptId);
        
        output.AdoptImageInfo = new AdoptImageInfo
        {
            Attributes = adoptInfo.Attributes,
            Generation = adoptInfo.Generation,
        };

        if (imageGenerationId == null)
        {
            await _adoptImageService.SetImageGenerationIdAsync(adoptId, Guid.NewGuid().ToString());
            return output;
        }

        output.AdoptImageInfo.Images = await GetImagesAsync(adoptId, adoptInfo.ImageCount);
        // if (!requestId.IsNullOrEmpty())
        // {
        //    var aiQueryResponse = await QueryImageInfoByAiAsync(requestId);
        //    var salt = _traitsOptions.CurrentValue.Salt;
        //    res.images = aiQueryResponse;
        // }
        // else
        // {
        //     // generate image by ai
        //     requestId = await GenerateImageByAiAsync(imageInfo, adoptId);
        //     // save to grain
        //     await _traitsService.SetImageGenerationIdAsync(adoptId, requestId);
        // }

        return output;
    }

    public async Task<GetWaterMarkImageInfoOutput> GetWaterMarkImageInfoAsync(GetWaterMarkImageInfoInput input)
    {
        var images = await _adoptImageService.GetImagesAsync(input.AdoptId);
        // if (images.IsNullOrEmpty() || !images.Contains(input.Image))
        // {
        //     throw new UserFriendlyException("Invalid adopt image");
        // }

        //TODO Need to save used image for checking next request.
        var index = _adoptImageOptions.Images.IndexOf(input.Image);
        var waterMarkImage = _adoptImageOptions.WaterMarkImages[index];
        return new GetWaterMarkImageInfoOutput
        {
            Image = waterMarkImage,
            Signature = GenerateSignature(ByteArrayHelper.HexStringToByteArray(_chainOptions.PrivateKey),input.AdoptId, waterMarkImage)
        };
    }
    
    private string GenerateSignature(byte[] privateKey, string adoptId, string image)
    {
        var data = new ConfirmInput {
            AdoptId = Hash.LoadFromHex(adoptId),
            Image = image
        };
        var dataHash = HashHelper.ComputeFrom(data);
        var signature = CryptoHelper.SignWithPrivateKey(privateKey, dataHash.ToByteArray());
        return signature.ToHex();
    }

    private async Task<List<string>> GetImagesAsync(string adoptId, int count)
    {
        var images = await _adoptImageService.GetImagesAsync(adoptId);
        if (images != null)
        {
            return images;
        } 
        
        images = new List<string>();
        var index = RandomHelper.GetRandom(_adoptImageOptions.Images.Count);
        for (int i = 0; i < count; i++)
        {
            images.Add(_adoptImageOptions.Images[index % _adoptImageOptions.Images.Count]);
            index++;
        }
        await _adoptImageService.SetImagesAsync(adoptId, images);
        return images;
    }

    private async Task<AdoptInfo> QueryAdoptInfoAsync(string adoptId)
    {
        return await _adoptGraphQlProvider.QueryAdoptInfoAsync(adoptId);
    }

    private async Task<string> GenerateImageByAiAsync(GenerateImage imageInfo, string adoptId)
    {
        try
        {
            // todo remove
            // imageInfo = new GenerateImage
            // {
            //     seed = "",
            //     newTraits = new List<Trait>
            //     {
            //         new Trait
            //         {
            //             name = "mouth",
            //             value = "bewitching"
            //         }
            //     },
            //     baseImage = new BaseImage
            //     {
            //         traits = new List<Trait>
            //         {
            //             new Trait
            //             {
            //                 name = "hat",
            //                 value = "alpine hat"
            //             },
            //             new Trait
            //             {
            //                 name = "eye",
            //                 value = "is wearing 3d glasses"
            //             }
            //         }
            //     }
            // };

            using var httpClient = new HttpClient();
            var jsonString = ConvertObjectToJsonString(imageInfo);
            var requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
            httpClient.DefaultRequestHeaders.Add("accept", "*/*");

            var response = await httpClient.PostAsync(_traitsOptions.CurrentValue.ImageGenerateUrl, requestContent);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("TraitsActionProvider GenerateImageByAiAsyncCheck generate success adopt id:" + adoptId);
                // save adopt id and request id to grain
                return responseString;
            }
            else
            {
                _logger.LogError("TraitsActionProvider GenerateImageByAiAsyncCheck generate error");
            }
            return "";
        }
        catch (Exception e)
        {
            _logger.LogError(e, "TraitsActionProvider GenerateImageByAiAsyncCheck generate exception");
            return "";
        }
    }
    
    private async Task<AiQueryResponse> QueryImageInfoByAiAsync(string requestId)
    {
        // requestId = "363408ba-4f7f-4a9b-8503-77df25b60203"; // todo remove
        var queryImage = new QueryImage
        {
            requestId = requestId 
        };
        var jsonString = ConvertObjectToJsonString(queryImage);
        using var httpClient = new HttpClient();
        var requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
        httpClient.DefaultRequestHeaders.Add("accept", "*/*");
        var response = await httpClient.PostAsync(_traitsOptions.CurrentValue.ImageQueryUrl, requestContent);
        if (response.IsSuccessStatusCode)
        {
            string responseContent = await response.Content.ReadAsStringAsync();
            AiQueryResponse aiQueryResponse = JsonConvert.DeserializeObject<AiQueryResponse>(responseContent);
            _logger.LogInformation("TraitsActionProvider QueryImageInfoByAiAsync query success");
            // todo image 字段加密
            // GenerateContractSignature 下面接口
            return aiQueryResponse;
        }
        else
        {
            _logger.LogError("TraitsActionProvider QueryImageInfoByAiAsync query not success");
            return new AiQueryResponse{};
        }
    }
    
    private string ConvertObjectToJsonString<T>(T paramObj)
    {
        var paramMap = paramObj.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(paramObj, null));
        return JsonConvert.SerializeObject(paramMap);
    }

    // private string GenerateContractSignature(string image)
    // {
    //     var data = new ImageOperation{
    //         salt = _traitsOptions.CurrentValue.Salt,
    //         image = image
    //     };
    //     var dataHash = HashHelper.ComputeFrom(data);
    //     var signature = CryptoHelper.SignWithPrivatebKey(Encoding.UTF8.GetBytes(_traitsOptions.CurrentValue.Salt), dataHash.ToByteArray());
    //     return signature.ToHex();
    //
    // }
}