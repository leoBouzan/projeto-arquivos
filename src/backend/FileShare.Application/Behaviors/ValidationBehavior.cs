using FileShare.Application.Common;
using FluentValidation;
using MediatR;

namespace FileShare.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(_validators.Select(x => x.ValidateAsync(context, cancellationToken)));
        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(error => error is not null)
            .Select(error => new Domain.Abstractions.Error(
                $"validation.{error.PropertyName.ToLowerInvariant()}",
                error.ErrorMessage,
                Domain.Abstractions.ErrorType.Validation))
            .ToArray();

        if (failures.Length == 0)
        {
            return await next();
        }

        return ResultFactory.CreateFailure<TResponse>(failures);
    }
}
