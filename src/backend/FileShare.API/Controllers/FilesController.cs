using FileShare.API.Extensions;
using FileShare.API.Filters;
using FileShare.API.Models;
using FileShare.Application.Features.TemporaryFiles.CheckFileAvailability;
using FileShare.Application.Features.TemporaryFiles.DeleteFile;
using FileShare.Application.Features.TemporaryFiles.DownloadFile;
using FileShare.Application.Features.TemporaryFiles.GetFileMetadata;
using FileShare.Application.Features.TemporaryFiles.RegisterDownload;
using FileShare.Application.Features.TemporaryFiles.UploadFile;
using FileShare.Contracts.Files;
using FileShare.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UploadFileResponseContract = FileShare.Contracts.Files.UploadFileResponse;

namespace FileShare.API.Controllers;

[ApiController]
[Route("api/files")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class FilesController : ControllerBase
{
    private readonly ISender _sender;

    public FilesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [ProducesResponseType(typeof(UploadFileResponseContract), StatusCodes.Status201Created)]
    public async Task<IActionResult> Upload([FromForm] UploadFileForm request, CancellationToken cancellationToken)
    {
        await using var stream = request.File.OpenReadStream();

        var result = await _sender.Send(new UploadFileCommand(
            request.File.FileName,
            request.File.ContentType,
            request.File.Length,
            request.ExpiresAt,
            request.MaxDownloads,
            stream), cancellationToken);

        if (result.IsFailure)
        {
            return this.ToActionResult((Result)result);
        }

        var accessToken = result.Value.AccessToken;
        var response = new UploadFileResponseContract(
            result.Value.Id,
            accessToken,
            result.Value.FileName,
            result.Value.ExpiresAt,
            result.Value.MaxDownloads,
            Url.RouteUrl(nameof(GetMetadata), new { accessToken }) ?? string.Empty,
            Url.RouteUrl(nameof(CheckAvailability), new { accessToken }) ?? string.Empty,
            Url.RouteUrl(nameof(Download), new { accessToken }) ?? string.Empty);

        return CreatedAtRoute(nameof(GetMetadata), new { accessToken }, response);
    }

    [HttpGet("{accessToken}/metadata", Name = nameof(GetMetadata))]
    [ValidatePublicAccessToken]
    [ProducesResponseType(typeof(FileMetadataResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetadata(string accessToken, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetFileMetadataQuery(accessToken), cancellationToken);
        if (result.IsFailure)
        {
            return this.ToActionResult((Result)result);
        }

        return Ok(new FileMetadataResponse(
            result.Value.Id,
            result.Value.FileName,
            result.Value.ContentType,
            result.Value.Size,
            result.Value.CreatedAt,
            result.Value.ExpiresAt,
            result.Value.DownloadCount,
            result.Value.MaxDownloads,
            result.Value.Status));
    }

    [HttpGet("{accessToken}/availability", Name = nameof(CheckAvailability))]
    [ValidatePublicAccessToken]
    [ProducesResponseType(typeof(FileAvailabilityResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckAvailability(string accessToken, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CheckFileAvailabilityQuery(accessToken), cancellationToken);
        if (result.IsFailure)
        {
            return this.ToActionResult((Result)result);
        }

        return Ok(new FileAvailabilityResponse(
            result.Value.Available,
            result.Value.Status,
            result.Value.Reason,
            result.Value.ExpiresAt,
            result.Value.DownloadCount,
            result.Value.MaxDownloads));
    }

    [HttpGet("{accessToken}/download", Name = nameof(Download))]
    [ValidatePublicAccessToken]
    public async Task<IActionResult> Download(string accessToken, CancellationToken cancellationToken)
    {
        var registerResult = await _sender.Send(new RegisterDownloadCommand(accessToken), cancellationToken);
        if (registerResult.IsFailure)
        {
            return this.ToActionResult(registerResult);
        }

        var downloadResult = await _sender.Send(new DownloadFileQuery(accessToken), cancellationToken);
        if (downloadResult.IsFailure)
        {
            return this.ToActionResult((Result)downloadResult);
        }

        return File(
            downloadResult.Value.Content,
            downloadResult.Value.ContentType,
            downloadResult.Value.FileName,
            enableRangeProcessing: true);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromHeader(Name = "X-File-Access-Token")] string? accessToken,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteFileCommand(id, accessToken ?? string.Empty), cancellationToken);
        if (result.IsFailure)
        {
            return this.ToActionResult(result);
        }

        return NoContent();
    }
}
