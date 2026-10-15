using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Enums;

namespace DeepPace.Infrastructure.Services;

public class AccountSessionAuthority : IAccountSessionAuthority
{
    private readonly IDeepPaceDbContext _dbContext;

    public AccountSessionAuthority(IDeepPaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AccountAuthorityResult> ValidateSessionAsync(Guid userId, int sessionVersion, CancellationToken ct = default)
    {
        if (userId == Guid.Empty || sessionVersion <= 0)
        {
            return new AccountAuthorityResult(AccountAuthorityStatus.UserNotFound);
        }

        try
        {
            // Use narrow query projection on non-deleted users
            var user = await _dbContext.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, ct);

            if (user == null)
            {
                return new AccountAuthorityResult(AccountAuthorityStatus.UserNotFound);
            }

            if (user.Status == AccountStatus.Suspended)
            {
                return new AccountAuthorityResult(AccountAuthorityStatus.Suspended, user);
            }

            if (user.AuthenticationVersion != sessionVersion)
            {
                return new AccountAuthorityResult(AccountAuthorityStatus.StaleVersion, user);
            }

            return new AccountAuthorityResult(AccountAuthorityStatus.Valid, user);
        }
        catch (Exception)
        {
            // Database outage maps to Unavailable (503)
            return new AccountAuthorityResult(AccountAuthorityStatus.Unavailable);
        }
    }
}
