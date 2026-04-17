using System.ComponentModel.DataAnnotations;

namespace FileShare.API.Models;

public sealed class UploadFileForm
{
    [Required]
    public required IFormFile File { get; init; }

    [Required]
    public DateTimeOffset ExpiresAt { get; init; }

    public int? MaxDownloads { get; init; }

    [StringLength(128, MinimumLength = 4)]
    public string? Password { get; init; }
}
