using TechDaily.Domain.Common;

namespace TechDaily.Domain.Entities;

/// <summary>
/// Purpose an <see cref="EmailOtp"/> was issued for.
/// </summary>
public enum OtpPurpose
{
    EmailVerification,
    PasswordReset
}

/// <summary>
/// A single-use email one-time passcode. For <see cref="OtpPurpose.EmailVerification"/>
/// the row also carries the pending registration so that no <see cref="User"/> exists
/// until the code is confirmed. The raw code is never stored; only its hash.
/// </summary>
public class EmailOtp : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public int AttemptCount { get; set; }

    // Pending registration payload (EmailVerification only). The password is already PBKDF2-hashed.
    public string? PendingName { get; set; }
    public string? PendingPasswordHash { get; set; }
    public string? PendingLocale { get; set; }
}
