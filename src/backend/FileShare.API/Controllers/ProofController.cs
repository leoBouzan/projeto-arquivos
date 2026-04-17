using FileShare.API.Extensions;
using FileShare.Application.Features.TemporaryFiles.VerifyProof;
using FileShare.Contracts.Files;
using FileShare.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FileShare.API.Controllers;

[ApiController]
[Route("api/proof")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProofController : ControllerBase
{
    private readonly ISender _sender;

    public ProofController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{hashPrefix}")]
    [ProducesResponseType(typeof(VerifyProofResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Verify(string hashPrefix, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new VerifyProofQuery(hashPrefix), cancellationToken);
        if (result.IsFailure)
        {
            return this.ToActionResult((Result)result);
        }

        var value = result.Value;
        return Ok(new VerifyProofResponse(
            value.Verified,
            value.FileName,
            value.Size,
            value.FileHash,
            value.BlockNumber,
            value.BlockHash,
            value.Signature,
            value.IssuedAt,
            value.ExpiresAt,
            value.Status,
            value.DownloadCount,
            value.MaxDownloads));
    }
}
