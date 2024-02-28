using System;
using System.Threading.Tasks;
using SchrodingerServer.Common;

namespace SchrodingerServer.Users;

public interface IUserActionProvider
{


    Task<bool> CheckDomainAsync(string domain);
    
    Task<DateTime?> GetActionTimeAsync(ActionType actionType);

    Task<UserActionGrainDto> AddActionAsync(ActionType actionType);

}