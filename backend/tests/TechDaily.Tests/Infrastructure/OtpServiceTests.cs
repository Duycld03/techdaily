using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Infrastructure.Persistence;
using TechDaily.Infrastructure.Services;
using Xunit;

namespace TechDaily.Tests.Infrastructure;

public class OtpServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TechDailyDbContext> _options;
    private readonly MutableTimeProvider _time = new(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
    private readonly CapturingEmailSender _email = new();

    public OtpServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<TechDailyDbContext>().UseSqlite(_connection).Options;
        using var db = new TechDailyDbContext(_options);
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private OtpService NewService() => new(new TechDailyDbContext(_options), _email, _time);

    [Fact]
    public async Task Verify_AfterExpiry_ReturnsExpired()
    {
        var svc = NewService();
        (await svc.RequestAsync("user@example.com", OtpPurpose.PasswordReset, null, "en")).IsSuccess.Should().BeTrue();
        var code = _email.LastCode!;

        _time.Advance(TimeSpan.FromMinutes(11));

        var result = await NewService().VerifyAsync("user@example.com", OtpPurpose.PasswordReset, code);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AUTH_OTP_EXPIRED");
    }

    [Fact]
    public async Task Verify_AfterFiveWrongAttempts_InvalidatesCode()
    {
        var svc = NewService();
        await svc.RequestAsync("user@example.com", OtpPurpose.PasswordReset, null, "en");
        var correct = _email.LastCode!;

        for (var i = 0; i < 4; i++)
        {
            var r = await NewService().VerifyAsync("user@example.com", OtpPurpose.PasswordReset, "000000" == correct ? "111111" : "000000");
            r.Error.Code.Should().Be("AUTH_OTP_INVALID");
        }

        // 5th wrong attempt invalidates
        var fifth = await NewService().VerifyAsync("user@example.com", OtpPurpose.PasswordReset, "000000" == correct ? "111111" : "000000");
        fifth.Error.Code.Should().Be("AUTH_OTP_MAX_ATTEMPTS");

        // Even the correct code no longer works
        var afterLock = await NewService().VerifyAsync("user@example.com", OtpPurpose.PasswordReset, correct);
        afterLock.Error.Code.Should().Be("AUTH_OTP_MAX_ATTEMPTS");
    }

    [Fact]
    public async Task Request_WithinCooldown_IsRejected_ThenAllowedAfter()
    {
        (await NewService().RequestAsync("user@example.com", OtpPurpose.PasswordReset, null, "en")).IsSuccess.Should().BeTrue();

        _time.Advance(TimeSpan.FromSeconds(30));
        var second = await NewService().RequestAsync("user@example.com", OtpPurpose.PasswordReset, null, "en");
        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Be("AUTH_OTP_RESEND_COOLDOWN");

        _time.Advance(TimeSpan.FromSeconds(31)); // total 61s since first
        (await NewService().RequestAsync("user@example.com", OtpPurpose.PasswordReset, null, "en")).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Verify_ConsumedCode_CannotBeReused()
    {
        await NewService().RequestAsync("user@example.com", OtpPurpose.PasswordReset, null, "en");
        var code = _email.LastCode!;

        var first = await NewService().VerifyAsync("user@example.com", OtpPurpose.PasswordReset, code);
        first.IsSuccess.Should().BeTrue();

        var second = await NewService().VerifyAsync("user@example.com", OtpPurpose.PasswordReset, code);
        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Be("AUTH_OTP_INVALID");
    }

    private sealed class MutableTimeProvider : TimeProvider
    {
        private DateTimeOffset _now;
        public MutableTimeProvider(DateTimeOffset start) => _now = start;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now = _now.Add(by);
    }

    private sealed class CapturingEmailSender : IEmailSender
    {
        public string? LastCode { get; private set; }
        public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        {
            var match = Regex.Match(htmlBody, "\\d{6}");
            if (match.Success) LastCode = match.Value;
            return Task.CompletedTask;
        }
    }
}
