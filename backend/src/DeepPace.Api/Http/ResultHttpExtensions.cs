using Microsoft.AspNetCore.Http;
using DeepPace.Application.Common;

namespace DeepPace.Api.Http;

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
    public static int ToStatusCode(this Error error)
    {
        var code = error.Code;
        if (code == "RESOURCE_NOT_FOUND" || code.EndsWith("NotFound", StringComparison.OrdinalIgnoreCase) || code == "Catalog.NotAvailable")
        {
            return StatusCodes.Status404NotFound;
        }
        if (code == "UNAUTHORIZED" || code == "AUTH_INVALID_CREDENTIALS")
        {
            return StatusCodes.Status401Unauthorized;
        }
        if (code == "FORBIDDEN" || code == "AUTH_FORBIDDEN" || code == "AUTH_ACCOUNT_SUSPENDED")
        {
            return StatusCodes.Status403Forbidden;
        }
        if (code == "CONFLICT" || code == "AUTH_EMAIL_EXISTS"
            || code.Contains("Conflict", StringComparison.OrdinalIgnoreCase)
            || code.Contains("Ineligible", StringComparison.OrdinalIgnoreCase)
            || code.Contains("Stale", StringComparison.OrdinalIgnoreCase)
            || code.Contains("Busy", StringComparison.OrdinalIgnoreCase)
            || code.Contains("PublicationFailed", StringComparison.OrdinalIgnoreCase)
            || code.Contains("Protected", StringComparison.OrdinalIgnoreCase)
            || code.Contains("Blocked", StringComparison.OrdinalIgnoreCase)
            || code.Contains("InvalidTransition", StringComparison.OrdinalIgnoreCase)
            || code.Contains("Unchanged", StringComparison.OrdinalIgnoreCase)
            || code.Contains("PublishedImmutable", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCodes.Status409Conflict;
        }
        if (code == "AUTH_OTP_RESEND_COOLDOWN" || code == "AudioQuotaExhausted")
        {
            return StatusCodes.Status429TooManyRequests;
        }
        return StatusCodes.Status400BadRequest;
    }

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
