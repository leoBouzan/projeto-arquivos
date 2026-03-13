using MediatR;

namespace FileShare.Application.Common.CQRS;

public interface ICommandBase
{
}

public interface ICommand<out TResponse> : IRequest<TResponse>, ICommandBase
{
}

public interface IQuery<out TResponse> : IRequest<TResponse>
{
}
