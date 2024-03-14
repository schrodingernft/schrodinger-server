using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using AElf;
using AElf.Cryptography;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.Runtime;
using Schrodinger;
using SchrodingerServer.Adopts.provider;
using SchrodingerServer.CoinGeckoApi;
using SchrodingerServer.Common;
using SchrodingerServer.Dtos.Adopts;
using SchrodingerServer.Dtos.TraitsDto;
using SchrodingerServer.Options;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;
using Volo.Abp.Users;
using Attribute = SchrodingerServer.Dtos.Adopts.Attribute;
using SchrodingerServer.Users;
using SchrodingerServer.Users.Dto;
using Trait = SchrodingerServer.Dtos.TraitsDto.Trait;

namespace SchrodingerServer.Adopts;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class AdoptApplicationService : ApplicationService, IAdoptApplicationService
{
    private readonly ILogger<AdoptApplicationService> _logger;
    private readonly IOptionsMonitor<TraitsOptions> _traitsOptions;
    private readonly IOptionsMonitor<CmsConfigOptions> _cmsConfigOptions;
    private readonly IAdoptImageService _adoptImageService;
    private readonly AdoptImageOptions _adoptImageOptions;
    private readonly ChainOptions _chainOptions;
    private readonly IAdoptGraphQLProvider _adoptGraphQlProvider;
    private readonly IUserActionProvider _userActionProvider;
    private readonly ISecretProvider _secretProvider;

    public AdoptApplicationService(ILogger<AdoptApplicationService> logger, IOptionsMonitor<TraitsOptions> traitsOption,
        IAdoptImageService adoptImageService, IOptionsMonitor<AdoptImageOptions> adoptImageOptions,
        IOptionsMonitor<ChainOptions> chainOptions, IAdoptGraphQLProvider adoptGraphQlProvider, 
        IOptionsMonitor<CmsConfigOptions> cmsConfigOptions, IUserActionProvider userActionProvider, 
        ISecretProvider secretProvider)
    {
        _logger = logger;
        _traitsOptions = traitsOption;
        _adoptImageService = adoptImageService;
        _adoptGraphQlProvider = adoptGraphQlProvider;
        _chainOptions = chainOptions.CurrentValue;
        _adoptImageOptions = adoptImageOptions.CurrentValue;
        _cmsConfigOptions = cmsConfigOptions;
        _userActionProvider = userActionProvider;
        _secretProvider = secretProvider;
    }


    public async Task<GetAdoptImageInfoOutput> GetAdoptImageInfoAsync(string adoptId)
    {
        _logger.Info("GetAdoptImageInfoAsync, {req}", adoptId);
        var output = new GetAdoptImageInfoOutput();
        var adoptInfo = await QueryAdoptInfoAsync(adoptId);
        if (adoptInfo == null)
        {
            return output;
        }
        var chainId = CommonConstant.MainChainId;
        if (_cmsConfigOptions.CurrentValue.ConfigMap.TryGetValue("curChain", out var curChain))
        {
            chainId = curChain;
        }
        var aelfAddress = await _userActionProvider.GetCurrentUserAddressAsync(chainId);
        
        var imageGenerationId = await _adoptImageService.GetImageGenerationIdAsync(JoinAdoptIdAndAelfAddress(adoptId, aelfAddress));
        
        output.AdoptImageInfo = new AdoptImageInfo
        {
            Attributes = adoptInfo.Attributes,
            Generation = adoptInfo.Generation,
        };

        if (imageGenerationId == null)
        {
            var imageInfo = new GenerateImage
            {
                newAttributes = new List<Trait>{},
                baseImage = new BaseImage
                {
                    attributes = new List<Trait>{}
                },
                numImages = adoptInfo.ImageCount
            };
            foreach (Attribute attributeItem in adoptInfo.Attributes)
            {
                var item = new Trait
                {
                    traitType = attributeItem.TraitType,
                    value = attributeItem.Value
                };
                imageInfo.newAttributes.Add(item);
            }
            _logger.LogInformation("GenerateImageByAiAsync Begin. imageInfo: {info} adoptId: {adoptId} ", JsonConvert.SerializeObject(adoptInfo), adoptId);
            var requestId = await GenerateImageByAiAsync(imageInfo, adoptId);
            _logger.LogInformation("GenerateImageByAiAsync Finish. requestId: {requestId} ",  requestId);
            if ("" != requestId)
            {
                await _adoptImageService.SetImageGenerationIdAsync(JoinAdoptIdAndAelfAddress(adoptId, aelfAddress), requestId);
            }
            return output;
        }

        output.AdoptImageInfo.Images = await GetImagesAsync(adoptId, imageGenerationId);
        return output;
    }
    
    

