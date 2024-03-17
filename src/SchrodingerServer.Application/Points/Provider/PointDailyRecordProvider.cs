using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using SchrodingerServer.Users.Index;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.Points.Provider;

public interface IPointDailyRecordProvider
{
    Task<List<PointDailyRecordIndex>> GetPointDailyRecordsAsync(string chainId, string bizDate,
        int skipCount);
}

public class PointDailyRecordProvider : IPointDailyRecordProvider, ISingletonDependency
{
    private readonly INESTRepository<PointDailyRecordIndex, string> _pointDailyRecordIndexRepository;

    public PointDailyRecordProvider(INESTRepository<PointDailyRecordIndex, string> pointDailyRecordIndexRepository)
    {
        _pointDailyRecordIndexRepository = pointDailyRecordIndexRepository;
    }

    public async Task<List<PointDailyRecordIndex>> GetPointDailyRecordsAsync(string chainId, string bizDate,
        int skipCount)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<PointDailyRecordIndex>, QueryContainer>>();
        
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.ChainId).Value(chainId)));
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.BizDate).Value(bizDate)));

        QueryContainer Filter(QueryContainerDescriptor<PointDailyRecordIndex> f) =>
            f.Bool(b => b.Must(mustQuery));
        
        var sorting = new Func<SortDescriptor<PointDailyRecordIndex>, IPromise<IList<ISort>>>(s =>
            s.Descending(t => t.BizDate));
        
        var tuple = await _pointDailyRecordIndexRepository.GetSortListAsync(Filter, skip: skipCount, sortFunc: sorting);
        return tuple.Item2;
    }
}