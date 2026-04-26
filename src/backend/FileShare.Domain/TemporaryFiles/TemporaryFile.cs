using FileShare.Domain.Abstractions;

namespace FileShare.Domain.TemporaryFiles;

public enum TemporaryFileStatus
{
    Available = 1,
    Expired = 2,
    Deleted = 3
}

public enum ExpirationReason
{
    TimeElapsed = 1,
    MaxDownloadsReached = 2,
    ManuallyExpired = 3,
    Deleted = 4
}

public static class TemporaryFileErrors
{
    public static readonly Error InvalidFileName = new("temporary_file.invalid_file_name", "The file name is required.", ErrorType.Validation);
    public static readonly Error InvalidContentType = new("temporary_file.invalid_content_type", "The content type is required.", ErrorType.Validation);
    public static readonly Error InvalidFileSize = new("temporary_file.invalid_file_size", "The file size must be greater than zero.", ErrorType.Validation);
    public static readonly Error InvalidExpirationPolicy = new("temporary_file.invalid_expiration_policy", "The expiration policy is invalid.", ErrorType.Validation);
    public static readonly Error InvalidMaxDownloads = new("temporary_file.invalid_max_downloads", "The max downloads must be greater than zero.", ErrorType.Validation);
    public static readonly Error InvalidStorageKey = new("temporary_file.invalid_storage_key", "The storage key is invalid.", ErrorType.Validation);
    public static readonly Error InvalidAccessToken = new("temporary_file.invalid_access_token", "The access token is invalid.", ErrorType.Validation);
    public static readonly Error Expired = new("temporary_file.expired", "The temporary file has expired.", ErrorType.Gone);
    public static readonly Error Deleted = new("temporary_file.deleted", "The temporary file has been deleted.", ErrorType.Gone);
    public static readonly Error DownloadLimitReached = new("temporary_file.download_limit_reached", "The download limit has been reached.", ErrorType.Gone);
    public static readonly Error PasswordRequired = new("temporary_file.password_required", "This file is password protected.", ErrorType.Validation);
    public static readonly Error PasswordInvalid = new("temporary_file.password_invalid", "The provided password is incorrect.", ErrorType.Validation);
    public static readonly Error InvalidFileHash = new("temporary_file.invalid_file_hash", "The file hash is invalid.", ErrorType.Validation);
    public static readonly Error MalwareDetected = new("temporary_file.malware_detected", "The uploaded file was flagged as malicious by the antivirus scan.", ErrorType.Validation);
}

public sealed class TemporaryFile : AggregateRoot
{
    private TemporaryFile()
    {
    }

    private TemporaryFile(
        Guid id,
        string fileName,
        string storagePath,
        string contentType,
        long size,
        string accessToken,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        int? maxDownloads,
        string? passwordHash,
        TransferProof proof)
    {
        Id = id;
        FileName = fileName;
        StoragePath = storagePath;
        ContentType = contentType;
        Size = size;
        AccessToken = accessToken;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        MaxDownloads = maxDownloads;
        PasswordHash = passwordHash;
        FileHash = proof.FileHash;
        BlockNumber = proof.BlockNumber;
        BlockHash = proof.BlockHash;
        Signature = proof.Signature;
        ProofIssuedAt = proof.IssuedAt;
        Status = TemporaryFileStatus.Available;
        Raise(new TemporaryFileUploadedDomainEvent(Id, AccessToken, createdAt));
    }

    public Guid Id { get; private set; }

    public string FileName { get; private set; } = string.Empty;

