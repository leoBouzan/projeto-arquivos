using FileShare.Application.Abstractions.Persistence;
using FileShare.Application.Abstractions.Security;
using FileShare.Application.Abstractions.Time;
using FileShare.Application.Common.CQRS;
using FileShare.Application.Common.Errors;
using FileShare.Domain.Abstractions;
using FileShare.Domain.TemporaryFiles;
using MediatR;

namespace FileShare.Application.Features.TemporaryFiles.RegisterDownload;

public sealed record RegisterDownloadCommand(string AccessToken, string? Password = null) : ICommand<Result>;

public sealed class RegisterDownloadCommandHandler : IRequestHandler<RegisterDownloadCommand, Result>
{
    private readonly IFileRepository _fileRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterDownloadCommandHandler(
        IFileRepository fileRepository,
        IDateTimeProvider dateTimeProvider,
        IPasswordHasher passwordHasher)
    {
        _fileRepository = fileRepository;
        _dateTimeProvider = dateTimeProvider;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(RegisterDownloadCommand request, CancellationToken cancellationToken)
    {
        var temporaryFile = await _fileRepository.GetByAccessTokenAsync(request.AccessToken, cancellationToken);
        if (temporaryFile is null)
        {
            return Result.Failure(ApplicationErrors.TemporaryFileNotFound(request.AccessToken));
        }

        if (temporaryFile.HasPassword)
        {
            if (string.IsNullOrEmpty(request.Password))
            {
                return Result.Failure(TemporaryFileErrors.PasswordRequired);
            }

            if (!_passwordHasher.Verify(request.Password, temporaryFile.PasswordHash!))
            {
                return Result.Failure(TemporaryFileErrors.PasswordInvalid);
            }
        }

        return temporaryFile.RegisterDownload(_dateTimeProvider.UtcNow);
    }
}
