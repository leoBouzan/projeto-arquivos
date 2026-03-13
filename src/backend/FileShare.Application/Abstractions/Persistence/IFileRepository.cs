using FileShare.Domain.TemporaryFiles;

namespace FileShare.Application.Abstractions.Persistence;

public interface IFileRepository
{
    Task AddAsync(TemporaryFile temporaryFile, CancellationToken cancellationToken);

    Task<bool> AccessTokenExistsAsync(string accessToken, CancellationToken cancellationToken);

    Task<TemporaryFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<TemporaryFile?> GetByAccessTokenAsync(string accessToken, CancellationToken cancellationToken);

    Task<IReadOnlyList<TemporaryFile>> GetExpiredBatchAsync(DateTimeOffset now, int batchSize, CancellationToken cancellationToken);

    Task<IReadOnlyList<TemporaryFile>> GetPendingCleanupBatchAsync(int batchSize, CancellationToken cancellationToken);
}
