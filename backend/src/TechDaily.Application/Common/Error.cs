namespace TechDaily.Application.Common;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("NULL_VALUE", "The specified result value is null.");
    public static readonly Error NotFound = new("RESOURCE_NOT_FOUND", "The requested resource was not found.");
    public static readonly Error Unauthorized = new("UNAUTHORIZED", "User is unauthorized to perform this operation.");
    public static readonly Error Forbidden = new("FORBIDDEN", "User does not have permission for this resource.");
    public static readonly Error Validation = new("VALIDATION_FAILED", "Validation failed for the request.");
    public static readonly Error Conflict = new("CONFLICT", "A conflict occurred with existing state.");
    public static readonly Error ServerError = new("SERVER_ERROR", "An unexpected server error occurred.");

    // Authentication Domain
    public static readonly Error EmailPasswordRequired = new("AUTH_EMAIL_PASSWORD_REQUIRED", "Email and password are required.");
    public static readonly Error PasswordTooShort = new("AUTH_PASSWORD_TOO_SHORT", "Password must be at least 6 characters.");
    public static readonly Error EmailExists = new("AUTH_EMAIL_EXISTS", "An account with this email already exists.");
    public static readonly Error InvalidCredentials = new("AUTH_INVALID_CREDENTIALS", "Invalid email or password.");
    public static readonly Error GoogleTokenInvalid = new("AUTH_GOOGLE_TOKEN_INVALID", "Invalid Google authentication token.");
    public static readonly Error GoogleNotConfigured = new("AUTH_GOOGLE_NOT_CONFIGURED", "Google Client ID is not configured.");

    // One-Time Passcode (OTP) Domain
    public static readonly Error OtpInvalid = new("AUTH_OTP_INVALID", "The verification code is invalid.");
    public static readonly Error OtpExpired = new("AUTH_OTP_EXPIRED", "The verification code has expired.");
    public static readonly Error OtpMaxAttempts = new("AUTH_OTP_MAX_ATTEMPTS", "Too many incorrect attempts. Request a new verification code.");
    public static readonly Error OtpResendCooldown = new("AUTH_OTP_RESEND_COOLDOWN", "Please wait before requesting another verification code.");

    // User Profile Domain
    public static readonly Error CurrentPasswordIncorrect = new("USER_CURRENT_PASSWORD_INCORRECT", "Current password is incorrect.");
    public static readonly Error NewPasswordTooShort = new("USER_NEW_PASSWORD_TOO_SHORT", "New password must be at least 6 characters.");

    // Library Domain
    public static readonly Error PdfRequired = new("LIBRARY_PDF_REQUIRED", "A valid PDF file is required.");
    public static readonly Error MultipartRequired = new("LIBRARY_MULTIPART_REQUIRED", "Multipart form data is required.");

    public static Error Custom(string code, string message) => new(code, message);
}
