using FileShare.Application.Abstractions.Persistence;
using FileShare.Application.Abstractions.Storage;
using FileShare.Application.Common.CQRS;
using FileShare.Application.Common.Errors;
using FileShare.Domain.Abstractions;
using FileShare.Domain.TemporaryFiles;
using MediatR;

namespace FileShare.Application.Features.TemporaryFiles.DownloadFile;

public sealed record DownloadFileQuery(string AccessToken) : IQuery<Result<FileDownloadDescriptor>>;

public sealed class DownloadFileQueryHandler : IRequestHandler<DownloadFileQuery, Result<FileDownloadDescriptor>>
{
    private readonly IFileRepository _fileRepository;
    private readonly IFileStorage _fileStorage;

    public DownloadFileQueryHandler(
        IFileRepository fileRepository,
        IFileStorage fileStorage)
    {
        _fileRepository = fileRepository;
        _fileStorage = fileStorage;
    }

    public async Task<Result<FileDownloadDescriptor>> Handle(DownloadFileQuery request, CancellationToken cancellationToken)
    {
        var temporaryFile = await _fileRepository.GetByAccessTokenAsync(request.AccessToken, cancellationToken);
        if (temporaryFile is null)
        {
            return Result<FileDownloadDescriptor>.Failure(ApplicationErrors.TemporaryFileNotFound(request.AccessToken));
        }

        var storageObjectKeyResult = StorageObjectKey.Create(temporaryFile.StoragePath);
        if (storageObjectKeyResult.IsFailure)
        {
            return Result<FileDownloadDescriptor>.Failure(storageObjectKeyResult.Errors);
        }

        var stream = await _fileStorage.OpenReadAsync(storageObjectKeyResult.Value, cancellationToken);
        if (stream is null)
        {
            return Result<FileDownloadDescriptor>.Failure(ApplicationErrors.StorageObjectMissing);
        }

        return Result<FileDownloadDescriptor>.Success(new FileDownloadDescriptor(
            stream,
            temporaryFile.FileName,
            temporaryFile.ContentType,
            temporaryFile.Size));
    }
}
