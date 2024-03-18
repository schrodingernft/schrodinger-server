using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using GraphQL;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SchrodingerServer.Common;
using SchrodingerServer.Common.GraphQL;
using SchrodingerServer.Options;
using SchrodingerServer.Points;
using SchrodingerServer.Users.Dto;
using SchrodingerServer.Users.Index;
using SchrodingerServer.Zealy;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.Background.Providers;

public interface ICallContractProvider
{
    Task CreateAsync(ZealyUserXpIndex userXp);
}

public class CallContractProvider : ICallContractProvider, ISingletonDependency
{
    private readonly IPointSettleService _pointSettleService;
    private readonly INESTRepository<ZealyUserXpRecordCleanUpIndex, string> _zealyUserXpRecordRepository;
    private readonly ZealyScoreOptions _options;
    private readonly ILogger<CallContractProvider> _logger;
    private readonly IGraphQlHelper _graphQlHelper;

    public CallContractProvider(INESTRepository<ZealyUserXpRecordCleanUpIndex, string> zealyUserXpRecordRepository,
        IOptionsSnapshot<ZealyScoreOptions> options,
        ILogger<CallContractProvider> logger,
        IPointSettleService pointSettleService, IGraphQlHelper graphQlHelper)
    {
        _zealyUserXpRecordRepository = zealyUserXpRecordRepository;
        _logger = logger;
        _pointSettleService = pointSettleService;
        _graphQlHelper = graphQlHelper;
        _options = options.Value;
    }

    public async Task CreateAsync(ZealyUserXpIndex userXp)
    {
        var recordId = $"{userXp.Id}-{DateTime.UtcNow:yyyy-MM-dd}-cleanup";
        _logger.LogInformation("begin create, recordId:{recordId}", recordId);

        var list = await GetOperatorPointsActionSumAsync(userXp.Address);
        var totalAmount = DecimalHelper.MultiplyByPowerOfTen(userXp.Xp * _options.Coefficient, 8);
        decimal pointAmount = 0m;
        var point = list.FirstOrDefault(t => t.PointsName == "XPSGR-4");
        if (point != null && point.Amount > 0)
        {
            pointAmount = point.Amount;
        }

        var record = new ZealyUserXpRecordCleanUpIndex
        {
            Id = recordId,
            CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Xp = userXp.Xp,
            Amount = totalAmount - pointAmount,
            RawAmount = pointAmount,
            TotalAmount = totalAmount,
            BizId = string.Empty,
            Status = ContractInvokeStatus.ToBeCreated.ToString(),
            UserId = userXp.Id,
            Address = userXp.Address
        };

        await _zealyUserXpRecordRepository.AddOrUpdateAsync(record);
        _logger.LogInformation("end create, recordId:{recordId},totalAmount:{totalAmount}, pointAmount:{pointAmount}",
            recordId, totalAmount, pointAmount);
    }

    public async Task<List<RankingDetailIndexerDto>> GetOperatorPointsActionSumAsync(
        string address)
    {
        var indexerResult = await _graphQlHelper.QueryAsync<RankingDetailIndexerQueryDto>(new GraphQLRequest
        {
            Query =
                @"query($dappId:String!, $address:String!, $domain:String!){
                    getPointsSumByAction(input: {dappId:$dappId,address:$address,domain:$domain}){
                        totalRecordCount,
                        data{
                        id,
                        address,
                        domain,
                        role,
                        dappId,
    					pointsName,
    					actionName,
    					amount,
    					createTime,
    					updateTime
                    }
                }
            }",
            Variables = new
            {
                dappId = string.Empty,
                domain = string.Empty,
                address = address
            }
        });

        return indexerResult.GetPointsSumByAction.Data;
    }
}