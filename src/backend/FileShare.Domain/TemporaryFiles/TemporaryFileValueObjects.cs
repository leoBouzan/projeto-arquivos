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
