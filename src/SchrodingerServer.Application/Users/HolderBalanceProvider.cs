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

    Task<HolderBalanceIndex> GetPreHolderBalanceAsync(string chainId, string bizDate, string address);
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


    public async Task<List<HolderDailyChangeDto>> GetHolderDailyChangeListAsync(string chainId, string date,
        int skipCount, int maxResultCount)
    {
        var graphQlResponse = await _graphQlHelper.QueryAsync<IndexerHolderDailyChangeDto>(new GraphQLRequest
        {
            Query = @"query($chainId:String!,$date:String!,$skipCount:Int!,$maxResultCount:Int!){
            getSchrodingerHolderDailyChangeList(input: {chainId:$chainId,date:$date,skipCount:$skipCount,maxResultCount:$maxResultCount})
            {
               data {
                address,
                symbol,
                date,
                changeAmount,
                balance
                },
                totalCount
            }}",
            Variables = new
            {
                chainId,
                date,
                skipCount,
                maxResultCount
            }
        });
        return graphQlResponse?.GetSchrodingerHolderDailyChangeList.Data;
    }

    public async Task<HolderBalanceIndex> GetPreHolderBalanceAsync(string chainId, string bizDate, string address)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<HolderBalanceIndex>, QueryContainer>>();

        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.ChainId).Value(chainId)));

        mustQuery.Add(q => q.TermRange(i
            => i.Field(index => index.BizDate).LessThan(bizDate)));

        mustQuery.Add(q => q.Terms(i =>
            i.Field(f => f.Address).Terms(address)));

        QueryContainer Filter(QueryContainerDescriptor<HolderBalanceIndex> f) =>
            f.Bool(b => b.Must(mustQuery));
        
        var sorting = new Func<SortDescriptor<HolderBalanceIndex>, IPromise<IList<ISort>>>(s =>
            s.Descending(t => t.BizDate));
        
        var tuple = await _holderBalanceIndexRepository.GetSortListAsync(Filter, sortFunc: sorting);
        
        return !tuple.Item2.IsNullOrEmpty() ? tuple.Item2.FirstOrDefault() : new HolderBalanceIndex();
    }
}