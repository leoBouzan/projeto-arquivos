using Microsoft.AspNetCore.Http;

namespace FileShare.API.Configuration;

public static class RequestPartitionKeyResolver
{
    public const string DeviceIdItemKey = "FileShare.DeviceId";

    public static string ResolveIpKey(HttpContext context)
    {
        return ResolveIpAddress(context);
    }

    public static string ResolveIpAndDeviceKey(HttpContext context, string cookieName)
    {
        return $"{ResolveIpAddress(context)}:{ResolveDeviceId(context, cookieName)}";
    }

    private static string ResolveDeviceId(HttpContext context, string cookieName)
    {
        if (context.Items.TryGetValue(DeviceIdItemKey, out var item) &&
            item is string deviceId &&
            !string.IsNullOrWhiteSpace(deviceId))
        {
            return deviceId;
        }

        if (context.Request.Cookies.TryGetValue(cookieName, out var cookieDeviceId) &&
            !string.IsNullOrWhiteSpace(cookieDeviceId))
        {
            return cookieDeviceId;
        }

        return "anonymous-device";
    }

    private static string ResolveIpAddress(HttpContext context)
    {
        var remoteIpAddress = context.Connection.RemoteIpAddress;
        if (remoteIpAddress is null)
        {
            return "unknown-ip";
        }

        if (remoteIpAddress.IsIPv4MappedToIPv6)
        {
            remoteIpAddress = remoteIpAddress.MapToIPv4();
        }

        return remoteIpAddress.ToString();
    }
}
