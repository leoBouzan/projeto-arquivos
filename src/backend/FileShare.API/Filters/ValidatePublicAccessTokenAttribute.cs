using FileShare.Domain.TemporaryFiles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FileShare.API.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ValidatePublicAccessTokenAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ActionArguments.TryGetValue("accessToken", out var rawValue) ||
            rawValue is not string accessToken)
        {
            context.Result = new NotFoundResult();
            return;
        }

        var normalized = accessToken.Trim().ToLowerInvariant();
        if (!AccessToken.IsWellFormed(normalized))
        {
            context.Result = new NotFoundResult();
            return;
        }

        context.ActionArguments["accessToken"] = normalized;
        context.RouteData.Values["accessToken"] = normalized;
    }
}
