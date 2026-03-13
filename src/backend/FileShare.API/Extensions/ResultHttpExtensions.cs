using FileShare.Domain.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace FileShare.API.Extensions;

internal static class ResultHttpExtensions
{
    public static IActionResult ToActionResult(this ControllerBase controller, Result result)
    {
        var error = result.Errors[0];
        return controller.Problem(title: error.Code, detail: error.Message, statusCode: MapStatusCode(error.Type));
    }

    public static ActionResult<TContract> ToActionResult<TContract>(
        this ControllerBase controller,
        Result<TContract> result)
    {
        if (result.IsSuccess)
        {
            return controller.Ok(result.Value);
        }

        var error = result.Errors[0];
        return controller.Problem(title: error.Code, detail: error.Message, statusCode: MapStatusCode(error.Type));
    }

    private static int MapStatusCode(ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Gone => StatusCodes.Status410Gone,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}
