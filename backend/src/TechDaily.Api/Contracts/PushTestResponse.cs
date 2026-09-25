namespace TechDaily.Api.Contracts;

/// <summary>
/// Response body for <c>POST /api/v1/notifications/push/test</c>: the delivery outcome
/// of the test push, including how many subscriptions were sent, the total attempted,
/// and how many stale subscriptions were purged.
/// </summary>
public record PushTestResponse(bool Success, int Sent, int Total, int StalePurged);
