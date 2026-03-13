using FileShare.Domain.TemporaryFiles;

namespace FileShare.Domain.Tests;

public sealed class TemporaryFileTests
{
    [Fact]
    public void RegisterDownload_ShouldExpireFile_WhenMaxDownloadsIsReached()
    {
        var now = DateTimeOffset.UtcNow;
        var file = CreateTemporaryFile(now, now.AddHours(1), 1);

        var result = file.RegisterDownload(now.AddMinutes(5));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, file.DownloadCount);
        Assert.Equal(TemporaryFileStatus.Expired, file.Status);
    }

    [Fact]
    public void CanBeDownloaded_ShouldFail_WhenFileExpiredByTime()
    {
        var now = DateTimeOffset.UtcNow;
        var file = CreateTemporaryFile(now.AddHours(-2), now.AddHours(-1), 5);

        var result = file.CanBeDownloaded(now);

        Assert.True(result.IsFailure);
        Assert.Equal(TemporaryFileErrors.Expired, result.Errors[0]);
    }

    private static TemporaryFile CreateTemporaryFile(DateTimeOffset createdAt, DateTimeOffset expiresAt, int? maxDownloads)
    {
        var accessToken = AccessToken.Generate();
        var storageKey = StorageObjectKey.Create("temporary-files/test/file.txt").Value;
        var expirationPolicy = ExpirationPolicy.Create(expiresAt, maxDownloads, createdAt.AddSeconds(-1)).Value;
        return TemporaryFile.Create(
            Guid.NewGuid(),
            "file.txt",
            "text/plain",
            10,
            storageKey,
            accessToken,
            expirationPolicy,
            createdAt).Value;
    }
}
