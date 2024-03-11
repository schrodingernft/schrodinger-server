using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.Extensions.Logging;
using SchrodingerServer.Common.GraphQL;
using SchrodingerServer.Dtos.Adopts;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.Adopts.provider;

public interface IAdoptGraphQLProvider
{
    Task<AdoptInfo> QueryAdoptInfoAsync(string adoptId);
}

public class AdoptGraphQLProvider : IAdoptGraphQLProvider, ISingletonDependency
{
    private readonly IGraphQlHelper _graphQlHelper;
    private readonly ILogger<AdoptGraphQLProvider> _logger;

    public AdoptGraphQLProvider(IGraphQlHelper graphQlHelper, ILogger<AdoptGraphQLProvider> logger)
    {
        _graphQlHelper = graphQlHelper;
        _logger = logger;
    }

    public async Task<AdoptInfo> QueryAdoptInfoAsync(string adoptId)
    {
        var adpotInfoDto = await _graphQlHelper.QueryAsync<AdpotInfoDto>(new GraphQLRequest
        {
            Query =
                @"query($adpotId:String){
                    getAdpotInfo(input: {adpotId:$adpotId}){
                          symbol
                          tokenName
                          attributes{
                            traitType
                            value
                            percent
                          }
                          adoptor
                          imageCount
                          gen
                }
            }",
            Variables = new
            {
                adoptId = adoptId
            }
        });
        if (adpotInfoDto == null)
        {
            _logger.LogError("query adopt info failed, adoptId = {AdoptId}", adoptId);
            return null;
        }

        return new AdoptInfo()
        {
            Symbol = adpotInfoDto.Symbol,
            TokenName = adpotInfoDto.TokenName,
            Attributes = adpotInfoDto.Attributes.Select(a => new Attribute()
            {
                TraitType = a.TraitType,
                value = a.Value
            }).ToList(),
            Adoptor = adpotInfoDto.Adoptor,
            ImageCount = adpotInfoDto.ImageCount,
            Generation = adpotInfoDto.Gen
        };
    }
}

public class AdpotInfoDto
{
    public string AdoptId { get; set; }
    public string Parent { get; set; }
    public string Ancestor { get; set; }
    public string Symbol { get; set; }
    public string Issuer { get; set; }
    public string Owner { get; set; }
    public string Deployer { get; set; }
    public string Adoptor { get; set; }
    public string TokenName { get; set; }

    public List<Trait> Attributes { get; set; }

    public Dictionary<string, string> AdoptExternalInfo { get; set; } = new();
    public long InputAmount { get; set; }
    public long LossAmount { get; set; }
    public long CommissionAmount { get; set; }
    public long OutputAmount { get; set; }
    public int ImageCount { get; set; }
    public long TotalSupply { get; set; }
    public int IssueChainId { get; set; }
    public int Gen { get; set; }
    public int ParentGen { get; set; }
    public int Decimals { get; set; }
}

public class Trait
{
    public string TraitType { get; set; }
    public string Value { get; set; }
    public string Percent { get; set; }
}