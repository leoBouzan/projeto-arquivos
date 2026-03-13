namespace FileShare.Worker.Tests;

public sealed class WorkerSmokeTests
{
    [Fact]
    public void WorkerAssembly_ShouldLoad()
    {
        Assert.NotNull(typeof(FileShare.Worker.Services.ExpiredFileCleanupBackgroundService).Assembly);
    }
}
