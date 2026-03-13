using System.Reflection;
using FileShare.Domain.Abstractions;

namespace FileShare.Application.Common;

internal static class ResultFactory
{
    public static TResponse CreateFailure<TResponse>(IReadOnlyList<Error> errors)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(errors);
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = responseType.GetGenericArguments()[0];
            var method = typeof(ResultFactory)
                .GetMethod(nameof(CreateGenericFailure), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(valueType);

            return (TResponse)method.Invoke(null, [errors])!;
        }

        throw new InvalidOperationException($"Unsupported response type '{responseType.Name}' for result pipeline handling.");
    }

    private static Result<TValue> CreateGenericFailure<TValue>(IReadOnlyList<Error> errors)
    {
        return Result<TValue>.Failure(errors);
    }
}
