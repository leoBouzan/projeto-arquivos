using FileShare.Application.Abstractions.Persistence;
using FileShare.Application.Abstractions.Time;
using FileShare.Application.Features.TemporaryFiles.CheckFileAvailability;
using FileShare.Domain.TemporaryFiles;

namespace FileShare.Application.Tests;

public sealed class CheckFileAvailabilityQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenFileDoesNotExist()
    {
        var handler = new CheckFileAvailabilityQueryHandler(new FakeFileRepository(), new FakeDateTimeProvider());

        var result = await handler.Handle(new CheckFileAvailabilityQuery("missing"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("temporary_file.not_found", result.Errors[0].Code);
    }

    private sealed class FakeFileRepository : IFileRepository
    {
        public Task AddAsync(TemporaryFile temporaryFile, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> AccessTokenExistsAsync(string accessToken, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<IReadOnlyList<TemporaryFile>> GetExpiredBatchAsync(DateTimeOffset now, int batchSize, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<TemporaryFile>>([]);

        public Task<TemporaryFile?> GetByAccessTokenAsync(string accessToken, CancellationToken cancellationToken)
            => Task.FromResult<TemporaryFile?>(null);

        public Task<TemporaryFile?> GetByFileHashPrefixAsync(string fileHashPrefix, CancellationToken cancellationToken)
            => Task.FromResult<TemporaryFile?>(null);

        public Task<TemporaryFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult<TemporaryFile?>(null);

        public Task<IReadOnlyList<TemporaryFile>> GetPendingCleanupBatchAsync(int batchSize, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<TemporaryFile>>([]);
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
