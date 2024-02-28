using System.Net.Http;
using System.Threading.Tasks;
using AElf;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SchrodingerServer.Common;
using SchrodingerServer.Common.Dtos;
using SchrodingerServer.Common.HttpClient;
using SchrodingerServer.Options;
using SchrodingerServer.PointServer.Dto;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.PointServer;

public interface IPointServerProvider
{
    Task InvitationRelationshipsAsync(InvitationRequest request);
    
    Task<bool> CheckDomainAsync(string domain);
    
}

public class PointServerProvider : IPointServerProvider, ISingletonDependency
{
    public static class Api
    {
        public static ApiInfo Invitation = new(HttpMethod.Post, "/api/app/dapps/invitation/relationships");
    }

    private readonly IOptionsMonitor<PointServiceOptions> _pointServiceOptions;
    private readonly IHttpProvider _httpProvider;

    private static readonly JsonSerializerSettings JsonSerializerSettings = JsonSettingsBuilder.New()
        .IgnoreNullValue()
        .WithCamelCasePropertyNamesResolver()
        .WithAElfTypesConverters()
        .Build();


    public PointServerProvider(IHttpProvider httpProvider, IOptionsMonitor<PointServiceOptions> pointServiceOptions)
    {
        _httpProvider = httpProvider;
        _pointServiceOptions = pointServiceOptions;
    }


    public async Task InvitationRelationshipsAsync(InvitationRequest request)
    {
        request.DappName = _pointServiceOptions.CurrentValue.DappName;
        request.Signature = GetSign(request);
        var resp = await _httpProvider.InvokeAsync<CommonResponseDto<bool>>(
            _pointServiceOptions.CurrentValue.BaseUrl, Api.Invitation,
            body: JsonConvert.SerializeObject(request, JsonSerializerSettings));
        AssertHelper.NotNull(resp, "Response empty");
        AssertHelper.NotNull(resp.Data, "Response failed, {}", resp.Message);
    }

    public async Task<bool> CheckDomainAsync(string domain)
    {
        //TODO
        return true;
    }


    public string GetSign(object obj)
    {
        var source = ObjectHelper.ConvertObjectToSortedString(obj, "Signature");
        source += _pointServiceOptions.CurrentValue.DappSecret;
        return HashHelper.ComputeFrom(source).ToHex();
    }
}