    public async Task<GetWaterMarkImageInfoOutput> GetWaterMarkImageInfoAsync(GetWaterMarkImageInfoInput input)
    {
        _logger.Info("GetWaterMarkImageInfoAsync, {req}", JsonConvert.SerializeObject(input));
        if (_adoptImageService.HasWatermark(input.AdoptId).Result)
        {
            _logger.Info("has already been watermarked, {id}", input.AdoptId);
            throw new UserFriendlyException("has already been watermarked");
        }
        
        var images = await _adoptImageService.GetImagesAsync(input.AdoptId);
        _logger.Info("GetImagesAsync, {images}", JsonConvert.SerializeObject(images));
        
        if (images.IsNullOrEmpty() || !images.Contains(input.Image))
        {
            throw new UserFriendlyException("Invalid adopt image");
        }
        
        var adoptInfo = await QueryAdoptInfoAsync(input.AdoptId);
        _logger.Info("QueryAdoptInfoAsync, {adoptInfo}", JsonConvert.SerializeObject(adoptInfo));
        if (adoptInfo == null)
        {
            throw new UserFriendlyException("query adopt info failed adoptId = " + input.AdoptId);
        }
        
        var waterMarkImage = await GetWatermarkImageAsync(new WatermarkInput()
        {
            sourceImage = input.Image,
            watermark = new WaterMark
            {
                text = adoptInfo.Symbol
            }
        });

        await _adoptImageService.SetWatermarkAsync(input.AdoptId);
        
        var signatureWithSecretService = GenerateSignatureWithSecretService(input.AdoptId, waterMarkImage);
        _logger.Info("signature from security service  {signatureWithSecretService}", signatureWithSecretService);
        
        return new GetWaterMarkImageInfoOutput
        {
            Image = waterMarkImage,
            Signature = signatureWithSecretService
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
    
    private string GenerateSignatureWithSecretService(string adoptId, string image)
    {
        var data = new ConfirmInput {
            AdoptId = Hash.LoadFromHex(adoptId),
            Image = image
        };
        var dataHash = HashHelper.ComputeFrom(data);
        var signature =  _secretProvider.GetSignatureFromHashAsync(_chainOptions.PublicKey, dataHash);
        return signature.Result;
    }

    private async Task<List<string>> GetImagesAsync(string adoptId, string requestId)
    {
        var images = await _adoptImageService.GetImagesAsync(adoptId);
        if (images != null && images.Count !=0)
        {
            _logger.LogInformation("TraitsActionProvider GetImagesAsync images null {requestId} {adoptId}", requestId, adoptId);
            return images;
        } 
        _logger.LogInformation("QueryImageInfoByAiAsync Begin. requestId: {requestId} adoptId: {adoptId} ", requestId, adoptId);
        var aiQueryResponse = await QueryImageInfoByAiAsync(requestId);
        images = new List<string>();
        _logger.LogInformation("QueryImageInfoByAiAsync Finish. resp: {resp}",  JsonConvert.SerializeObject(aiQueryResponse));
        if (aiQueryResponse == null || aiQueryResponse.images == null || aiQueryResponse.images.Count == 0)
        {
            _logger.LogInformation("TraitsActionProvider GetImagesAsync aiQueryResponse.images null");
            return images;
        }
        foreach (Dtos.TraitsDto.Image imageItem in aiQueryResponse.images)
        {
            images.Add(imageItem.image);
        }
        await _adoptImageService.SetImagesAsync(adoptId, images);
        return images;
    }

    private async Task<AdoptInfo> QueryAdoptInfoAsync(string adoptId)
    {
        return await _adoptGraphQlProvider.QueryAdoptInfoAsync(adoptId);
    }
    
    private async Task<string> GetWatermarkImageAsync(WatermarkInput input)
    {
        try
        {
            using var httpClient = new HttpClient();
            var jsonString = ConvertObjectToJsonString(input);
            var requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
            httpClient.DefaultRequestHeaders.Add("accept", "*/*");

            var response = await httpClient.PostAsync(_traitsOptions.CurrentValue.ImageProcessUrl, requestContent);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Get Watermark Image Success");
                
                var resp = JsonConvert.DeserializeObject<WatermarkResponse>(responseString);
               
                return resp.processedImage;
            }
            else
            {
                _logger.LogError("Get Watermark Image Success fail, {resp}", response.ToString());
            }
            return "";
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Get Watermark Image Success fail error, {err}", e.ToString());
            return "";
        }
    }
    
    

    private async Task<string> GenerateImageByAiAsync(GenerateImage imageInfo, string adoptId)
    {
        try
        {
            using var httpClient = new HttpClient();
            var jsonString = ConvertObjectToJsonString(imageInfo);
            var requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
            httpClient.DefaultRequestHeaders.Add("accept", "*/*");

            var response = await httpClient.PostAsync(_traitsOptions.CurrentValue.ImageGenerateUrl, requestContent);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("TraitsActionProvider GenerateImageByAiAsync generate success adopt id:" + adoptId);
                // save adopt id and request id to grain
                GenerateImageFromAiRes aiQueryResponse = JsonConvert.DeserializeObject<GenerateImageFromAiRes>(responseString);
                return aiQueryResponse.requestId;
            }
            else
            {
                _logger.LogError("TraitsActionProvider GenerateImageByAiAsync generate error {adoptId}", adoptId);
            }
            return "";
        }
        catch (Exception e)
        {
            _logger.LogError(e, "TraitsActionProvider GenerateImageByAiAsync generate exception {adoptId}", adoptId);
            return "";
        }
    }
    
    private async Task<AiQueryResponse> QueryImageInfoByAiAsync(string requestId)
    {
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
            _logger.LogInformation("TraitsActionProvider QueryImageInfoByAiAsync query success {requestId}", requestId);
            return aiQueryResponse;
        }
        else
        {
            _logger.LogError("TraitsActionProvider QueryImageInfoByAiAsync query not success {requestId}", requestId);
            return new AiQueryResponse{};
        }
    }
    
    private string ConvertObjectToJsonString<T>(T paramObj)
    {
        var paramMap = paramObj.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(paramObj, null));
        return JsonConvert.SerializeObject(paramMap);
    }
    
    private string JoinAdoptIdAndAelfAddress(string adoptId, string aelfAddress)
    {
        return adoptId + "_" + aelfAddress;
    }
}