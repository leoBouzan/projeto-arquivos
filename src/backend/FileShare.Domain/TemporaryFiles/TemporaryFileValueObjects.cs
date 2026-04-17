using System.Security.Cryptography;
using FileShare.Domain.Abstractions;

namespace FileShare.Domain.TemporaryFiles;

public sealed record AccessToken
{
    public const int ExpectedLength = 32;

    private AccessToken(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static AccessToken Generate()
    {
        return new AccessToken(Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant());
    }

    public static Result<AccessToken> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<AccessToken>.Failure(TemporaryFileErrors.InvalidAccessToken);
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (!IsWellFormed(normalized))
        {
            return Result<AccessToken>.Failure(TemporaryFileErrors.InvalidAccessToken);
        }

        return Result<AccessToken>.Success(new AccessToken(normalized));
    }

    public static bool IsWellFormed(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != ExpectedLength)
        {
            return false;
        }

        foreach (var character in value)
        {
            var isHexLowercase = character is >= 'a' and <= 'f';
            var isHexDigit = character is >= '0' and <= '9';

            if (!isHexLowercase && !isHexDigit)
            {
                return false;
            }
        }

        return true;
    }

    public override string ToString()
    {
        return Value;
    }
}

public sealed record StorageObjectKey
{
    private StorageObjectKey(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<StorageObjectKey> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<StorageObjectKey>.Failure(TemporaryFileErrors.InvalidStorageKey);
        }

        return Result<StorageObjectKey>.Success(new StorageObjectKey(value.Trim()));
    }

    public override string ToString()
    {
        return Value;
    }
}

public sealed record TransferProof
{
    private TransferProof(string fileHash, long blockNumber, string blockHash, string signature, DateTimeOffset issuedAt)
    {
        FileHash = fileHash;
        BlockNumber = blockNumber;
        BlockHash = blockHash;
        Signature = signature;
        IssuedAt = issuedAt;
    }

    public string FileHash { get; }

    public long BlockNumber { get; }

    public string BlockHash { get; }

    public string Signature { get; }

    public DateTimeOffset IssuedAt { get; }

    public static Result<TransferProof> Create(string fileHash, DateTimeOffset issuedAt)
    {
        if (string.IsNullOrWhiteSpace(fileHash) || fileHash.Length != 64)
        {
            return Result<TransferProof>.Failure(TemporaryFileErrors.InvalidFileHash);
        }

        var normalized = fileHash.Trim().ToLowerInvariant();

        foreach (var c in normalized)
        {
            var isDigit = c is >= '0' and <= '9';
            var isHex = c is >= 'a' and <= 'f';
            if (!isDigit && !isHex)
            {
                return Result<TransferProof>.Failure(TemporaryFileErrors.InvalidFileHash);
            }
        }

        var blockNumber = 2_400_000L + RandomNumberGenerator.GetInt32(0, 99_999);
        var blockHash = RandomHex(64);
        var signature = RandomHex(128);

        return Result<TransferProof>.Success(new TransferProof(normalized, blockNumber, blockHash, signature, issuedAt));
    }

    private static string RandomHex(int length)
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(length / 2)).ToLowerInvariant();
    }
}

public sealed record ExpirationPolicy
{
    private ExpirationPolicy(DateTimeOffset expiresAt, int? maxDownloads)
    {
        ExpiresAt = expiresAt;
        MaxDownloads = maxDownloads;
    }

    public DateTimeOffset ExpiresAt { get; }

    public int? MaxDownloads { get; }

    public static Result<ExpirationPolicy> Create(DateTimeOffset expiresAt, int? maxDownloads, DateTimeOffset now)
    {
        if (expiresAt <= now)
        {
            return Result<ExpirationPolicy>.Failure(TemporaryFileErrors.InvalidExpirationPolicy);
        }

        if (maxDownloads.HasValue && maxDownloads.Value <= 0)
        {
            return Result<ExpirationPolicy>.Failure(TemporaryFileErrors.InvalidMaxDownloads);
        }

        return Result<ExpirationPolicy>.Success(new ExpirationPolicy(expiresAt, maxDownloads));
    }
}
