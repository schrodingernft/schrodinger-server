using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchrodingerServer.Dtos.TraitsDto;
using SchrodingerServer.Options;
using Volo.Abp.Application.Services;
using Newtonsoft.Json;

namespace SchrodingerServer.Traits;

public class TraitsActionProvider : ApplicationService, ITraitsActionProvider
{
    private readonly ILogger<TraitsActionProvider> _logger;
    private readonly IOptionsMonitor<TraitsOptions> _traitsOptions;
    private readonly ITraitsService _traitsService;

    public TraitsActionProvider(ILogger<TraitsActionProvider> logger,IOptionsMonitor<TraitsOptions> traitsOption, ITraitsService traitsService)
    {
        _logger = logger;
        _traitsOptions = traitsOption;
        _traitsService = traitsService;
    }


    public async Task<bool> ImageGenerateAsync(string adoptId)
    {
        // query from grain if adopt id and request id not exist generate image and save  adopt id and request id to grain if exist query result from ai interface
        var requestId = await _traitsService.GetRequestAsync(adoptId);
        if (!requestId.IsNullOrEmpty())
        {
            await QueryImageInfoByAiAsync(requestId);
        }
        else
        {
            // query traits from indexer
            var imageInfo = await QueryTraitsAsync(adoptId);
            // generate image by ai 
            requestId = await GenerateImageByAiAsync(imageInfo, adoptId);
            // save to grain
            await _traitsService.SetRequestAsync(adoptId, requestId);
        }

        return true;
    }

    private async Task<GenerateImage> QueryTraitsAsync(string adoptId)
    {
        return new GenerateImage{}; // todo
    }
    
    

    private async Task<string> GenerateImageByAiAsync(GenerateImage imageInfo, string adoptId)
    {
        try
        {
            imageInfo = new GenerateImage
            {
                seed = "",
                newTraits = new List<Trait>
                {
                    new Trait
                    {
                        name = "mouth",
                        value = "bewitching"
                    }
                },
                baseImage = new BaseImage
                {
                    traits = new List<Trait>
                    {
                        new Trait
                        {
                            name = "hat",
                            value = "alpine hat"
                        },
                        new Trait
                        {
                            name = "eye",
                            value = "is wearing 3d glasses"
                        }
                    }
                }
            };

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
}