using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using SchrodingerServer.Users.Index;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.Background.Providers;

public interface IZealyProvider
{
    Task<List<ZealyUserIndex>> GetUsersAsync(int skipCount, int maxResultCount);
    Task<List<ZealyUserXpIndex>> GetUserXpsAsync(int skipCount, int maxResultCount);
}

public class ZealyProvider : IZealyProvider, ISingletonDependency
{
    private readonly ILogger<ZealyProvider> _logger;
    private readonly INESTRepository<ZealyUserIndex, string> _zealyUserRepository;
    private readonly INESTRepository<ZealyUserXpIndex, string> _zealyUserXpRepository;

    public ZealyProvider(INESTRepository<ZealyUserIndex, string> zealyUserRepository, ILogger<ZealyProvider> logger,
        INESTRepository<ZealyUserXpIndex, string> zealyUserXpRepository)
    {
        _zealyUserRepository = zealyUserRepository;
        _logger = logger;
        _zealyUserXpRepository = zealyUserXpRepository;
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
}