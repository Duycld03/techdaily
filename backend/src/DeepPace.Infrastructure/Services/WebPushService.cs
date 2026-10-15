using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DeepPace.Application.Interfaces;
using WebPush;

namespace DeepPace.Infrastructure.Services;

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
    private readonly Func<PushSubscription, string, VapidDetails, CancellationToken, Task> _send;

    public string PublicKey => _vapidDetails?.PublicKey ?? string.Empty;

    public WebPushService(IConfiguration configuration, ILogger<WebPushService> logger)
        : this(configuration, logger, null)
    {
    }

    internal WebPushService(IConfiguration configuration, ILogger<WebPushService> logger,
        Func<PushSubscription, string, VapidDetails, CancellationToken, Task>? send)
    {
        _logger = logger;
        _client = new WebPushClient();
        _send = send ?? ((subscription, payload, vapid, cancellationToken) =>
            _client.SendNotificationAsync(subscription, payload, vapid, cancellationToken));

        var subject = configuration["WebPush:Subject"] ?? "mailto:support@deeppace.app";
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
                tag = payload.Tag ?? "deeppace-daily",
                icon = payload.Icon ?? "/icon.png"
            });

            await _send(subscription, jsonPayload, _vapidDetails, cancellationToken);
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
