namespace FileShare.Contracts.Files;

public sealed record UploadFileResponse(
    Guid Id,
    string AccessToken,
    string FileName,
    DateTimeOffset ExpiresAt,
    int? MaxDownloads,
    string MetadataUrl,
    string AvailabilityUrl,
    string DownloadUrl);

public sealed record FileMetadataResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long Size,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    int DownloadCount,
    int? MaxDownloads,
    string Status);

public sealed record FileAvailabilityResponse(
    bool Available,
    string Status,
    string? Reason,
    DateTimeOffset? ExpiresAt,
    int? DownloadCount,
    int? MaxDownloads);
