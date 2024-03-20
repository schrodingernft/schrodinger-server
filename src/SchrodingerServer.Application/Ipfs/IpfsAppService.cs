using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SchrodingerServer.Common.Options;
using SchrodingerServer.Dtos.Ipfs;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.Ipfs;


public class IpfsAppService : ISingletonDependency, IIpfsAppService
{
    private readonly ILogger<IpfsAppService> _logger;
    private readonly IOptionsMonitor<IpfsOptions> _options;
    
    public IpfsAppService(ILogger<IpfsAppService> logger, 
        IOptionsMonitor<IpfsOptions> ipfsOption)
    {
        _logger = logger;
        _options = ipfsOption;
    }

    public async Task<string> Upload(string content, string name)
    {
        try
        {
            var url = _options.CurrentValue.Url;
            var request = new HttpRequestMessage(HttpMethod.Post, url);
        
            request.Headers.Add("Authorization", _options.CurrentValue.Token);
            request.Content = JsonContent.Create(new Dictionary<string, object>());

            var ipfsBody = new IpfsBody
            {
                pinataContent = new PinataContent
                {
                    data = content,
                },
                pinataOptions = new PinataOptions
                {
                    cidVersion = 1,
                },
                pinataMetadata = new PinataMetadata
                {
                    name = name 
                }
            };
            var jsonString = JsonConvert.SerializeObject(ipfsBody);
        
            request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            var client = new HttpClient();
        
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("UploadIpfs Success, name: {name} resp: {resp}", name, response.ToString());
                var responseString = await response.Content.ReadAsStringAsync();
                var resp = JsonConvert.DeserializeObject<IpfsResponse>(responseString);
                return resp.IpfsHash;
            }
    
            _logger.LogError("UploadIpfs Success fail, name: {name}, resp: {resp}", name, response.ToString());
            return "";
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UploadIpfs Success error, name: {name}, err: {err}", name, e.ToString());
            return "";
        }
    }
    
    public async Task<string> UploadFile(string base64String, string name)
    {
        try
        {
            var content = new MultipartFormDataContent();
            byte[] imageBytes = Convert.FromBase64String(base64String);
            var fileContent = new ByteArrayContent(imageBytes);
            
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
            content.Add(fileContent, "file", name);
            
            var url = _options.CurrentValue.PinFileUrl;
            var request = new HttpRequestMessage(HttpMethod.Post, url);
        
            request.Headers.Add("Authorization", _options.CurrentValue.Token);
            request.Content = content;
            
            var client = new HttpClient();
        
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("UploadIpfs Success, name: {name} resp: {resp}", name, response.ToString());
                var responseString = await response.Content.ReadAsStringAsync();
                var resp = JsonConvert.DeserializeObject<IpfsResponse>(responseString);
                return resp.IpfsHash;
            }
    
            _logger.LogError("UploadIpfs Success fail, name: {name}, resp: {resp}", name, response.ToString());
            return "";
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UploadIpfs Success error, name: {name}, err: {err}", name, e.ToString());
            return "";
        }
    }
}