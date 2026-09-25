using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TechDaily.Infrastructure.Services;
using Xunit;

namespace TechDaily.Tests.Infrastructure;

public class SmtpEmailSenderTests
{
    private static IConfiguration Config(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public async Task SendAsync_WhenSmtpUnconfigured_ThrowsInsteadOfSilentSuccess()
    {
        var sender = new SmtpEmailSender(Config(new Dictionary<string, string?>()), new NullLogger<SmtpEmailSender>());

        var act = () => sender.SendAsync("user@example.com", "Subject", "<p>Body</p>");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*");
    }

    [Fact]
    public async Task SendAsync_WhenSendFails_DoesNotLogTheCredential()
    {
        const string secret = "SECRET-APP-PASSWORD-DO-NOT-LOG";
        var logger = new CapturingLogger<SmtpEmailSender>();
        var sender = new SmtpEmailSender(Config(new Dictionary<string, string?>
        {
            ["Email:Smtp:Host"] = "127.0.0.1",
            ["Email:Smtp:Port"] = "1", // refused -> connect fails fast
            ["Email:Smtp:Username"] = "sender@example.com",
            ["Email:Smtp:Password"] = secret,
            ["Email:Smtp:From"] = "sender@example.com"
        }), logger);

        var act = () => sender.SendAsync("user@example.com", "Subject", "<p>Body</p>");

        await act.Should().ThrowAsync<Exception>();
        logger.Entries.Should().NotBeEmpty("a send failure should be logged");
        logger.Entries.Should().NotContain(e => e.Contains(secret), "credentials must never be written to logs");
    }

    private sealed class NullLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Entries { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add(formatter(state, exception) + " " + (exception?.ToString() ?? string.Empty));
    }
}
