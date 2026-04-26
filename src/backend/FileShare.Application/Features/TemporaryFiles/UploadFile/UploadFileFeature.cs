using System.Security.Cryptography;
using FileShare.Application.Abstractions.Persistence;
using FileShare.Application.Abstractions.Scanning;
using FileShare.Application.Abstractions.Security;
using FileShare.Application.Abstractions.Storage;
using FileShare.Application.Abstractions.Time;
using FileShare.Application.Common.CQRS;
using FileShare.Domain.Abstractions;
using FileShare.Domain.TemporaryFiles;
using FluentValidation;
using MediatR;

namespace FileShare.Application.Features.TemporaryFiles.UploadFile;

public sealed record UploadFileCommand(
    string FileName,
    string ContentType,
    long Size,
    DateTimeOffset ExpiresAt,
    int? MaxDownloads,
    Stream Content,
    string? Password) : ICommand<Result<UploadFileResponse>>;

public sealed record UploadFileResponse(
    Guid Id,
    string AccessToken,
    string FileName,
    DateTimeOffset ExpiresAt,
    int? MaxDownloads,
    bool HasPassword,
    TransferProofDto Proof,
    MalwareScanDto Scan);

public sealed record TransferProofDto(
    string FileHash,
    long BlockNumber,
    string BlockHash,
    string Signature,
    DateTimeOffset IssuedAt);

public sealed record MalwareScanDto(
    string Status,
    int? MaliciousCount,
    int? SuspiciousCount,
    int? TotalEngines,
    DateTimeOffset? ScannedAt,
    string? Permalink,
    bool IsEmulated);

public sealed class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    public UploadFileCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty();
        RuleFor(x => x.ContentType).NotEmpty();
        RuleFor(x => x.Size).GreaterThan(0);
        RuleFor(x => x.Content).NotNull();
        RuleFor(x => x.Password)
            .MinimumLength(4)
            .MaximumLength(128)
            .When(x => !string.IsNullOrEmpty(x.Password));
    }
}

public sealed class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, Result<UploadFileResponse>>
{
    private readonly IFileRepository _fileRepository;
    private readonly IFileStorage _fileStorage;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMalwareScanner _malwareScanner;
    private readonly IMalwareScanPolicy _malwareScanPolicy;

    public UploadFileCommandHandler(
        IFileRepository fileRepository,
        IFileStorage fileStorage,
        IDateTimeProvider dateTimeProvider,
        IPasswordHasher passwordHasher,
        IMalwareScanner malwareScanner,
        IMalwareScanPolicy malwareScanPolicy)
    {
        _fileRepository = fileRepository;
        _fileStorage = fileStorage;
        _dateTimeProvider = dateTimeProvider;
        _passwordHasher = passwordHasher;
        _malwareScanner = malwareScanner;
        _malwareScanPolicy = malwareScanPolicy;
    }

    public async Task<Result<UploadFileResponse>> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;

        var expirationPolicyResult = ExpirationPolicy.Create(request.ExpiresAt, request.MaxDownloads, now);
        if (expirationPolicyResult.IsFailure)
        {
            return Result<UploadFileResponse>.Failure(expirationPolicyResult.Errors);
        }

        var storageObjectKeyResult = StorageObjectKey.Create(BuildStorageObjectKey(request.FileName));
        if (storageObjectKeyResult.IsFailure)
        {
            return Result<UploadFileResponse>.Failure(storageObjectKeyResult.Errors);
        }

        var accessToken = await GenerateUniqueAccessTokenAsync(cancellationToken);

        var (hashStream, fileHash) = await BufferAndHashAsync(request.Content, cancellationToken);
        await using (hashStream)
        {
            var scanResult = await _malwareScanner.ScanByHashAsync(fileHash, cancellationToken);

            if (scanResult.Status == MalwareScanStatus.Malicious && _malwareScanPolicy.BlockOnMalicious)
            {
                return Result<UploadFileResponse>.Failure(TemporaryFileErrors.MalwareDetected);
            }

            var proofResult = TransferProof.Create(fileHash, now);
            if (proofResult.IsFailure)
            {
                return Result<UploadFileResponse>.Failure(proofResult.Errors);
            }

            var passwordHash = string.IsNullOrEmpty(request.Password)
                ? null
                : _passwordHasher.Hash(request.Password);

            var aggregateResult = TemporaryFile.Create(
                Guid.NewGuid(),
                request.FileName,
                request.ContentType,
                request.Size,
                storageObjectKeyResult.Value,
                accessToken,
                expirationPolicyResult.Value,
                proofResult.Value,
                now,
                passwordHash);

            if (aggregateResult.IsFailure)
            {
                return Result<UploadFileResponse>.Failure(aggregateResult.Errors);
            }

            aggregateResult.Value.ApplyMalwareScanResult(
                scanResult.Status,
                scanResult.MaliciousCount,
                scanResult.SuspiciousCount,
                scanResult.TotalEngines,
                scanResult.AnalyzedAt ?? now,
                scanResult.Permalink,
                scanResult.IsEmulated);

            await _fileStorage.UploadAsync(storageObjectKeyResult.Value, hashStream, request.ContentType, cancellationToken);
            await _fileRepository.AddAsync(aggregateResult.Value, cancellationToken);

            var aggregate = aggregateResult.Value;
            return Result<UploadFileResponse>.Success(new UploadFileResponse(
                aggregate.Id,
                aggregate.AccessToken,
                aggregate.FileName,
                aggregate.ExpiresAt,
                aggregate.MaxDownloads,
                aggregate.HasPassword,
                new TransferProofDto(
                    aggregate.FileHash,
                    aggregate.BlockNumber,
                    aggregate.BlockHash,
                    aggregate.Signature,
                    aggregate.ProofIssuedAt),
                new MalwareScanDto(
                    aggregate.ScanStatus.ToString(),
                    aggregate.ScanMaliciousCount,
                    aggregate.ScanSuspiciousCount,
                    aggregate.ScanTotalEngines,
                    aggregate.ScannedAt,
                    aggregate.ScanPermalink,
                    aggregate.ScanIsEmulated)));
        }
    }

    private static async Task<(Stream Stream, string Hash)> BufferAndHashAsync(Stream source, CancellationToken cancellationToken)
    {
        var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var hashBytes = await SHA256.HashDataAsync(buffer, cancellationToken);
        buffer.Position = 0;

        return (buffer, Convert.ToHexString(hashBytes).ToLowerInvariant());
    }

    private async Task<AccessToken> GenerateUniqueAccessTokenAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var accessToken = AccessToken.Generate();
            if (!await _fileRepository.AccessTokenExistsAsync(accessToken.Value, cancellationToken))
            {
                return accessToken;
            }
        }

        return AccessToken.Generate();
    }

    private static string BuildStorageObjectKey(string fileName)
    {
        var safeFileName = Path.GetFileName(fileName).Replace(' ', '-').ToLowerInvariant();
        return $"temporary-files/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}-{safeFileName}";
    }
}
