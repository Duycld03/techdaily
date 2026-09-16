namespace TechDaily.Application.Interfaces;

public record PushNotificationPayload(
    string Title,
    string Body,
    string? Url = "/today",
    string? Tag = "techdaily-daily",
    string? Icon = "/icon.png"
);

public interface IWebPushService
{
    string PublicKey { get; }
    Task<bool> SendNotificationAsync(
        string endpoint,
        string p256dh,
        string auth,
        PushNotificationPayload payload,
        CancellationToken cancellationToken = default);
}