    public string StoragePath { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long Size { get; private set; }

    public string AccessToken { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public int DownloadCount { get; private set; }

    public int? MaxDownloads { get; private set; }

    public TemporaryFileStatus Status { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public DateTimeOffset? ExpiredAt { get; private set; }

    public DateTimeOffset? LastDownloadedAt { get; private set; }

    public DateTimeOffset? StorageDeletedAt { get; private set; }

    public long RowVersion { get; private set; }

    public string? PasswordHash { get; private set; }

    public string FileHash { get; private set; } = string.Empty;

    public long BlockNumber { get; private set; }

    public string BlockHash { get; private set; } = string.Empty;

    public string Signature { get; private set; } = string.Empty;

    public DateTimeOffset ProofIssuedAt { get; private set; }

    public MalwareScanStatus ScanStatus { get; private set; } = MalwareScanStatus.Unscanned;

    public int? ScanMaliciousCount { get; private set; }

    public int? ScanSuspiciousCount { get; private set; }

    public int? ScanTotalEngines { get; private set; }

    public DateTimeOffset? ScannedAt { get; private set; }

    public string? ScanPermalink { get; private set; }

    public bool ScanIsEmulated { get; private set; }

    public bool HasPassword => !string.IsNullOrEmpty(PasswordHash);

    public bool RequiresStorageCleanup => StorageDeletedAt is null && (Status == TemporaryFileStatus.Expired || Status == TemporaryFileStatus.Deleted);

    public static Result<TemporaryFile> Create(
        Guid id,
        string fileName,
        string contentType,
        long size,
        StorageObjectKey storageObjectKey,
        AccessToken accessToken,
        ExpirationPolicy expirationPolicy,
        TransferProof proof,
        DateTimeOffset createdAt,
        string? passwordHash = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Result<TemporaryFile>.Failure(TemporaryFileErrors.InvalidFileName);
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            return Result<TemporaryFile>.Failure(TemporaryFileErrors.InvalidContentType);
        }

        if (size <= 0)
        {
            return Result<TemporaryFile>.Failure(TemporaryFileErrors.InvalidFileSize);
        }

        var temporaryFile = new TemporaryFile(
            id,
            fileName.Trim(),
            storageObjectKey.Value,
            contentType.Trim(),
            size,
            accessToken.Value,
            createdAt,
            expirationPolicy.ExpiresAt,
            expirationPolicy.MaxDownloads,
            passwordHash,
            proof);

        return Result<TemporaryFile>.Success(temporaryFile);
    }

    public bool IsExpired(DateTimeOffset now)
    {
        return Status == TemporaryFileStatus.Expired || ExpiresAt <= now;
    }

    public Result CanBeDownloaded(DateTimeOffset now)
    {
        if (Status == TemporaryFileStatus.Deleted || DeletedAt.HasValue)
        {
            return Result.Failure(TemporaryFileErrors.Deleted);
        }

        if (IsExpired(now))
        {
            return Result.Failure(TemporaryFileErrors.Expired);
        }

        if (MaxDownloads.HasValue && DownloadCount >= MaxDownloads.Value)
        {
            return Result.Failure(TemporaryFileErrors.DownloadLimitReached);
        }

        return Result.Success();
    }

    public Result RegisterDownload(DateTimeOffset now)
    {
        var availability = CanBeDownloaded(now);
        if (availability.IsFailure)
        {
            return availability;
        }

        DownloadCount++;
        LastDownloadedAt = now;
        Raise(new TemporaryFileDownloadRegisteredDomainEvent(Id, DownloadCount, now));

        if (MaxDownloads.HasValue && DownloadCount >= MaxDownloads.Value)
        {
            return Expire(ExpirationReason.MaxDownloadsReached, now);
        }

        return Result.Success();
    }

    public Result Expire(ExpirationReason reason, DateTimeOffset now)
    {
        if (Status == TemporaryFileStatus.Deleted)
        {
            return Result.Success();
        }

        if (Status == TemporaryFileStatus.Expired)
        {
            return Result.Success();
        }

        Status = TemporaryFileStatus.Expired;
        ExpiredAt ??= now;
        Raise(new TemporaryFileExpiredDomainEvent(Id, reason, now));
        return Result.Success();
    }

    public Result Delete(DateTimeOffset now)
    {
        if (Status == TemporaryFileStatus.Deleted)
        {
            return Result.Success();
        }

        Status = TemporaryFileStatus.Deleted;
        DeletedAt ??= now;
        Raise(new TemporaryFileDeletedDomainEvent(Id, now));
        return Result.Success();
    }

    public void MarkStorageDeleted(DateTimeOffset now)
    {
        StorageDeletedAt ??= now;
    }

    public void ApplyMalwareScanResult(
        MalwareScanStatus status,
        int maliciousCount,
        int suspiciousCount,
        int totalEngines,
        DateTimeOffset scannedAt,
        string? permalink,
        bool isEmulated)
    {
        ScanStatus = status;
        ScanMaliciousCount = maliciousCount;
        ScanSuspiciousCount = suspiciousCount;
        ScanTotalEngines = totalEngines;
        ScannedAt = scannedAt;
        ScanPermalink = permalink;
        ScanIsEmulated = isEmulated;
    }
}
