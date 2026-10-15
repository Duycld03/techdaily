using DeepPace.Domain.Entities;

namespace DeepPace.Application.Interfaces;

public enum AccountAuthorityStatus
{
    Valid,
    Suspended,
    StaleVersion,
    UserNotFound,
    Unavailable
}

public record AccountAuthorityResult(AccountAuthorityStatus Status, User? User = null);

public interface IAccountSessionAuthority
{
    Task<AccountAuthorityResult> ValidateSessionAsync(Guid userId, int sessionVersion, CancellationToken ct = default);
}
