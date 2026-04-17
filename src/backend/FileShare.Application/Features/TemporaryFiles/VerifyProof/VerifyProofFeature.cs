using FileShare.Application.Abstractions.Persistence;
using FileShare.Application.Common.CQRS;
using FileShare.Application.Common.Errors;
using FileShare.Domain.Abstractions;
using MediatR;

namespace FileShare.Application.Features.TemporaryFiles.VerifyProof;

public sealed record VerifyProofQuery(string FileHashPrefix) : IQuery<Result<VerifyProofDto>>;

public sealed record VerifyProofDto(
    bool Verified,
    string FileName,
    long Size,
    string FileHash,
    long BlockNumber,
    string BlockHash,
    string Signature,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    string Status,
    int DownloadCount,
    int? MaxDownloads);

public sealed class VerifyProofQueryHandler : IRequestHandler<VerifyProofQuery, Result<VerifyProofDto>>
{
    private readonly IFileRepository _fileRepository;

    public VerifyProofQueryHandler(IFileRepository fileRepository)
    {
        _fileRepository = fileRepository;
    }

    public async Task<Result<VerifyProofDto>> Handle(VerifyProofQuery request, CancellationToken cancellationToken)
    {
        var normalized = (request.FileHashPrefix ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.Length < 8)
        {
            return Result<VerifyProofDto>.Failure(new Error(
                "proof.invalid_hash",
                "The hash prefix must be at least 8 characters.",
                ErrorType.Validation));
        }

        var file = await _fileRepository.GetByFileHashPrefixAsync(normalized, cancellationToken);
        if (file is null)
        {
            return Result<VerifyProofDto>.Failure(ApplicationErrors.TemporaryFileNotFound(normalized));
        }

        return Result<VerifyProofDto>.Success(new VerifyProofDto(
            true,
            file.FileName,
            file.Size,
            file.FileHash,
            file.BlockNumber,
            file.BlockHash,
            file.Signature,
            file.ProofIssuedAt,
            file.ExpiresAt,
            file.Status.ToString(),
            file.DownloadCount,
            file.MaxDownloads));
    }
}
