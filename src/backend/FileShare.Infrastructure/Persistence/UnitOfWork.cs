using FileShare.Application.Abstractions.Persistence;

namespace FileShare.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly FileShareDbContext _dbContext;

    public UnitOfWork(FileShareDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
