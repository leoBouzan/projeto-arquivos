using System.Security.Cryptography;
using FileShare.API.Configuration;
using Microsoft.Extensions.Options;

namespace FileShare.API.Middleware;

public sealed class DeviceFingerprintMiddleware
{
    private static readonly TimeSpan DeviceCookieLifetime = TimeSpan.FromDays(180);

    private readonly RequestDelegate _next;
    private readonly SecurityOptions _securityOptions;

    public DeviceFingerprintMiddleware(RequestDelegate next, IOptions<SecurityOptions> securityOptions)
    {
        _next = next;
        _securityOptions = securityOptions.Value;
    }

    public async Task Invoke(HttpContext context)
    {
        var deviceId = ResolveDeviceId(context);
        context.Items[RequestPartitionKeyResolver.DeviceIdItemKey] = deviceId;

        if (!context.Request.Cookies.ContainsKey(_securityOptions.DeviceCookieName))
        {
            context.Response.Cookies.Append(_securityOptions.DeviceCookieName, deviceId, new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = _securityOptions.RequireHttpsCookies || context.Request.IsHttps,
                MaxAge = DeviceCookieLifetime
            });
        }

        await _next(context);
    }

    private string ResolveDeviceId(HttpContext context)
    {
        if (context.Request.Cookies.TryGetValue(_securityOptions.DeviceCookieName, out var existingDeviceId) &&
            IsWellFormedDeviceId(existingDeviceId))
        {
            return existingDeviceId;
        }

        return Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
    }

    private static bool IsWellFormedDeviceId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 32)
        {
            return false;
        }

        foreach (var character in value)
        {
            var isHexLetter = character is >= 'a' and <= 'f';
            var isDigit = character is >= '0' and <= '9';
            if (!isHexLetter && !isDigit)
            {
                return false;
            }
        }

        return true;
    }
}
