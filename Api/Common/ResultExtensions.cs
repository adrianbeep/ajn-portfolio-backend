using Application.Abstractions.Common;
using Microsoft.AspNetCore.Http;

namespace Api.Common;

public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        return MapErrorToHttpResult(result.Error);
    }

    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok();
        }

        return MapErrorToHttpResult(result.Error);
    }

    private static IResult MapErrorToHttpResult(Error error)
    {
        int statusCode = error.Code switch
        {
            "Unauthorized" => StatusCodes.Status401Unauthorized,
            "Forbidden" => StatusCodes.Status403Forbidden,
            "Auth.InvalidCredentials" => StatusCodes.Status401Unauthorized,
            "Auth.BootstrapAlreadyUsed" => StatusCodes.Status409Conflict,
            "Auth.EmailAlreadyExists" => StatusCodes.Status409Conflict,

            string code when code.Contains("NotFound") => StatusCodes.Status404NotFound,
            string code when code.Contains("Validation") => StatusCodes.Status400BadRequest,

            _ => StatusCodes.Status400BadRequest
        };
        
        return Results.Problem(
            statusCode: statusCode,
            title: error.Code,
            detail: error.Message
        );
    }
}
