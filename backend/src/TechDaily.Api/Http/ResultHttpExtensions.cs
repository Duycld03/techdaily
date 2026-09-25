using Microsoft.AspNetCore.Http;
using TechDaily.Application.Common;

namespace TechDaily.Api.Http;

/// <summary>
/// Maps domain <see cref="Error"/> values to RFC 7807 problem-details HTTP responses,
/// preserving the domain error <c>code</c> in the problem extensions so clients keep
/// a stable, machine-readable error identifier.
/// </summary>
public static class ResultHttpExtensions
{
    /// <summary>
    /// Category-driven mapping from a domain <see cref="Error"/> to an HTTP status code.
    /// Endpoints that already choose an explicit status SHOULD pass it to
    /// <see cref="ToProblem(Error,int)"/> instead of relying on this map.
    /// </summary>
    public static int ToStatusCode(this Error error) => error.Code switch
    {
        "RESOURCE_NOT_FOUND" => StatusCodes.Status404NotFound,
        "UNAUTHORIZED" => StatusCodes.Status401Unauthorized,
        "AUTH_INVALID_CREDENTIALS" => StatusCodes.Status401Unauthorized,
        "FORBIDDEN" => StatusCodes.Status403Forbidden,
        "CONFLICT" => StatusCodes.Status409Conflict,
        "AUTH_EMAIL_EXISTS" => StatusCodes.Status409Conflict,
        "AUTH_OTP_RESEND_COOLDOWN" => StatusCodes.Status429TooManyRequests,
        _ => StatusCodes.Status400BadRequest,
    };

    /// <summary>
    /// Builds an RFC 7807 <c>application/problem+json</c> result for the given error and
    /// status code, carrying the domain error code under <c>extensions["code"]</c>.
    /// </summary>
    public static IResult ToProblem(this Error error, int statusCode)
    {
        return Results.Problem(
            detail: error.Message,
            statusCode: statusCode,
            title: TitleFor(statusCode),
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }

    /// <summary>
    /// Builds an RFC 7807 problem result using the category-driven status code from
    /// <see cref="ToStatusCode(Error)"/>.
    /// </summary>
    public static IResult ToProblem(this Error error) => error.ToProblem(error.ToStatusCode());

    private static string TitleFor(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status409Conflict => "Conflict",
        StatusCodes.Status429TooManyRequests => "Too Many Requests",
        _ => "Error",
    };
}
