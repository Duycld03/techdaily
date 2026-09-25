namespace TechDaily.Api.Contracts;

/// <summary>
/// Response body for <c>POST /api/v1/notifications/push/subscribe</c>:
/// acknowledges that the Web Push subscription was registered or updated.
/// </summary>
public record PushAckResponse(bool Success);
