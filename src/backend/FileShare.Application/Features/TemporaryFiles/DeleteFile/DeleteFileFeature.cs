using FileShare.Application.Abstractions.Persistence;
using FileShare.Application.Abstractions.Time;
using FileShare.Application.Common.CQRS;
using FileShare.Application.Common.Errors;
using FileShare.Domain.Abstractions;
using FileShare.Domain.TemporaryFiles;
using MediatR;

namespace FileShare.Application.Features.TemporaryFiles.DeleteFile;

public sealed record DeleteFileCommand(Guid Id, string AccessToken) : ICommand<Result>;

public sealed class DeleteFileCommandHandler : IRequestHandler<DeleteFileCommand, Result>
{
    private readonly IFileRepository _fileRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteFileCommandHandler(IFileRepository fileRepository, IDateTimeProvider dateTimeProvider)
    {
        _fileRepository = fileRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteFileCommand request, CancellationToken cancellationToken)
    {
        var accessTokenResult = AccessToken.Create(request.AccessToken);
        if (accessTokenResult.IsFailure)
        {
            return Result.Failure(ApplicationErrors.TemporaryFileNotFound(request.AccessToken));
        }

        var temporaryFile = await _fileRepository.GetByIdAsync(request.Id, cancellationToken);
        if (temporaryFile is null)
        {
            return Result.Failure(ApplicationErrors.TemporaryFileNotFound(request.AccessToken));
        }

        if (!string.Equals(temporaryFile.AccessToken, accessTokenResult.Value.Value, StringComparison.Ordinal))
        {
            return Result.Failure(ApplicationErrors.TemporaryFileNotFound(request.AccessToken));
        }

        return temporaryFile.Delete(_dateTimeProvider.UtcNow);
    }
}
