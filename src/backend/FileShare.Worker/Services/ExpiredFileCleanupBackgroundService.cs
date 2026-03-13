using FileShare.Application.Abstractions.Persistence;
using FileShare.Application.Abstractions.Storage;
using FileShare.Application.Abstractions.Time;
using FileShare.Application.Features.TemporaryFiles.ExpireFile;
using FileShare.Domain.TemporaryFiles;
using FileShare.Worker.Configuration;
using MediatR;
using Microsoft.Extensions.Options;

namespace FileShare.Worker.Services;

public sealed class ExpiredFileCleanupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ExpiredFileCleanupBackgroundService> _logger;
    private readonly CleanupOptions _options;

    public ExpiredFileCleanupBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<CleanupOptions> options,
        ILogger<ExpiredFileCleanupBackgroundService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.IntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed while processing expired file cleanup batch");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var fileRepository = scope.ServiceProvider.GetRequiredService<IFileRepository>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var now = dateTimeProvider.UtcNow;

        var expiredFiles = await fileRepository.GetExpiredBatchAsync(now, _options.BatchSize, cancellationToken);
        foreach (var expiredFile in expiredFiles)
        {
            await sender.Send(new ExpireFileCommand(expiredFile.Id, ExpirationReason.TimeElapsed), cancellationToken);
        }

        var cleanupCandidates = await fileRepository.GetPendingCleanupBatchAsync(_options.BatchSize, cancellationToken);

        foreach (var cleanupCandidate in cleanupCandidates)
        {
            var storageKeyResult = StorageObjectKey.Create(cleanupCandidate.StoragePath);
            if (storageKeyResult.IsFailure)
            {
                continue;
            }

            await fileStorage.DeleteAsync(storageKeyResult.Value, cancellationToken);
            cleanupCandidate.MarkStorageDeleted(now);
        }

        if (cleanupCandidates.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
