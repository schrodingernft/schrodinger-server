using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SchrodingerServer.Common;
using SchrodingerServer.Points.Provider;
using SchrodingerServer.Users.Dto;
using SchrodingerServer.Users.Index;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.Points;

public interface IPointAssemblyTransactionService
{
    Task AssembleAsync(string chainId, string bizDate);
}

public class PointAssemblyTransactionService : IPointAssemblyTransactionService, ISingletonDependency
{
    private const int MaxBatchSize = 10;
    private readonly ILogger<PointAssemblyTransactionService> _logger;
    private readonly IPointSettleService _pointSettleService;
    private readonly IPointDailyRecordProvider _pointDailyRecordProvider;
    
    public PointAssemblyTransactionService(IPointSettleService pointSettleService,
        ILogger<PointAssemblyTransactionService> logger, IPointDailyRecordProvider pointDailyRecordProvider)
    {
        _pointSettleService = pointSettleService;
        _logger = logger;
        _pointDailyRecordProvider = pointDailyRecordProvider;
    }

    public async Task AssembleAsync(string chainId, string bizDate)
    {
        var skipCount = 0;
        List<PointDailyRecordIndex> pointDailyRecords;
        do
        {
            pointDailyRecords = await _pointDailyRecordProvider.GetPointDailyRecordsAsync(chainId, bizDate, skipCount);
            _logger.LogInformation(
                "GetPointDailyRecords chainId:{chainId} bizDate:{bizDate}  skipCount: {skipCount} count: {count}",
                chainId, bizDate, skipCount, pointDailyRecords?.Count);
            if (pointDailyRecords.IsNullOrEmpty())
            {
                break;
            }

            var assemblyDict = pointDailyRecords.GroupBy(balance => balance.PointName)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToList()
                );


            foreach (var (pointName, records) in assemblyDict)
            {
                var batchList = SplitList(records, MaxBatchSize);

                foreach (var tradeList in batchList)
                {
                    var pointSettleDto = new PointSettleDto
                    {
                        ChainId = chainId,
                        PointName = pointName,
                        BizId = IdGenerateHelper.GetPointBizId(chainId, bizDate, pointName, Guid.NewGuid().ToString()),
                        UserPointsInfos = tradeList.Select(item => new UserPointInfo
                        {
                            Address = item.Address,
                            PointAmount = item.PointAmount
                        }).ToList()
                    };

                    await _pointSettleService.BatchSettleAsync(pointSettleDto);
                }
            }

            skipCount += pointDailyRecords.Count;
        } while (!pointDailyRecords.IsNullOrEmpty());
    }
    
    private static List<List<PointDailyRecordIndex>> SplitList(List<PointDailyRecordIndex> records, int n)
    {
        return records
            .Select((item, index) => new { item, index })
            .GroupBy(x => x.index / n)
            .Select(g => g.Select(x => x.item).ToList())
            .ToList();
    }
}