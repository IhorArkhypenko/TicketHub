using BuildingBlocks.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BuildingBlocks.Observability.Errors;

/// <summary>
/// Maps a failed <see cref="Result"/> to an RFC 7807 ProblemDetails ActionResult, giving a
/// single, consistent error shape for all controllers. Success is handled by the caller.
/// </summary>
public static class ApiResults
{
    public static ObjectResult Problem(Error error)
    {
        int status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status400BadRequest
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = error.Code,
            Detail = error.Message,
            Type = $"https://httpstatuses.io/{status}"
        };

        return new ObjectResult(problem) { StatusCode = status };
    }
}
