namespace TechDaily.Application.Interfaces;

/// <summary>
/// Transport-agnostic sender for transactional emails (for example OTP codes).
/// Implementations deliver through a concrete transport such as SMTP.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends a single transactional email. Implementations MUST throw when the
    /// transport is not configured rather than silently succeeding.
    /// </summary>
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}
