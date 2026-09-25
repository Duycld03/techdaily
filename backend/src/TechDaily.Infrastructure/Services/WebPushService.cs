using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TechDaily.Application.Interfaces;
using WebPush;

namespace TechDaily.Infrastructure.Services;

public class WebPushSubscriptionExpiredException : Exception
{
    public string Endpoint { get; }

    public WebPushSubscriptionExpiredException(string endpoint, string message, Exception? inner = null)
        : base(message, inner)
    {
        Endpoint = endpoint;
    }
}

public class WebPushService : IWebPushService
{
    private readonly ILogger<WebPushService> _logger;
    private readonly VapidDetails? _vapidDetails;
    private readonly WebPushClient _client;

    public string PublicKey => _vapidDetails?.PublicKey ?? string.Empty;

    public WebPushService(IConfiguration configuration, ILogger<WebPushService> logger)
    {
        _logger = logger;
        _client = new WebPushClient();

        var subject = configuration["WebPush:Subject"] ?? "mailto:support@techdaily.app";
        var publicKey = configuration["WebPush:PublicKey"];
        var privateKey = configuration["WebPush:PrivateKey"];

        if (!string.IsNullOrWhiteSpace(publicKey) && !string.IsNullOrWhiteSpace(privateKey))
        {
            _vapidDetails = new VapidDetails(subject, publicKey, privateKey);
        }
        else
        {
            _logger.LogWarning("VAPID keys not configured. Web push notifications will be simulated.");
        }
    }

    public async Task<bool> SendNotificationAsync(
        string endpoint,
        string p256dh,
        string auth,
        PushNotificationPayload payload,
        CancellationToken cancellationToken = default)
    {
        if (_vapidDetails == null)
        {
            _logger.LogInformation("Simulating Web Push to {Endpoint}: {Title} - {Body}", endpoint, payload.Title, payload.Body);
            return true;
        }

        try
        {
            var subscription = new PushSubscription(endpoint, p256dh, auth);
            var jsonPayload = JsonSerializer.Serialize(new
            {
                title = payload.Title,
                body = payload.Body,
                url = payload.Url ?? "/today",
                tag = payload.Tag ?? "techdaily-daily",
                icon = payload.Icon ?? "/icon.png"
            });

            await _client.SendNotificationAsync(subscription, jsonPayload, _vapidDetails, cancellationToken);
            return true;
        }
        catch (WebPushException ex) when (ex.StatusCode == HttpStatusCode.NotFound || ex.StatusCode == HttpStatusCode.Gone)
        {
            _logger.LogWarning("Web Push endpoint expired ({StatusCode}): {Endpoint}", ex.StatusCode, endpoint);
            throw new WebPushSubscriptionExpiredException(endpoint, $"Endpoint returned {ex.StatusCode}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Web Push notification to {Endpoint}", endpoint);
            return false;
        }
    }
}
