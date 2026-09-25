using TechDaily.Application.Common;
using TechDaily.Domain.Entities;

namespace TechDaily.Application.Interfaces;

/// <summary>
/// Registration data held only until an email-verification OTP is confirmed.
/// The password is already hashed; the raw password is never stored.
/// </summary>
public sealed record PendingRegistration(string Name, string PasswordHash, string Locale);

/// <summary>
/// Issues and verifies single-use email one-time passcodes for registration
/// and password reset. Enforces expiry, attempt limits, and resend cooldown.
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Issues (or re-issues after cooldown) a passcode for the given email and purpose
    /// and emails it. For <see cref="OtpPurpose.EmailVerification"/> a
    /// <paramref name="pending"/> registration is required (or carried over from a prior
    /// unconsumed code on resend). Returns <c>AUTH_OTP_RESEND_COOLDOWN</c> when called
    /// inside the cooldown window.
    /// </summary>
    Task<Result> RequestAsync(string email, OtpPurpose purpose, PendingRegistration? pending, string? locale, CancellationToken ct = default);

    /// <summary>
    /// Validates and consumes a passcode. On success returns the consumed row (carrying
    /// any pending registration). On failure returns an <c>AUTH_OTP_*</c> error.
    /// </summary>
    Task<Result<EmailOtp>> VerifyAsync(string email, OtpPurpose purpose, string code, CancellationToken ct = default);
}
