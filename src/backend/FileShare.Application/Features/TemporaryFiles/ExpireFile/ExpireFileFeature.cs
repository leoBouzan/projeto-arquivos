using FileShare.Application.Abstractions.Persistence;
using FileShare.Application.Abstractions.Time;
using FileShare.Application.Common.CQRS;
using FileShare.Application.Common.Errors;
using FileShare.Domain.Abstractions;
using FileShare.Domain.TemporaryFiles;
using MediatR;

namespace FileShare.Application.Features.TemporaryFiles.ExpireFile;

public sealed record ExpireFileCommand(Guid Id, ExpirationReason Reason) : ICommand<Result>;

public sealed class ExpireFileCommandHandler : IRequestHandler<ExpireFileCommand, Result>
{
    private readonly IFileRepository _fileRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ExpireFileCommandHandler(IFileRepository fileRepository, IDateTimeProvider dateTimeProvider)
    {
        _fileRepository = fileRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ExpireFileCommand request, CancellationToken cancellationToken)
    {
        var temporaryFile = await _fileRepository.GetByIdAsync(request.Id, cancellationToken);
        if (temporaryFile is null)
        {
            return Result.Failure(ApplicationErrors.TemporaryFileNotFound(request.Id));
        }

        return temporaryFile.Expire(request.Reason, _dateTimeProvider.UtcNow);
    }
}
