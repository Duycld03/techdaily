namespace TechDaily.Api.Contracts;

/// <summary>
/// Generic single-message response body (e.g. <c>PUT /api/v1/user/change-password</c>).
/// </summary>
public record MessageResponse(string Message);
