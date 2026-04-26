using FileShare.Application.Abstractions.Persistence;
using FileShare.Application.Common.CQRS;
using FileShare.Application.Common.Errors;
using FileShare.Application.Features.TemporaryFiles.UploadFile;
using FileShare.Domain.Abstractions;
using MediatR;

namespace FileShare.Application.Features.TemporaryFiles.GetFileMetadata;

public sealed record GetFileMetadataQuery(string AccessToken) : IQuery<Result<FileMetadataDto>>;

public sealed record FileMetadataDto(
    Guid Id,
    string FileName,
    string ContentType,
    long Size,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    int DownloadCount,
    int? MaxDownloads,
    string Status,
    bool HasPassword,
    TransferProofDto Proof,
    MalwareScanDto Scan);

public sealed class GetFileMetadataQueryHandler : IRequestHandler<GetFileMetadataQuery, Result<FileMetadataDto>>
{
    private readonly IFileRepository _fileRepository;

    public GetFileMetadataQueryHandler(IFileRepository fileRepository)
    {
        _fileRepository = fileRepository;
    }

    public async Task<Result<FileMetadataDto>> Handle(GetFileMetadataQuery request, CancellationToken cancellationToken)
    {
        var temporaryFile = await _fileRepository.GetByAccessTokenAsync(request.AccessToken, cancellationToken);
        if (temporaryFile is null)
        {
            return Result<FileMetadataDto>.Failure(ApplicationErrors.TemporaryFileNotFound(request.AccessToken));
        }

        return Result<FileMetadataDto>.Success(new FileMetadataDto(
            temporaryFile.Id,
            temporaryFile.FileName,
            temporaryFile.ContentType,
            temporaryFile.Size,
            temporaryFile.CreatedAt,
            temporaryFile.ExpiresAt,
            temporaryFile.DownloadCount,
            temporaryFile.MaxDownloads,
            temporaryFile.Status.ToString(),
            temporaryFile.HasPassword,
            new TransferProofDto(
                temporaryFile.FileHash,
                temporaryFile.BlockNumber,
                temporaryFile.BlockHash,
                temporaryFile.Signature,
                temporaryFile.ProofIssuedAt),
            new MalwareScanDto(
                temporaryFile.ScanStatus.ToString(),
                temporaryFile.ScanMaliciousCount,
                temporaryFile.ScanSuspiciousCount,
                temporaryFile.ScanTotalEngines,
                temporaryFile.ScannedAt,
                temporaryFile.ScanPermalink,
                temporaryFile.ScanIsEmulated)));
    }
}
