using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using SchrodingerServer.ScoreRepair.Dtos;
using SchrodingerServer.Zealy;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace SchrodingerServer.ScoreRepair;

public class XpScoreRepairAppService : IXpScoreRepairAppService, ISingletonDependency
{
    private readonly ILogger<XpScoreRepairAppService> _logger;
    private readonly INESTRepository<ZealyXpScoreIndex, string> _zealyXpScoreRepository;
    private readonly IObjectMapper _objectMapper;

    public XpScoreRepairAppService(ILogger<XpScoreRepairAppService> logger,
        INESTRepository<ZealyXpScoreIndex, string> zealyXpScoreRepository, IObjectMapper objectMapper)
    {
        _logger = logger;
        _zealyXpScoreRepository = zealyXpScoreRepository;
        _objectMapper = objectMapper;
    }

    public async Task UpdateScoreRepairDataAsync(List<UpdateXpScoreRepairDataDto> input)
    {
        _logger.LogInformation("begin to update score, data:{data}", JsonConvert.SerializeObject(input));

        var userIds = input.Select(t => t.UserId).ToList();
        var scores = await GetXpDataAsync(userIds);
        var scoreInfos = _objectMapper.Map<List<UpdateXpScoreRepairDataDto>, List<ZealyXpScoreIndex>>(input);

        var timeSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        foreach (var score in scores)
        {
            var newScore = scoreInfos.FirstOrDefault(t => t.Id == score.Id);
            if (newScore == null) continue;

            score.LastRawScore = score.RawScore;
            score.LastActualScore = score.ActualScore;
            score.RawScore = newScore.RawScore;
            score.ActualScore = newScore.ActualScore;

            score.UpdateTime = timeSeconds;
        }

        if (!scores.IsNullOrEmpty())
        {
            await _zealyXpScoreRepository.BulkAddOrUpdateAsync(scores);
        }

        var scoreIds = scores.Select(t => t.Id).ToList();
        scoreInfos.RemoveAll(t => scoreIds.Contains(t.Id));

        foreach (var scoreInfo in scoreInfos)
        {
            scoreInfo.CreateTime = timeSeconds;
            scoreInfo.UpdateTime = timeSeconds;
        }

        if (!scoreInfos.IsNullOrEmpty())
        {
            await _zealyXpScoreRepository.BulkAddOrUpdateAsync(scoreInfos);
        }
    }

    public async Task<XpScoreRepairDataPageDto> GetXpScoreRepairDataAsync(XpScoreRepairDataRequestDto input)
    {
        var scoreInfos = await GetXpDataAsync(input);

        return new XpScoreRepairDataPageDto
        {
            TotalCount = scoreInfos.totalCount,
            Data = _objectMapper.Map<List<ZealyXpScoreIndex>, List<XpScoreRepairDataDto>>(scoreInfos.data)
        };
    }

    private async Task<(List<ZealyXpScoreIndex> data, long totalCount)> GetXpDataAsync(
        XpScoreRepairDataRequestDto input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ZealyXpScoreIndex>, QueryContainer>>();

        if (!input.UserId.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.Id).Value(input.UserId)));
        }

        QueryContainer Filter(QueryContainerDescriptor<ZealyXpScoreIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var (totalCount, data) = await _zealyXpScoreRepository.GetListAsync(Filter, skip: input.SkipCount,
            limit: input.MaxResultCount);

        return (data, totalCount);
    }

    private async Task<List<ZealyXpScoreIndex>> GetXpDataAsync(List<string> userIds)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ZealyXpScoreIndex>, QueryContainer>>();
        if (userIds.IsNullOrEmpty())
        {
            return new List<ZealyXpScoreIndex>();
        }

        mustQuery.Add(q => q.Terms(i =>
            i.Field(f => f.Id).Terms(userIds)));

        QueryContainer Filter(QueryContainerDescriptor<ZealyXpScoreIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var (totalCount, data) = await _zealyXpScoreRepository.GetListAsync(Filter);
        return data;
    }
}