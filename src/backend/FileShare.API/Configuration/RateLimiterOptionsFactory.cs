using System.Threading.RateLimiting;

namespace FileShare.API.Configuration;

public static class RateLimiterOptionsFactory
{
    public static FixedWindowRateLimiterOptions Create(FixedWindowPolicyOptions policyOptions)
    {
        return new FixedWindowRateLimiterOptions
        {
            PermitLimit = policyOptions.PermitLimit,
            Window = TimeSpan.FromSeconds(policyOptions.WindowSeconds),
            QueueLimit = policyOptions.QueueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        };
    }
}
