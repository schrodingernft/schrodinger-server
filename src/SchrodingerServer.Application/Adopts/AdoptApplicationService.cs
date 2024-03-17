using System;
using System.Collections.Generic;
using System.IO;
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
using SchrodingerServer.AwsS3;
using SchrodingerServer.CoinGeckoApi;
using SchrodingerServer.Common;
using SchrodingerServer.Dtos.Adopts;
using SchrodingerServer.Dtos.TraitsDto;
using SchrodingerServer.Ipfs;
using SchrodingerServer.Options;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;
using Volo.Abp.Users;
using Attribute = SchrodingerServer.Dtos.Adopts.Attribute;
using SchrodingerServer.Users;
using SchrodingerServer.Users.Dto;
using AdoptInfo = SchrodingerServer.Adopts.provider.AdoptInfo;
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
    private readonly IIpfsAppService _ipfsAppService;
    private readonly AwsS3Client _awsS3Client;
    

    public AdoptApplicationService(ILogger<AdoptApplicationService> logger, IOptionsMonitor<TraitsOptions> traitsOption,
        IAdoptImageService adoptImageService, IOptionsMonitor<AdoptImageOptions> adoptImageOptions,
        IOptionsMonitor<ChainOptions> chainOptions, IAdoptGraphQLProvider adoptGraphQlProvider, 
        IOptionsMonitor<CmsConfigOptions> cmsConfigOptions, IUserActionProvider userActionProvider, 
        ISecretProvider secretProvider, IIpfsAppService ipfsAppService, AwsS3Client awsS3Client)
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
        _ipfsAppService = ipfsAppService;
        _awsS3Client = awsS3Client;
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

    public async Task<bool> IsOverLoadedAsync()
    {
        try
        {
            using var httpClient = new HttpClient();
            var isOverLoaded = _traitsOptions.CurrentValue.IsOverLoadedUrl;
            var response = await httpClient.GetAsync(_traitsOptions.CurrentValue.IsOverLoadedUrl);
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("IsOverLoadedAsync get result Success");
                var resp = JsonConvert.DeserializeObject<IsOverLoadedResponse>(responseString);
                return resp.isOverLoaded;
            }
            else
            {
                _logger.LogError("IsOverLoadedAsync get result Success fail, {resp}", response.ToString());
            }
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "IsOverLoadedAsync get result Success fail error, {err}", e.ToString());
            return true;
        }
    }
    

    public async Task<GetWaterMarkImageInfoOutput> GetWaterMarkImageInfoAsync(GetWaterMarkImageInfoInput input)
    {
        _logger.Info("GetWaterMarkImageInfoAsync, {req}", JsonConvert.SerializeObject(input));
        var images = await _adoptImageService.GetImagesAsync(input.AdoptId);
        
        if (images.IsNullOrEmpty() || !images.Contains(input.Image))
        {
            _logger.Info("Invalid adopt image, images:{}", JsonConvert.SerializeObject(images));
            throw new UserFriendlyException("Invalid adopt image");
        }

        var hasWaterMark = await _adoptImageService.HasWatermark(input.AdoptId);
        if (hasWaterMark)
        {
            var info = await _adoptImageService.GetWatermarkImageInfoAsync(input.AdoptId);
            _logger.Info("GetWatermarkImageInfo from grain, info: {info}", JsonConvert.SerializeObject(info));

            if (info == null || info.ImageUri == "" || info.ResizedImage == "")
            {
                _logger.Info("Invalid watermark info, uri:{}, resizeImage", info.ImageUri, info.ResizedImage);
                throw new UserFriendlyException("Invalid watermark info");
            }
            
            var signature = GenerateSignatureWithSecretService(input.AdoptId, info.ImageUri, info.ResizedImage);
        
            var response = new GetWaterMarkImageInfoOutput
            {
                Image = info.ResizedImage,
                Signature = signature,
                ImageUri = info.ImageUri
            };
            
            _logger.LogInformation("GetWatermarkImageResp {resp} ",  JsonConvert.SerializeObject(response));
            return response;
        }
        
        var adoptInfo = await QueryAdoptInfoAsync(input.AdoptId);
        _logger.Info("QueryAdoptInfoAsync, {adoptInfo}", JsonConvert.SerializeObject(adoptInfo));
        if (adoptInfo == null)
        {
            throw new UserFriendlyException("query adopt info failed adoptId = " + input.AdoptId);
        }
        
        var waterMarkInfo = await GetWatermarkImageAsync(new WatermarkInput()
        {
            sourceImage = input.Image,
            watermark = new WaterMark
            {
                text = adoptInfo.Symbol
            }
        });
        _logger.LogInformation("GetWatermarkImageAsync : {info} ",  JsonConvert.SerializeObject(waterMarkInfo));

        if (waterMarkInfo == null || waterMarkInfo.processedImage == "" || waterMarkInfo.resized == "")
        {
            throw new UserFriendlyException("waterMarkImage empty");
        }

        var stringArray = waterMarkInfo.processedImage.Split(",");
        if (stringArray.Length < 2)
        {
            _logger.LogInformation("invalid waterMarkInfo");
            throw new UserFriendlyException("invalid waterMarkInfo");
        }
        
        var base64String = stringArray[1].Trim();
        string waterImageHash = await _ipfsAppService.UploadFile( base64String, input.AdoptId);
        var uri = "ipfs://" + waterImageHash;
        
        // uploadToS3
        var s3Url = await uploadToS3Async(base64String, waterImageHash);
        _logger.LogInformation("upload to s3, url:{url}", s3Url);
        
        await _adoptImageService.SetWatermarkImageInfoAsync(input.AdoptId, uri, waterMarkInfo.resized, input.Image);

        var signatureWithSecretService = GenerateSignatureWithSecretService(input.AdoptId, uri, waterMarkInfo.resized);
        
        var resp = new GetWaterMarkImageInfoOutput
        {
            Image = waterMarkInfo.resized,
            Signature = signatureWithSecretService,
            ImageUri = uri
        };
        _logger.LogInformation("GetWatermarkImageResp {resp} ",  JsonConvert.SerializeObject(resp));
        
        return resp;
    }

    private async Task<string> uploadToS3Async(string base64String, string fileName)
    {
        try
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            var stream = new MemoryStream(imageBytes);
            return await _awsS3Client.UpLoadFileForNFTAsync(stream, fileName);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "upload to s3 error, {err}", e.ToString());
            return string.Empty;
        }
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
    
    private string GenerateSignatureWithSecretService(string adoptId, string uri, string image)
    {
        var data = new ConfirmInput {
            AdoptId = Hash.LoadFromHex(adoptId),
            Image = image,
            ImageUri = uri 
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
    
    private async Task<WatermarkResponse> GetWatermarkImageAsync(WatermarkInput input)
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
               
                return resp;
            }
            else
            {
                _logger.LogError("Get Watermark Image Success fail, {resp}", response.ToString());
            }
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Get Watermark Image Success fail error, {err}", e.ToString());
            return null;
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