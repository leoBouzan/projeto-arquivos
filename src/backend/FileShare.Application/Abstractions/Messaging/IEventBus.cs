namespace FileShare.Application.Abstractions.Messaging;

public interface IEventBus
{
    Task PublishAsync(object message, CancellationToken cancellationToken);
}
