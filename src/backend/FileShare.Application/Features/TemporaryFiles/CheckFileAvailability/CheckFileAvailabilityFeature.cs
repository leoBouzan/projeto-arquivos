using FileShare.Application.Abstractions.Persistence;
using FileShare.Application.Abstractions.Time;
using FileShare.Application.Common.CQRS;
using FileShare.Domain.Abstractions;
using MediatR;

namespace FileShare.Application.Features.TemporaryFiles.CheckFileAvailability;

public sealed record CheckFileAvailabilityQuery(string AccessToken) : IQuery<Result<FileAvailabilityDto>>;

public sealed record FileAvailabilityDto(
    bool Available,
    string Status,
    string? Reason,
    DateTimeOffset? ExpiresAt,
    int? DownloadCount,
    int? MaxDownloads,
    bool HasPassword);

public sealed class CheckFileAvailabilityQueryHandler : IRequestHandler<CheckFileAvailabilityQuery, Result<FileAvailabilityDto>>
{
    private readonly IFileRepository _fileRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CheckFileAvailabilityQueryHandler(IFileRepository fileRepository, IDateTimeProvider dateTimeProvider)
    {
        _fileRepository = fileRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<FileAvailabilityDto>> Handle(CheckFileAvailabilityQuery request, CancellationToken cancellationToken)
    {
        var temporaryFile = await _fileRepository.GetByAccessTokenAsync(request.AccessToken, cancellationToken);
        if (temporaryFile is null)
        {
            return Result<FileAvailabilityDto>.Failure(Common.Errors.ApplicationErrors.TemporaryFileNotFound(request.AccessToken));
        }

        var now = _dateTimeProvider.UtcNow;
        var availability = temporaryFile.CanBeDownloaded(now);

        if (availability.IsSuccess)
        {
            return Result<FileAvailabilityDto>.Success(new FileAvailabilityDto(
                true,
                temporaryFile.Status.ToString(),
                null,
                temporaryFile.ExpiresAt,
                temporaryFile.DownloadCount,
                temporaryFile.MaxDownloads,
                temporaryFile.HasPassword));
        }

        var firstError = availability.Errors[0];
        var effectiveStatus = firstError.Code switch
        {
            "temporary_file.expired" => "Expired",
            "temporary_file.download_limit_reached" => "Expired",
            "temporary_file.deleted" => "Deleted",
            _ => temporaryFile.Status.ToString()
        };

        return Result<FileAvailabilityDto>.Success(new FileAvailabilityDto(
            false,
            effectiveStatus,
            firstError.Message,
            temporaryFile.ExpiresAt,
            temporaryFile.DownloadCount,
            temporaryFile.MaxDownloads,
            temporaryFile.HasPassword));
    }
}
