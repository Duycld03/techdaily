namespace TechDaily.Application.Common;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "The specified result value is null.");
    public static readonly Error NotFound = new("Error.NotFound", "The requested resource was not found.");
    public static readonly Error Unauthorized = new("Error.Unauthorized", "User is unauthorized to perform this operation.");
    public static readonly Error Validation = new("Error.Validation", "Validation failed for the request.");
    public static readonly Error Conflict = new("Error.Conflict", "A conflict occurred with existing state.");

    public static Error Custom(string code, string message) => new(code, message);
}
