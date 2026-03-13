using FileShare.Application.Abstractions.Persistence;
using FileShare.Domain.TemporaryFiles;
using Microsoft.EntityFrameworkCore;

namespace FileShare.Infrastructure.Persistence.Repositories;

public sealed class FileRepository : IFileRepository
{
    private readonly FileShareDbContext _dbContext;

    public FileRepository(FileShareDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(TemporaryFile temporaryFile, CancellationToken cancellationToken)
    {
        return _dbContext.TemporaryFiles.AddAsync(temporaryFile, cancellationToken).AsTask();
    }

    public Task<bool> AccessTokenExistsAsync(string accessToken, CancellationToken cancellationToken)
    {
        return _dbContext.TemporaryFiles.AnyAsync(x => x.AccessToken == accessToken, cancellationToken);
    }

    public Task<TemporaryFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.TemporaryFiles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<TemporaryFile?> GetByAccessTokenAsync(string accessToken, CancellationToken cancellationToken)
    {
        return _dbContext.TemporaryFiles.FirstOrDefaultAsync(x => x.AccessToken == accessToken, cancellationToken);
    }

    public async Task<IReadOnlyList<TemporaryFile>> GetExpiredBatchAsync(DateTimeOffset now, int batchSize, CancellationToken cancellationToken)
    {
        var candidates = await _dbContext.TemporaryFiles
            .Where(x => x.Status == TemporaryFileStatus.Available)
            .ToListAsync(cancellationToken);

        return candidates
            .Where(x => x.ExpiresAt <= now)
            .OrderBy(x => x.ExpiresAt)
            .Take(batchSize)
            .ToList();
    }

    public async Task<IReadOnlyList<TemporaryFile>> GetPendingCleanupBatchAsync(int batchSize, CancellationToken cancellationToken)
    {
        var candidates = await _dbContext.TemporaryFiles
            .Where(x => x.StorageDeletedAt == null && (x.Status == TemporaryFileStatus.Expired || x.Status == TemporaryFileStatus.Deleted))
            .ToListAsync(cancellationToken);

        return candidates
            .OrderBy(x => x.ExpiresAt)
            .Take(batchSize)
            .ToList();
    }
}
