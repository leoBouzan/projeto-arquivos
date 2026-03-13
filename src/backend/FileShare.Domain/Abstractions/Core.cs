using System.Collections.ObjectModel;

namespace FileShare.Domain.Abstractions;

public enum ErrorType
{
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Forbidden = 4,
    Gone = 5,
    Unexpected = 6
}

public sealed record Error(string Code, string Message, ErrorType Type);

public interface IResult
{
    bool IsSuccess { get; }
    bool IsFailure { get; }
    IReadOnlyList<Error> Errors { get; }
}

public class Result : IResult
{
    protected Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        if (isSuccess && errors.Count > 0)
        {
            throw new InvalidOperationException("Successful results cannot contain errors.");
        }

        if (!isSuccess && errors.Count == 0)
        {
            throw new InvalidOperationException("Failure results must contain at least one error.");
        }

        IsSuccess = isSuccess;
        Errors = new ReadOnlyCollection<Error>(errors.ToArray());
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public IReadOnlyList<Error> Errors { get; }

    public static Result Success()
    {
        return new Result(true, Array.Empty<Error>());
    }

    public static Result Failure(params Error[] errors)
    {
        return new Result(false, errors);
    }

    public static Result Failure(IReadOnlyList<Error> errors)
    {
        return new Result(false, errors);
    }
}

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    private Result(bool isSuccess, TValue? value, IReadOnlyList<Error> errors)
        : base(isSuccess, errors)
    {
        _value = value;
    }

    public TValue Value =>
        IsSuccess ? _value! : throw new InvalidOperationException("A failed result does not have a value.");

    public static Result<TValue> Success(TValue value)
    {
        return new Result<TValue>(true, value, Array.Empty<Error>());
    }

    public new static Result<TValue> Failure(params Error[] errors)
    {
        return new Result<TValue>(false, default, errors);
    }

    public new static Result<TValue> Failure(IReadOnlyList<Error> errors)
    {
        return new Result<TValue>(false, default, errors);
    }
}

public interface IDomainEvent
{
    DateTimeOffset OccurredOnUtc { get; }
}

public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
