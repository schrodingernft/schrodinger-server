using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Schrodinger;
using SchrodingerServer.Common;
using SchrodingerServer.Grains.Grain.ContractInvoke;
using SchrodingerServer.Options;
using SchrodingerServer.Users.Dto;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using UserPoints = Schrodinger.UserPoints;

namespace SchrodingerServer.Users;

public interface IPointSettleService
{
    Task BatchSettleAsync(PointSettleDto dto);
}

public class PointSettleService : IPointSettleService, ISingletonDependency
{
    private readonly ILogger<PointSettleService> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IOptionsMonitor<PointTradeOptions> _pointTradeOptions;

    public PointSettleService(ILogger<PointSettleService> logger, IClusterClient clusterClient,
        IOptionsMonitor<PointTradeOptions> pointTradeOptions)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _pointTradeOptions = pointTradeOptions;
    }

    public async Task BatchSettleAsync(PointSettleDto dto)
    {
        AssertHelper.NotEmpty(dto.BizId, "Invalid bizId.");
        _logger.LogInformation("BatchSettle bizId:{bizId}", dto.BizId);
        var userPoints = dto.UserPointsInfos
            .Where(item => item.PointAmount > 0)
            .Select(item => new UserPoints
            {
                UserAddress = Address.FromBase58(item.Address),
                UserPoints_ = DecimalHelper.ConvertToLong(item.PointAmount, 0)
            }).ToList();
        var actionName = _pointTradeOptions.CurrentValue.GetActionName(dto.PointName);
        var batchSettleInput = new BatchSettleInput()
        {
            ActionName = actionName,
            UserPointsList = { userPoints }
        };
        var input = new ContractInvokeGrainDto()
        {
            ChainId = dto.ChainId,
            BizId = dto.BizId,
            BizType = dto.PointName,
            ContractAddress = _pointTradeOptions.CurrentValue.ContractAddress,
            ContractMethod = _pointTradeOptions.CurrentValue.ContractMethod,
            Param = batchSettleInput
        };
        var contractInvokeGrain = _clusterClient.GetGrain<IContractInvokeGrain>(dto.BizId);
        var result = await contractInvokeGrain.CreateAsync(input);
        if (!result.Success)
        {
            _logger.LogError(
                "Create Contract Invoke fail, bizId: {dto.BizId}.", dto.BizId);
            throw new UserFriendlyException($"Create Contract Invoke fail, bizId: {dto.BizId}.");
        }
    }
}