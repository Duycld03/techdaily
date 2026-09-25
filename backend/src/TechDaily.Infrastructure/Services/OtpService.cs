using System.Globalization;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;

namespace TechDaily.Infrastructure.Services;

/// <summary>
/// Persists email OTPs (hashed) and enforces the shared issuance/validation policy:
/// 6-digit CSPRNG codes, 10-minute expiry, single-use, max 5 attempts, 60s resend cooldown.
/// </summary>
public class OtpService : IOtpService
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan Expiry = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);

    private readonly ITechDailyDbContext _db;
    private readonly IEmailSender _email;
    private readonly TimeProvider _time;

    public OtpService(ITechDailyDbContext db, IEmailSender email, TimeProvider time)
    {
        _db = db;
        _email = email;
        _time = time;
    }

    public async Task<Result> RequestAsync(string email, OtpPurpose purpose, PendingRegistration? pending, string? locale, CancellationToken ct = default)
    {
        var now = _time.GetUtcNow();
        var normalized = Normalize(email);

        var existing = await _db.EmailOtps
            .Where(o => o.Email == normalized && o.Purpose == purpose && o.ConsumedAt == null)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        var latest = existing.FirstOrDefault();
        if (latest != null && now - latest.CreatedAt < ResendCooldown)
        {
            return Error.OtpResendCooldown;
        }

        // Carry pending registration over on resend when the caller did not supply it.
        var effectivePending = pending;
        if (effectivePending == null
            && purpose == OtpPurpose.EmailVerification
            && latest?.PendingPasswordHash != null)
        {
            effectivePending = new PendingRegistration(
                latest.PendingName ?? string.Empty,
                latest.PendingPasswordHash,
                latest.PendingLocale ?? "en");
        }

        if (purpose == OtpPurpose.EmailVerification && effectivePending == null)
        {
            // No pending registration to (re)send a verification code for.
            return Error.OtpInvalid;
        }

        var code = GenerateCode();
        var emailLocale = effectivePending?.Locale ?? locale ?? "en";
        var (subject, body) = Compose(purpose, code, emailLocale);

        // Send first; only persist once delivery succeeds so a transport failure
        // neither leaves an orphan row nor starts a cooldown the user cannot clear.
        await _email.SendAsync(normalized, subject, body, ct);

        if (existing.Count > 0)
        {
            _db.EmailOtps.RemoveRange(existing);
        }

        await _db.EmailOtps.AddAsync(new EmailOtp
        {
            Email = normalized,
            Purpose = purpose,
            CodeHash = RefreshTokenService.HashToken(code),
            ExpiresAt = now.Add(Expiry),
            CreatedAt = now,
            AttemptCount = 0,
            PendingName = effectivePending?.Name,
            PendingPasswordHash = effectivePending?.PasswordHash,
            PendingLocale = effectivePending?.Locale
        }, ct);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<EmailOtp>> VerifyAsync(string email, OtpPurpose purpose, string code, CancellationToken ct = default)
    {
        var now = _time.GetUtcNow();
        var normalized = Normalize(email);

        var otp = await _db.EmailOtps
            .Where(o => o.Email == normalized && o.Purpose == purpose && o.ConsumedAt == null)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (otp == null)
        {
            return Error.OtpInvalid;
        }

        if (otp.ExpiresAt <= now)
        {
            return Error.OtpExpired;
        }

        if (otp.AttemptCount >= MaxAttempts)
        {
            return Error.OtpMaxAttempts;
        }

        var providedHash = RefreshTokenService.HashToken((code ?? string.Empty).Trim());
        if (!string.Equals(providedHash, otp.CodeHash, StringComparison.Ordinal))
        {
            otp.AttemptCount++;
            otp.MarkUpdated();
            await _db.SaveChangesAsync(ct);
            return otp.AttemptCount >= MaxAttempts ? Error.OtpMaxAttempts : Error.OtpInvalid;
        }

        otp.ConsumedAt = now;
        otp.MarkUpdated();
        await _db.SaveChangesAsync(ct);
        return otp;
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();

    private static string GenerateCode()
        => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);

    private static (string Subject, string Body) Compose(OtpPurpose purpose, string code, string locale)
    {
        var isVietnamese = locale.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
        var action = purpose == OtpPurpose.EmailVerification
            ? (isVietnamese ? "xác minh email" : "verify your email")
            : (isVietnamese ? "đặt lại mật khẩu" : "reset your password");

        var subject = purpose == OtpPurpose.EmailVerification
            ? (isVietnamese ? "Mã xác minh TechDaily" : "Your TechDaily verification code")
            : (isVietnamese ? "Mã đặt lại mật khẩu TechDaily" : "Your TechDaily password reset code");

        var intro = isVietnamese
            ? $"Dùng mã bên dưới để {action}. Mã có hiệu lực trong 10 phút."
            : $"Use the code below to {action}. It expires in 10 minutes.";
        var ignore = isVietnamese
            ? "Nếu bạn không yêu cầu, hãy bỏ qua email này."
            : "If you did not request this, you can safely ignore this email.";

        var body =
            $"<div style=\"font-family:system-ui,-apple-system,Segoe UI,Roboto,sans-serif;\">" +
            $"<p>{intro}</p>" +
            $"<p style=\"font-size:28px;font-weight:700;letter-spacing:6px;margin:16px 0;\">{code}</p>" +
            $"<p style=\"color:#64748b;font-size:13px;\">{ignore}</p>" +
            $"</div>";

        return (subject, body);
    }
}
