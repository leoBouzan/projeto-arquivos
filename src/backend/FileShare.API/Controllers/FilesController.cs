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

internal static class FilesControllerMappers
{
    public static TransferProofContract ToContract(this TransferProofDto dto) =>
        new(dto.FileHash, dto.BlockNumber, dto.BlockHash, dto.Signature, dto.IssuedAt);

    public static MalwareScanContract ToContract(this MalwareScanDto dto) =>
        new(dto.Status, dto.MaliciousCount, dto.SuspiciousCount, dto.TotalEngines, dto.ScannedAt, dto.Permalink, dto.IsEmulated);
}

/// <summary>
/// Endpoints de upload, consulta e download de arquivos temporarios.
/// </summary>
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

    /// <summary>
    /// Realiza o upload de um arquivo e gera um link temporario com prova de transferencia e resultado de escaneamento antivirus.
    /// </summary>
    /// <remarks>
    /// O arquivo e enviado como multipart/form-data. O servidor calcula o SHA-256, consulta o VirusTotal pelo hash
    /// (ou usa o scanner emulado quando a chave nao esta configurada) e, se o veredito for malicioso, recusa o upload.
    /// </remarks>
    /// <returns>Link publico, prova de transferencia e resultado do antivirus.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UploadFileResponseContract), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> Upload([FromForm] UploadFileForm request, CancellationToken cancellationToken)
    {
        await using var stream = request.File.OpenReadStream();

        var result = await _sender.Send(new UploadFileCommand(
            request.File.FileName,
            request.File.ContentType,
            request.File.Length,
            request.ExpiresAt,
            request.MaxDownloads,
            stream,
            request.Password), cancellationToken);

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
            result.Value.HasPassword,
            result.Value.Proof.ToContract(),
            result.Value.Scan.ToContract(),
            Url.RouteUrl(nameof(GetMetadata), new { accessToken }) ?? string.Empty,
            Url.RouteUrl(nameof(CheckAvailability), new { accessToken }) ?? string.Empty,
            Url.RouteUrl(nameof(Download), new { accessToken }) ?? string.Empty);

        return CreatedAtRoute(nameof(GetMetadata), new { accessToken }, response);
    }

    /// <summary>
    /// Retorna os metadados publicos de um arquivo (nome, tamanho, expiracao, contagem de downloads, scan antivirus, prova).
    /// </summary>
    /// <param name="accessToken">Token publico de 32 caracteres hexadecimais retornado no upload.</param>
    [HttpGet("{accessToken}/metadata", Name = nameof(GetMetadata))]
    [ValidatePublicAccessToken]
    [ProducesResponseType(typeof(FileMetadataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
            result.Value.Status,
            result.Value.HasPassword,
            result.Value.Proof.ToContract(),
            result.Value.Scan.ToContract()));
    }

    /// <summary>
    /// Verifica se o arquivo ainda esta disponivel para download (nao expirou e nao excedeu o limite).
    /// </summary>
    /// <param name="accessToken">Token publico de 32 caracteres hexadecimais retornado no upload.</param>
    [HttpGet("{accessToken}/availability", Name = nameof(CheckAvailability))]
    [ValidatePublicAccessToken]
    [ProducesResponseType(typeof(FileAvailabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
            result.Value.MaxDownloads,
            result.Value.HasPassword));
    }

    /// <summary>
    /// Faz o download do arquivo. Incrementa o contador de downloads e respeita expiracao por tempo e por limite.
    /// </summary>
    /// <param name="accessToken">Token publico de 32 caracteres hexadecimais retornado no upload.</param>
    /// <param name="password">Senha (opcional) via header X-File-Password.</param>
    /// <param name="passwordQuery">Senha (opcional) via query string ?password=.</param>
    [HttpGet("{accessToken}/download", Name = nameof(Download))]
    [ValidatePublicAccessToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> Download(
        string accessToken,
        [FromHeader(Name = "X-File-Password")] string? password,
        [FromQuery(Name = "password")] string? passwordQuery,
        CancellationToken cancellationToken)
    {
        var effectivePassword = password ?? passwordQuery;
        var registerResult = await _sender.Send(new RegisterDownloadCommand(accessToken, effectivePassword), cancellationToken);
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

    /// <summary>
    /// Exclui logicamente um arquivo. Requer o token original no header X-File-Access-Token.
    /// </summary>
    /// <param name="id">Identificador interno do arquivo (Guid).</param>
    /// <param name="accessToken">Token original retornado no upload (header X-File-Access-Token).</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
