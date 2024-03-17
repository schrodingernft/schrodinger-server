using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using SchrodingerServer.Background.Providers;
using SchrodingerServer.Common;
using SchrodingerServer.ContractInvoke.Index;
using SchrodingerServer.Zealy;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.Background.Services;

public interface IXpScoreResultService
{
    Task HandleXpResultAsync();
}

public class XpScoreResultService : IXpScoreResultService, ISingletonDependency
{
    private readonly IZealyProvider _zealyProvider;
    private readonly ILogger<XpScoreResultService> _logger;
    private readonly INESTRepository<ContractInvokeIndex, string> _contractInvokeIndexRepository;
    private const int FetchPendingCount = 300;
    private bool Start = false;

    public XpScoreResultService(IZealyProvider zealyProvider, ILogger<XpScoreResultService> logger,
        INESTRepository<ContractInvokeIndex, string> contractInvokeIndexRepository)
    {
        _zealyProvider = zealyProvider;
        _logger = logger;
        _contractInvokeIndexRepository = contractInvokeIndexRepository;
    }

    public async Task HandleXpResultAsync()
    {
        if (Start)
        {
            _logger.LogError("task already execute");
            return;
        }
        await HandleXpResultAsync(0, FetchPendingCount);
    }

    private async Task HandleXpResultAsync(int skipCount, int maxResultCount)
    {
        Start = true;
        var records = await _zealyProvider.GetPendingUserXpsAsync(skipCount, maxResultCount);
        if (records.IsNullOrEmpty())
        {
            _logger.LogInformation("no pending xp score records");
            return;
        }

        _logger.LogInformation("handle pending xp score records, count:{count}", records.Count);
        var bizIds = records.Select(t => t.Id).Distinct().ToList();
      //  var bizIds = records.Select(t => t.BizId).Distinct().ToList();

        
        // fix , need to remove
        for (var i = 0; i < bizIds.Count; i++)
        {
            try
            {
                if (bizIds[i].Contains(':'))
                {
                    var ids = bizIds[i].Split(':');
                    bizIds[i] = ids[0];
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e,"error");
            }
        }
        //

        // get transaction from trans
        var contractInfos = await GetContractInvokeTxByIdsAsync(bizIds);
        foreach (var record in records)
        {
            await HandleRecordAsync(record, contractInfos);
        }

        // if (records.Count < maxResultCount)
        // {
        //     return;
        // }
        //
        // var newSkipCount = skipCount + maxResultCount;
        // await HandleXpResultAsync(newSkipCount, maxResultCount);
    }

    private async Task HandleRecordAsync(ZealyUserXpRecordIndex record, List<ContractInvokeIndex> contractInfos)
    {
        try
        {
            //var contractInfo = contractInfos.FirstOrDefault(t => t.BizId == record.BizId);

            var tempId = string.Empty;
            if (record.Id.Contains(':'))
            {
                var ids = record.Id.Split(':');
                tempId = ids[0];
            }
            var contractInfo = contractInfos.FirstOrDefault(t => t.BizId == tempId);
            if (contractInfo == null)
            {
                return;
            }

            if (contractInfo.Status == ContractInvokeStatus.Success.ToString())
            {
                //update userxp
                var userXp = await _zealyProvider.GetUserXpByIdAsync(record.UserId);
                if (userXp == null)
                {
                    _logger.LogError("user not exist, userId:{userId}, recordId:{recordId}", record.UserId, record.Id);
                }
                else
                {
                    userXp.LastXp = userXp.Xp;
                    userXp.Xp = record.Xp;
                    userXp.UpdateTime = DateTime.UtcNow;
                    await _zealyProvider.UserXpAddOrUpdateAsync(userXp);
                }

                record.Status = ContractInvokeStatus.Success.ToString();
                record.UpdateTime = TimeHelper.GetTimeStampInSeconds();
            }

            if (contractInfo.Status == ContractInvokeStatus.FinalFailed.ToString())
            {
                record.Status = ContractInvokeStatus.FinalFailed.ToString();
                record.UpdateTime = TimeHelper.GetTimeStampInSeconds();
            }

            await _zealyProvider.XpRecordAddOrUpdateAsync(record);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "handle pending record error, record:{record}", JsonConvert.SerializeObject(record));
        }
    }

    private async Task<List<ContractInvokeIndex>> GetContractInvokeTxByIdsAsync(List<string> bizIds)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ContractInvokeIndex>, QueryContainer>>
        {
            q => q.Terms(i => i.Field(f => f.BizId).Terms(bizIds))
        };

        QueryContainer Filter(QueryContainerDescriptor<ContractInvokeIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var (totalCount, syncTxs) = await _contractInvokeIndexRepository.GetListAsync(Filter);

        return syncTxs;
    }
}