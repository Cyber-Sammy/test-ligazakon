using Microsoft.AspNetCore.Mvc;
using UserService.Application.Common;
using UserService.Application.Common.Results;

namespace UserService.Api.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<TValue>(
        this Result<TValue> result,
        ControllerBase controller,
        Func<TValue, IActionResult>? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            return onSuccess is null
                ? controller.Ok(result.Value)
                : onSuccess(result.Value);
        }

        return ToProblemResult(result, controller);
    }

    public static IActionResult ToActionResult(
        this Result result,
        ControllerBase controller,
        Func<IActionResult>? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            return onSuccess is null
                ? controller.NoContent()
                : onSuccess();
        }

        return ToProblemResult(result, controller);
    }

    private static IActionResult ToProblemResult(Result result, ControllerBase controller)
    {
        var statusCode = result.Status switch
        {
            ResultStatus.ValidationError => StatusCodes.Status400BadRequest,
            ResultStatus.Unauthorized => StatusCodes.Status401Unauthorized,
            ResultStatus.Forbidden => StatusCodes.Status403Forbidden,
            ResultStatus.NotFound => StatusCodes.Status404NotFound,
            ResultStatus.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(result.Status),
            Detail = result.Message
        };

        return controller.StatusCode(statusCode, problemDetails);
    }

    private static string GetTitle(ResultStatus status) => status switch
    {
        ResultStatus.ValidationError => Constants.ProblemDetails.ValidationErrorTitle,
        ResultStatus.Unauthorized => Constants.ProblemDetails.UnauthorizedTitle,
        ResultStatus.Forbidden => Constants.ProblemDetails.ForbiddenTitle,
        ResultStatus.NotFound => Constants.ProblemDetails.NotFoundTitle,
        ResultStatus.Conflict => Constants.ProblemDetails.ConflictTitle,
        _ => Constants.ProblemDetails.UnexpectedErrorTitle
    };
}
