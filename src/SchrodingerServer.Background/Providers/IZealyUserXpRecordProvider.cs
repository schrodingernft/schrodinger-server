using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using SchrodingerServer.Common;
using SchrodingerServer.Zealy;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.Background.Providers;

public interface IZealyUserXpRecordProvider
{
    Task<List<ZealyUserXpRecordCleanUpIndex>> GetToCreateRecordAsync(int skipCount, int maxResultCount);
}

public class ZealyUserXpRecordProvider : IZealyUserXpRecordProvider, ISingletonDependency
{
    private readonly INESTRepository<ZealyUserXpRecordCleanUpIndex, string> _zealyXpRecordRepository;

    public ZealyUserXpRecordProvider(INESTRepository<ZealyUserXpRecordCleanUpIndex, string> zealyXpRecordRepository)
    {
        _zealyXpRecordRepository = zealyXpRecordRepository;
    }

    public async Task<List<ZealyUserXpRecordCleanUpIndex>> GetToCreateRecordAsync(int skipCount, int maxResultCount)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ZealyUserXpRecordCleanUpIndex>, QueryContainer>>();

        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.Status).Value(ContractInvokeStatus.ToBeCreated.ToString())));

        QueryContainer Filter(QueryContainerDescriptor<ZealyUserXpRecordCleanUpIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var (totalCount, data) =
            await _zealyXpRecordRepository.GetListAsync(Filter, skip: skipCount, limit: maxResultCount);

        return data;
    }
}