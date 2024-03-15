using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using GraphQL;
using Nest;
using SchrodingerServer.Common.GraphQL;
using SchrodingerServer.Users.Index;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.Users;

public interface IHolderBalanceProvider
{
    Task<List<HolderDailyChangeDto>> GetHolderDailyChangeListAsync(string chainId, string bizDate, int skipCount,
        int maxResultCount);

    Task<Dictionary<string, HolderBalanceIndex>> GetPreHolderBalanceAsync(string chainId, string bizDate,
        List<string> addressList);
}

public class HolderBalanceProvider : IHolderBalanceProvider, ISingletonDependency
{
    private readonly IGraphQlHelper _graphQlHelper;
    private readonly INESTRepository<HolderBalanceIndex, string> _holderBalanceIndexRepository;

    public HolderBalanceProvider(IGraphQlHelper graphQlHelper,
        INESTRepository<HolderBalanceIndex, string> holderBalanceIndexRepository)
    {
        _graphQlHelper = graphQlHelper;
        _holderBalanceIndexRepository = holderBalanceIndexRepository;
    }


    public async Task<List<HolderDailyChangeDto>> GetHolderDailyChangeListAsync(string chainId, string bizDate,
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

    public async Task<Dictionary<string, HolderBalanceIndex>> GetPreHolderBalanceAsync(string chainId, string bizDate,
        List<string> addressList)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<HolderBalanceIndex>, QueryContainer>>();

        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.ChainId).Value(chainId)));

        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.BizDate).Value(bizDate)));
        
        mustQuery.Add(q => q.TermRange(i
            => i.Field(index => index.BizDate).LessThan(bizDate)));

        mustQuery.Add(q => q.Terms(i =>
            i.Field(f => f.Address).Terms(addressList)));

        QueryContainer Filter(QueryContainerDescriptor<HolderBalanceIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var tuple = await _holderBalanceIndexRepository.GetSortListAsync(Filter);
        return !tuple.Item2.IsNullOrEmpty()
            ? tuple.Item2.ToDictionary(item => item.Address, item => item)
            : new Dictionary<string, HolderBalanceIndex>();
    }
}