using BuildingBlocks.Abstractions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Orders.Api.Endpoints;

/// <summary>Translates application <see cref="Error"/> values into RFC 9457 problem details.</summary>
internal static class ErrorResults
{
    public static ProblemHttpResult ToProblem(this Error error)
    {
        var (statusCode, title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Validation failed"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Resource not found"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected failure"),
        };

        return TypedResults.Problem(
            detail: error.Description,
            statusCode: statusCode,
            title: title,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
