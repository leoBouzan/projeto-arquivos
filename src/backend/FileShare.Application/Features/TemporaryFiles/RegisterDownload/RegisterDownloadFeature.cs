using FileShare.Application.Abstractions.Persistence;
using FileShare.Application.Abstractions.Time;
using FileShare.Application.Common.CQRS;
using FileShare.Application.Common.Errors;
using FileShare.Domain.Abstractions;
using MediatR;

namespace FileShare.Application.Features.TemporaryFiles.RegisterDownload;

public sealed record RegisterDownloadCommand(string AccessToken) : ICommand<Result>;

public sealed class RegisterDownloadCommandHandler : IRequestHandler<RegisterDownloadCommand, Result>
{
    private readonly IFileRepository _fileRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RegisterDownloadCommandHandler(IFileRepository fileRepository, IDateTimeProvider dateTimeProvider)
    {
        _fileRepository = fileRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RegisterDownloadCommand request, CancellationToken cancellationToken)
    {
        var temporaryFile = await _fileRepository.GetByAccessTokenAsync(request.AccessToken, cancellationToken);
        if (temporaryFile is null)
        {
            return Result.Failure(ApplicationErrors.TemporaryFileNotFound(request.AccessToken));
        }

        return temporaryFile.RegisterDownload(_dateTimeProvider.UtcNow);
    }
}
