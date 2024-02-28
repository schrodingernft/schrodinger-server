using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SchrodingerServer.Common;
using SchrodingerServer.Options;
using SchrodingerServer.Users;

namespace SchrodingerServer.Middleware;

public class HostWhitelistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HostWhitelistMiddleware> _logger;
    private readonly IOptionsMonitor<IpWhiteListOptions> _ipWhiteListOptions;
    private readonly IUserActionProvider _userActionProvider;

    public HostWhitelistMiddleware(RequestDelegate next, ILogger<HostWhitelistMiddleware> logger,
        IOptionsMonitor<IpWhiteListOptions> ipWhiteListOptions, IUserActionProvider userActionProvider)
    {
        _next = next;
        _logger = logger;
        _ipWhiteListOptions = ipWhiteListOptions;
        _userActionProvider = userActionProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            if (_ipWhiteListOptions.CurrentValue.HostWhiteList.IsNullOrEmpty())
            {
                await _next(context);
                return;
            }

            var hostHeader = _ipWhiteListOptions.CurrentValue.HostHeader ?? "Host";
            var headers = context.Request.Headers;
            var host = headers[hostHeader].FirstOrDefault() ?? CommonConstant.EmptyString;
            
            if (host.IsNullOrEmpty())
            {
                _logger.LogWarning("Protected resource requested from empty Host");
                await _next(context);
                return;
            }

            _logger.LogDebug("Protected resource requested from IP: {ClientIp}, Target:{Path}, Host:{Host}",
                DeviceInfoContext.CurrentDeviceInfo.ClientIp, context.Request.Path.ToString(), host);

            if (!_ipWhiteListOptions.CurrentValue.HostWhiteList.Any(k => host.Match(k)) 
                && !await _userActionProvider.CheckDomainAsync(host))
            {
                _logger.LogWarning("Request blocked from Host: {Host}", host);
                context.Response.StatusCode = 404;
                return;
            }

        }
        catch (Exception e)
        {
            _logger.LogError(e, "HostWhitelistMiddleware handle error!");
        }

        await _next(context);
    }
}