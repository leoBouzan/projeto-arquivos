using FileShare.Domain.TemporaryFiles;
using Microsoft.EntityFrameworkCore;

namespace FileShare.Infrastructure.Persistence;

public sealed class FileShareDbContext : DbContext
{
    public FileShareDbContext(DbContextOptions<FileShareDbContext> options)
        : base(options)
    {
    }

    public DbSet<TemporaryFile> TemporaryFiles => Set<TemporaryFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FileShareDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<TemporaryFile>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(x => x.RowVersion).CurrentValue = 1;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(x => x.RowVersion).CurrentValue = entry.Entity.RowVersion + 1;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
