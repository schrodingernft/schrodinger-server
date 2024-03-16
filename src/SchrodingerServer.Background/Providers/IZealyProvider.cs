using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using SchrodingerServer.Zealy;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.Background.Providers;

public interface IZealyProvider
{
    Task<List<ZealyUserIndex>> GetUsersAsync(int skipCount, int maxResultCount);
    Task<List<ZealyUserXpIndex>> GetUserXpsAsync(int skipCount, int maxResultCount);
    Task<List<ZealyXpScoreIndex>> GetXpScoresAsync(int skipCount, int maxResultCount);

    Task<List<ZealyUserXpRecordIndex>> GetPendingUserXpsAsync(int skipCount, int maxResultCount);
}

public class ZealyProvider : IZealyProvider, ISingletonDependency
{
    private readonly ILogger<ZealyProvider> _logger;
    private readonly INESTRepository<ZealyUserIndex, string> _zealyUserRepository;
    private readonly INESTRepository<ZealyUserXpIndex, string> _zealyUserXpRepository;
    private readonly INESTRepository<ZealyXpScoreIndex, string> _zealyXpScoreRepository;
    private readonly INESTRepository<ZealyUserXpRecordIndex, string> _zealyXpRecordRepository;

    public ZealyProvider(INESTRepository<ZealyUserIndex, string> zealyUserRepository, ILogger<ZealyProvider> logger,
        INESTRepository<ZealyUserXpIndex, string> zealyUserXpRepository,
        INESTRepository<ZealyXpScoreIndex, string> zealyXpScoreRepository,
        INESTRepository<ZealyUserXpRecordIndex, string> zealyXpRecordRepository)
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

    public async Task<List<ZealyUserXpIndex>> GetUserXpsAsync(int skipCount, int maxResultCount)
    {
        var (totalCount, data) =
            await _zealyUserXpRepository.GetListAsync(skip: skipCount, limit: maxResultCount);

        return data;
    }

    public async Task<List<ZealyXpScoreIndex>> GetXpScoresAsync(int skipCount, int maxResultCount)
    {
        var (totalCount, data) =
            await _zealyXpScoreRepository.GetListAsync(skip: skipCount, limit: maxResultCount);

        return data;
    }

    public async Task<List<ZealyUserXpRecordIndex>> GetPendingUserXpsAsync(int skipCount, int maxResultCount)
    {
        var (totalCount, data) =
            await _zealyXpRecordRepository.GetListAsync(skip: skipCount, limit: maxResultCount);

        return data;
    }
}