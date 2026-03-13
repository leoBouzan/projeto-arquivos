using FileShare.Application.Abstractions.Persistence;
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
    Stream Content) : ICommand<Result<UploadFileResponse>>;

public sealed record UploadFileResponse(Guid Id, string AccessToken, string FileName, DateTimeOffset ExpiresAt, int? MaxDownloads);

public sealed class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    public UploadFileCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty();
        RuleFor(x => x.ContentType).NotEmpty();
        RuleFor(x => x.Size).GreaterThan(0);
        RuleFor(x => x.Content).NotNull();
    }
}

public sealed class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, Result<UploadFileResponse>>
{
    private readonly IFileRepository _fileRepository;
    private readonly IFileStorage _fileStorage;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UploadFileCommandHandler(
        IFileRepository fileRepository,
        IFileStorage fileStorage,
        IDateTimeProvider dateTimeProvider)
    {
        _fileRepository = fileRepository;
        _fileStorage = fileStorage;
        _dateTimeProvider = dateTimeProvider;
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

        var aggregateResult = TemporaryFile.Create(
            Guid.NewGuid(),
            request.FileName,
            request.ContentType,
            request.Size,
            storageObjectKeyResult.Value,
            accessToken,
            expirationPolicyResult.Value,
            now);

        if (aggregateResult.IsFailure)
        {
            return Result<UploadFileResponse>.Failure(aggregateResult.Errors);
        }

        await _fileStorage.UploadAsync(storageObjectKeyResult.Value, request.Content, request.ContentType, cancellationToken);
        await _fileRepository.AddAsync(aggregateResult.Value, cancellationToken);

        return Result<UploadFileResponse>.Success(new UploadFileResponse(
            aggregateResult.Value.Id,
            aggregateResult.Value.AccessToken,
            aggregateResult.Value.FileName,
            aggregateResult.Value.ExpiresAt,
            aggregateResult.Value.MaxDownloads));
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
