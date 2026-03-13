using FileShare.Application.Abstractions.Storage;
using FileShare.Domain.TemporaryFiles;
using FileShare.Infrastructure.Configuration;
using FileShare.Infrastructure.Paths;
using Microsoft.Extensions.Options;

namespace FileShare.Infrastructure.Storage.Local;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage(IOptions<StorageOptions> options)
    {
        var configuredRootPath = options.Value.RootPath;
        _rootPath = Path.IsPathRooted(configuredRootPath)
            ? configuredRootPath
            : PathResolver.ResolveFromRepositoryRoot(configuredRootPath);
    }

    public async Task UploadAsync(StorageObjectKey storageObjectKey, Stream content, string contentType, CancellationToken cancellationToken)
    {
        var fullPath = BuildFullPath(storageObjectKey.Value);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        await using var destination = File.Create(fullPath);
        await content.CopyToAsync(destination, cancellationToken);
    }

    public Task<Stream?> OpenReadAsync(StorageObjectKey storageObjectKey, CancellationToken cancellationToken)
    {
        var fullPath = BuildFullPath(storageObjectKey.Value);
        Stream? stream = File.Exists(fullPath) ? File.OpenRead(fullPath) : null;
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(StorageObjectKey storageObjectKey, CancellationToken cancellationToken)
    {
        var fullPath = BuildFullPath(storageObjectKey.Value);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(StorageObjectKey storageObjectKey, CancellationToken cancellationToken)
    {
        var fullPath = BuildFullPath(storageObjectKey.Value);
        return Task.FromResult(File.Exists(fullPath));
    }

    private string BuildFullPath(string storageKey)
    {
        var safeKey = storageKey.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(_rootPath, safeKey);
    }
}
