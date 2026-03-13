using FileShare.Domain.Abstractions;

namespace FileShare.Application.Common.Errors;

public static class ApplicationErrors
{
    public static readonly Error StorageObjectMissing = new("storage.object_not_found", "The file content could not be found in storage.", ErrorType.NotFound);
    public static readonly Error StorageUploadFailed = new("storage.upload_failed", "The file content could not be uploaded to storage.", ErrorType.Unexpected);
    public static readonly Error StorageDeleteFailed = new("storage.delete_failed", "The file content could not be deleted from storage.", ErrorType.Unexpected);

    public static Error TemporaryFileNotFound(Guid id)
    {
        return new Error("temporary_file.not_found", $"Temporary file '{id}' was not found.", ErrorType.NotFound);
    }

    public static Error TemporaryFileNotFound(string accessToken)
    {
        return new Error("temporary_file.not_found", "The requested file link is invalid or unavailable.", ErrorType.NotFound);
    }
}
