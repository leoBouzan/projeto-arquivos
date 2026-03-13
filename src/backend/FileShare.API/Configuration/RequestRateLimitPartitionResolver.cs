using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace FileShare.API.Configuration;

public static class RequestRateLimitPartitionResolver
{
    public static RateLimitPartition<string> ResolveOperationPartition(HttpContext context, SecurityOptions securityOptions)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? string.Empty;
        var ipAndDeviceKey = RequestPartitionKeyResolver.ResolveIpAndDeviceKey(context, securityOptions.DeviceCookieName);

        if (HttpMethods.IsPost(method) && path.Equals("/api/files", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                $"{RateLimitPolicyNames.Upload}:{ipAndDeviceKey}",
                _ => RateLimiterOptionsFactory.Create(securityOptions.RateLimiting.Upload));
        }

        if (HttpMethods.IsDelete(method) && path.StartsWith("/api/files/", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                $"{RateLimitPolicyNames.Delete}:{ipAndDeviceKey}",
                _ => RateLimiterOptionsFactory.Create(securityOptions.RateLimiting.Delete));
        }

        if (HttpMethods.IsGet(method) && path.StartsWith("/api/files/", StringComparison.OrdinalIgnoreCase))
        {
            if (path.EndsWith("/download", StringComparison.OrdinalIgnoreCase))
            {
                return RateLimitPartition.GetFixedWindowLimiter(
                    $"{RateLimitPolicyNames.PublicDownload}:{ipAndDeviceKey}",
                    _ => RateLimiterOptionsFactory.Create(securityOptions.RateLimiting.PublicDownload));
            }

            if (path.EndsWith("/metadata", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith("/availability", StringComparison.OrdinalIgnoreCase))
            {
                return RateLimitPartition.GetFixedWindowLimiter(
                    $"{RateLimitPolicyNames.PublicRead}:{ipAndDeviceKey}",
                    _ => RateLimiterOptionsFactory.Create(securityOptions.RateLimiting.PublicRead));
            }
        }

        return RateLimitPartition.GetNoLimiter("no-operation-limit");
    }
}
