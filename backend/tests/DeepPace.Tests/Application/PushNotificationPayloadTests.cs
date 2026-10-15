using DeepPace.Application.Interfaces;
using FluentAssertions;
using Xunit;

namespace DeepPace.Tests.Application;

public class PushNotificationPayloadTests
{
    [Fact]
    public void OmittedNotificationFieldsUseCanonicalDefaults()
    {
        var payload = new PushNotificationPayload("DeepPace", "Reminder");
        payload.Tag.Should().Be("deeppace-daily");
        payload.Url.Should().Be("/today");
        payload.Icon.Should().Be("/icon.png");
    }

    [Fact]
    public void ExplicitNotificationFieldsRemainUnchanged()
    {
        var payload = new PushNotificationPayload("Title", "Body", "/library", "custom-tag", "/custom.png");
        payload.Tag.Should().Be("custom-tag");
        payload.Url.Should().Be("/library");
        payload.Icon.Should().Be("/custom.png");
    }
}
