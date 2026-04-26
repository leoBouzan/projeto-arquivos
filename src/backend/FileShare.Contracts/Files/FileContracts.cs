namespace FileShare.Contracts.Files;

public sealed record TransferProofContract(
    string FileHash,
    long BlockNumber,
    string BlockHash,
    string Signature,
    DateTimeOffset IssuedAt);

public sealed record MalwareScanContract(
    string Status,
    int? MaliciousCount,
    int? SuspiciousCount,
    int? TotalEngines,
    DateTimeOffset? ScannedAt,
    string? Permalink,
    bool IsEmulated);

public sealed record UploadFileResponse(
    Guid Id,
    string AccessToken,
    string FileName,
    DateTimeOffset ExpiresAt,
    int? MaxDownloads,
    bool HasPassword,
    TransferProofContract Proof,
    MalwareScanContract Scan,
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
    string Status,
    bool HasPassword,
    TransferProofContract Proof,
    MalwareScanContract Scan);

public sealed record FileAvailabilityResponse(
    bool Available,
    string Status,
    string? Reason,
    DateTimeOffset? ExpiresAt,
    int? DownloadCount,
    int? MaxDownloads,
    bool HasPassword);

public sealed record VerifyProofResponse(
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
