using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SchrodingerServer.Common;
using SchrodingerServer.Options;

namespace SchrodingerServer.Middleware;

public class IpWhitelistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IpWhitelistMiddleware> _logger;
    private readonly IOptionsMonitor<IpWhiteListOptions> _ipWhiteListOptions;

    public IpWhitelistMiddleware(RequestDelegate next, ILogger<IpWhitelistMiddleware> logger,
        IOptionsMonitor<IpWhiteListOptions> ipWhiteListOptions)
    {
        _next = next;
        _logger = logger;
        _ipWhiteListOptions = ipWhiteListOptions;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            if (!IsProtectedResource(context.Request, out var whiteList))
            {
                await _next(context);
                return;
            }

            if (DeviceInfoContext.CurrentDeviceInfo == null ||
                CollectionUtilities.IsNullOrEmpty(DeviceInfoContext.CurrentDeviceInfo.ClientIp))
            {
                _logger.LogWarning("Protected resource requested from empty IP");
                await _next(context);
                return;
            }

            _logger.LogDebug("Protected resource requested from IP: {ClientIp}, Target:{Path}",
                DeviceInfoContext.CurrentDeviceInfo.ClientIp, context.Request.Path.ToString());

            if (!IpHelper.AssertAllowed(DeviceInfoContext.CurrentDeviceInfo.ClientIp, whiteList))
            {
                _logger.LogWarning("Forbidden request from IP: {ClientIp}, Target:{Path}",
                    DeviceInfoContext.CurrentDeviceInfo.ClientIp, context.Request.Path.ToString());
                context.Response.StatusCode = 403;
                return;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "IpWhitelistMiddleware handle error!");
        }

        await _next(context);
    }

    private bool IsProtectedResource(HttpRequest request, out string[] whiteList)
    {
        whiteList = null;
        if (_ipWhiteListOptions?.CurrentValue == null || _ipWhiteListOptions.CurrentValue.ByPath.IsNullOrEmpty())
        {
            return false;
        }

        foreach (var path in _ipWhiteListOptions.CurrentValue.ByPath.Keys.Where(path =>
                     PathHelper.IsPathMatch(request.Path, path)))
        {
            whiteList = _ipWhiteListOptions.CurrentValue.ByPath[path].Split(CommonConstant.Comma);
            return true;
        }

        return false;
    }
}