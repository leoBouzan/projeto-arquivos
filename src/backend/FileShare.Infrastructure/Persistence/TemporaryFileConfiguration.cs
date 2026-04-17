using FileShare.Domain.TemporaryFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileShare.Infrastructure.Persistence;

public sealed class TemporaryFileConfiguration : IEntityTypeConfiguration<TemporaryFile>
{
    public void Configure(EntityTypeBuilder<TemporaryFile> builder)
    {
        builder.ToTable("temporary_files");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.StoragePath).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Size).IsRequired();
        builder.Property(x => x.AccessToken).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.DownloadCount).IsRequired();
        builder.Property(x => x.MaxDownloads);
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.DeletedAt);
        builder.Property(x => x.ExpiredAt);
        builder.Property(x => x.LastDownloadedAt);
        builder.Property(x => x.StorageDeletedAt);
        builder.Property(x => x.RowVersion).IsConcurrencyToken().IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(512);
        builder.Property(x => x.FileHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.BlockNumber).IsRequired();
        builder.Property(x => x.BlockHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Signature).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ProofIssuedAt).IsRequired();

        builder.HasIndex(x => x.AccessToken).IsUnique();
        builder.HasIndex(x => new { x.Status, x.ExpiresAt });
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.FileHash);
    }
}
