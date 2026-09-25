namespace TechDaily.Api.Contracts;

/// <summary>
/// Response body for <c>GET /api/v1/notifications/push/vapid-public-key</c>:
/// the VAPID public key used to create a browser Web Push subscription.
/// </summary>
public record VapidPublicKeyResponse(string PublicKey);
