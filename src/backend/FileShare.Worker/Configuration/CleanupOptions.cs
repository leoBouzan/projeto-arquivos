namespace FileShare.Worker.Configuration;

public sealed class CleanupOptions
{
    public const string SectionName = "Cleanup";

    public int IntervalSeconds { get; set; } = 30;

    public int BatchSize { get; set; } = 100;
}
