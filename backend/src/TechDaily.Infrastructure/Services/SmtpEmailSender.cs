using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using TechDaily.Application.Interfaces;

namespace TechDaily.Infrastructure.Services;

/// <summary>
/// Sends transactional emails through an SMTP transport (e.g. Gmail app-password).
/// Configuration keys live under <c>Email:Smtp:*</c>; credentials are secrets and
/// are only ever read from configuration, never logged.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        var host = _configuration["Email:Smtp:Host"];
        var username = _configuration["Email:Smtp:Username"];
        var password = _configuration["Email:Smtp:Password"];
        var from = _configuration["Email:Smtp:From"];
        var port = _configuration.GetValue<int?>("Email:Smtp:Port") ?? 587;
        var useStartTls = _configuration.GetValue<bool?>("Email:Smtp:UseStartTls") ?? true;

        if (string.IsNullOrWhiteSpace(host)
            || string.IsNullOrWhiteSpace(username)
            || string.IsNullOrWhiteSpace(password))
        {
            // Fail loudly: never report a fake success when the transport is unconfigured.
            throw new InvalidOperationException(
                "SMTP email transport is not configured. Set Email:Smtp:Host, Email:Smtp:Username, and Email:Smtp:Password (secrets belong in .env / appsettings.Local.json).");
        }

        var senderAddress = string.IsNullOrWhiteSpace(from) ? username : from;

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(senderAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            var socketOptions = useStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect;
            await client.ConnectAsync(host, port, socketOptions, ct);
            await client.AuthenticateAsync(username, password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            // Log the failure and recipient, but never the credentials.
            _logger.LogError(ex, "Failed to send transactional email to {Recipient} via SMTP host {Host}", toEmail, host);
            throw;
        }
    }
}
