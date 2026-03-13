namespace FileShare.API.Configuration;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    public string DeviceCookieName { get; set; } = "fs_device";

    public bool RequireHttpsCookies { get; set; } = false;

    public long MaxUploadSizeBytes { get; set; } = 104_857_600;

    public string[] AllowedOrigins { get; set; } = ["http://localhost:4200"];

    public RateLimitingOptions RateLimiting { get; set; } = new();
}

public sealed class RateLimitingOptions
{
    public FixedWindowPolicyOptions GlobalIp { get; set; } = new()
    {
        PermitLimit = 300,
        WindowSeconds = 60
    };

    public FixedWindowPolicyOptions PublicRead { get; set; } = new()
    {
        PermitLimit = 60,
        WindowSeconds = 60
    };

    public FixedWindowPolicyOptions PublicDownload { get; set; } = new()
    {
        PermitLimit = 20,
        WindowSeconds = 60
    };

    public FixedWindowPolicyOptions Upload { get; set; } = new()
    {
        PermitLimit = 10,
        WindowSeconds = 300
    };

    public FixedWindowPolicyOptions Delete { get; set; } = new()
    {
        PermitLimit = 10,
        WindowSeconds = 300
    };
}

public sealed class FixedWindowPolicyOptions
{
    public int PermitLimit { get; set; } = 60;

    public int WindowSeconds { get; set; } = 60;

    public int QueueLimit { get; set; } = 0;
}
