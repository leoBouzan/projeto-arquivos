using FileShare.Application.Abstractions.Messaging;

namespace FileShare.Infrastructure.Messaging;

public sealed class NoOpEventBus : IEventBus
{
    public Task PublishAsync(object message, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
