using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using SchrodingerServer.Common.GraphQL;
using SchrodingerServer.Users.Index;
using Volo.Abp.DependencyInjection;
namespace SchrodingerServer.Users;


public interface IHolderBalanceProvider
{
    Task<List<HolderDailyChangeDto>> GetHolderDailyChangeList(string chainId, string bizDate, int skipCount, int maxResultCount);
}

public class HolderBalanceProvider : IHolderBalanceProvider, ISingletonDependency
{
    private readonly IGraphQlHelper _graphQlHelper;

    public HolderBalanceProvider(IGraphQlHelper graphQlHelper)
    {
        _graphQlHelper = graphQlHelper;
    }


    public async Task<List<HolderDailyChangeDto>> GetHolderDailyChangeList(string chainId, string bizDate,
        int skipCount, int maxResultCount)
    {
        var graphQlResponse = await _graphQlHelper.QueryAsync<IndexerHolderDailyChanges>(new GraphQLRequest
        {
            Query = @"query($chainId:String!,$bizDate:String!,$skipCount:Int!,$maxResultCount:Int!){
            dataList:getSchrodingerHolderDailyChangeList(input: {chainId:$chainId,date:$bizDate,skipCount:$skipCount,maxResultCount:$maxResultCount})
            {
                address,
                symbol,
                date,
                changeAmount,
                balance
            }}",
            Variables = new
            {
                chainId,
                bizDate,
                skipCount,
                maxResultCount
            }
        });
        return graphQlResponse?.DataList ?? new List<HolderDailyChangeDto>();
    }
}