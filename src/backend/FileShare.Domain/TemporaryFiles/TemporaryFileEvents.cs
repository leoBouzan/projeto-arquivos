using FileShare.Domain.Abstractions;

namespace FileShare.Domain.TemporaryFiles;

public sealed record TemporaryFileUploadedDomainEvent(Guid TemporaryFileId, string AccessToken, DateTimeOffset OccurredOnUtc) : IDomainEvent;

public sealed record TemporaryFileDownloadRegisteredDomainEvent(Guid TemporaryFileId, int DownloadCount, DateTimeOffset OccurredOnUtc) : IDomainEvent;

public sealed record TemporaryFileExpiredDomainEvent(Guid TemporaryFileId, ExpirationReason Reason, DateTimeOffset OccurredOnUtc) : IDomainEvent;

public sealed record TemporaryFileDeletedDomainEvent(Guid TemporaryFileId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
