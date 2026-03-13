using FileShare.Domain.TemporaryFiles;

namespace FileShare.Application.Abstractions.Storage;

public interface IFileStorage
{
    Task UploadAsync(StorageObjectKey storageObjectKey, Stream content, string contentType, CancellationToken cancellationToken);

    Task<Stream?> OpenReadAsync(StorageObjectKey storageObjectKey, CancellationToken cancellationToken);

    Task DeleteAsync(StorageObjectKey storageObjectKey, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(StorageObjectKey storageObjectKey, CancellationToken cancellationToken);
}

public sealed record FileDownloadDescriptor(Stream Content, string FileName, string ContentType, long Size);
