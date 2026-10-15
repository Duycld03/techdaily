using System.Text.Json;
using DeepPace.Application.Interfaces;
using DeepPace.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using WebPush;
using Xunit;

namespace DeepPace.Tests.Infrastructure;

public class WebPushServiceTests
{
    [Theory]
    [InlineData(null, "mailto:support@deeppace.app", null, "deeppace-daily")]
    [InlineData("mailto:configured@example.test", "mailto:configured@example.test", "custom-tag", "custom-tag")]
    public async Task SenderReceivesCanonicalDefaultsOrExplicitConfiguration(
        string? configuredSubject, string expectedSubject, string? tag, string expectedTag)
    {
        var keys = VapidHelper.GenerateVapidKeys();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["WebPush:Subject"] = configuredSubject,
            ["WebPush:PublicKey"] = keys.PublicKey,
            ["WebPush:PrivateKey"] = keys.PrivateKey
        }).Build();
        string? actualPayload = null;
        VapidDetails? actualVapid = null;
        CancellationToken actualCancellation = default;
        var sender = new WebPushService(configuration, NullLogger<WebPushService>.Instance,
            (subscription, payload, vapid, cancellationToken) =>
            {
                subscription.Endpoint.Should().Be("https://push.example.test/subscription");
                actualPayload = payload;
                actualVapid = vapid;
                actualCancellation = cancellationToken;
                return Task.CompletedTask;
            });
        using var cancellation = new CancellationTokenSource();
        var sent = await sender.SendNotificationAsync("https://push.example.test/subscription", "test-key", "test-auth",
            new PushNotificationPayload("DeepPace", "Reminder", Tag: tag), cancellation.Token);

        sent.Should().BeTrue();
        actualVapid!.Subject.Should().Be(expectedSubject);
        actualVapid.PublicKey.Should().Be(keys.PublicKey);
        actualVapid.PrivateKey.Should().Be(keys.PrivateKey);
        actualCancellation.Should().Be(cancellation.Token);
        using var json = JsonDocument.Parse(actualPayload!);
        json.RootElement.GetProperty("tag").GetString().Should().Be(expectedTag);
        json.RootElement.GetProperty("url").GetString().Should().Be("/today");
    }
}
