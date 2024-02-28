using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SchrodingerServer.Common;
using SchrodingerServer.PointServer;
using SchrodingerServer.PointServer.Dto;
using SchrodingerServer.Users;
using SchrodingerServer.Users.Eto;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace SchrodingerServer.EntityEventHandler.Core.UserInformationHandler;

public class RegisterPointServerHandler : IDistributedEventHandler<UserInformationEto>,
    ITransientDependency
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<RegisterPointServerHandler> _logger;
    private readonly IUserInformationProvider _userInformationProvider;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IPointServerProvider _pointServerProvider;

    public RegisterPointServerHandler(IObjectMapper objectMapper, ILogger<RegisterPointServerHandler> logger,
        IUserInformationProvider userInformationProvider, IAbpDistributedLock distributedLock, IPointServerProvider pointServerProvider)
    {
        _objectMapper = objectMapper;
        _logger = logger;
        _userInformationProvider = userInformationProvider;
        _distributedLock = distributedLock;
        _pointServerProvider = pointServerProvider;
    }


    public async Task HandleEventAsync(UserInformationEto eventData)
    {
        try
        {
            AssertHelper.NotNull(eventData, "Empty event");
            if (eventData.PointRegisterTime > 0) return;

            await using var locked = await _distributedLock.TryAcquireAsync("UserInviteToPointService_" + eventData.Id);

            var userGrainDto = await _userInformationProvider.GetUserById(eventData.Id);
            if (userGrainDto.PointRegisterTime > 0) return;
            
            userGrainDto.PointRegisterTime = DateTime.UtcNow.ToUtcMilliSeconds();
            
            await _pointServerProvider.InvitationRelationshipsAsync(new InvitationRequest
            {
                Address = userGrainDto.CaAddressMain,
                InviteTime = userGrainDto.PointRegisterTime,
                Domain = userGrainDto.RegisterDomain
            });

            await _userInformationProvider.SaveUserSourceAsync(userGrainDto);
            _logger.LogDebug("HandleEventAsync UserInformationEto success");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", JsonConvert.SerializeObject(eventData));
        }
    }
}