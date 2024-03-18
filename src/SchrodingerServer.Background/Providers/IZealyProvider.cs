using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Nest;
using SchrodingerServer.Common;
using SchrodingerServer.Zealy;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.Background.Providers;

public interface IZealyProvider
{
    Task<List<ZealyUserIndex>> GetUsersAsync(int skipCount, int maxResultCount);
    Task<ZealyUserIndex> GetUserByIdAsync(string userId);
    Task<List<ZealyUserXpIndex>> GetUserXpsAsync(int skipCount, int maxResultCount);
    Task<ZealyUserXpIndex> GetUserXpByIdAsync(string id);
    Task<List<ZealyXpScoreIndex>> GetXpScoresAsync(int skipCount, int maxResultCount);

    Task<List<ZealyUserXpRecordCleanUpIndex>> GetPendingUserXpsAsync(int skipCount, int maxResultCount);

    Task UserXpAddOrUpdateAsync(ZealyUserXpIndex zealyUserXp);
    Task XpRecordAddOrUpdateAsync(ZealyUserXpRecordCleanUpIndex record);
}

public class ZealyProvider : IZealyProvider, ISingletonDependency
{
    private readonly ILogger<ZealyProvider> _logger;
    private readonly INESTRepository<ZealyUserIndex, string> _zealyUserRepository;
    private readonly INESTRepository<ZealyUserXpIndex, string> _zealyUserXpRepository;
    private readonly INESTRepository<ZealyXpScoreIndex, string> _zealyXpScoreRepository;
    private readonly INESTRepository<ZealyUserXpRecordCleanUpIndex, string> _zealyXpRecordRepository;

    public ZealyProvider(INESTRepository<ZealyUserIndex, string> zealyUserRepository, ILogger<ZealyProvider> logger,
        INESTRepository<ZealyUserXpIndex, string> zealyUserXpRepository,
        INESTRepository<ZealyXpScoreIndex, string> zealyXpScoreRepository,
        INESTRepository<ZealyUserXpRecordCleanUpIndex, string> zealyXpRecordRepository)
    {
        _zealyUserRepository = zealyUserRepository;
        _logger = logger;
        _zealyUserXpRepository = zealyUserXpRepository;
        _zealyXpScoreRepository = zealyXpScoreRepository;
        _zealyXpRecordRepository = zealyXpRecordRepository;
    }

    public async Task<List<ZealyUserIndex>> GetUsersAsync(int skipCount, int maxResultCount)
    {
        var (totalCount, data) =
            await _zealyUserRepository.GetListAsync(skip: skipCount, limit: maxResultCount);

        return data;
    }

    public async Task<ZealyUserIndex> GetUserByIdAsync(string userId)
    {
        if (userId.IsNullOrEmpty())
        {
            return null;
        }
        
        var mustQuery = new List<Func<QueryContainerDescriptor<ZealyUserIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.Id).Value(userId)));
        
        QueryContainer Filter(QueryContainerDescriptor<ZealyUserIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        return await _zealyUserRepository.GetAsync(Filter);
    }

    public async Task<List<ZealyUserXpIndex>> GetUserXpsAsync(int skipCount, int maxResultCount)
    {
        var (totalCount, data) =
            await _zealyUserXpRepository.GetListAsync(skip: skipCount, limit: maxResultCount);

        return data;
    }

    public async Task<ZealyUserXpIndex> GetUserXpByIdAsync(string id)
    {
        if (id.IsNullOrEmpty())
        {
            return null;
        }
        
        var mustQuery = new List<Func<QueryContainerDescriptor<ZealyUserXpIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.Id).Value(id)));
        
        QueryContainer Filter(QueryContainerDescriptor<ZealyUserXpIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        return await _zealyUserXpRepository.GetAsync(Filter);
    }

    public async Task UserXpAddOrUpdateAsync(ZealyUserXpIndex zealyUserXp)
    {
        await _zealyUserXpRepository.AddOrUpdateAsync(zealyUserXp);
    }
    
    public async Task XpRecordAddOrUpdateAsync(ZealyUserXpRecordCleanUpIndex record)
    {
        await _zealyXpRecordRepository.AddOrUpdateAsync(record);
    }

    public async Task<List<ZealyXpScoreIndex>> GetXpScoresAsync(int skipCount, int maxResultCount)
    {
        var (totalCount, data) =
            await _zealyXpScoreRepository.GetListAsync(skip: skipCount, limit: maxResultCount);

        return data;
    }

    public async Task<List<ZealyUserXpRecordCleanUpIndex>> GetPendingUserXpsAsync(int skipCount, int maxResultCount)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ZealyUserXpRecordCleanUpIndex>, QueryContainer>>();

        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.Status).Value(ContractInvokeStatus.Pending.ToString())));

        QueryContainer Filter(QueryContainerDescriptor<ZealyUserXpRecordCleanUpIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var (totalCount, data) = await _zealyXpRecordRepository.GetListAsync(Filter);

        return data;
    }
}