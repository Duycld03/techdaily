using System.Data;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;

namespace DeepPace.Infrastructure.Services;

public class AuthorizationInitializationService : IAuthorizationInitializationService
{
    private const string AdvisoryLockKey = "4354, 524"; // "DPACCTRL" (0x1102, 0x020C)
    private static readonly SemaphoreSlim NonPostgresLock = new(1, 1);

    private readonly DeepPaceDbContext _dbContext;
    private readonly IRefreshTokenService _tokenService;

    public AuthorizationInitializationService(
        DeepPaceDbContext dbContext,
        IRefreshTokenService tokenService)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
    }

    public async Task<bool> IsInitializedAsync(CancellationToken ct = default)
    {
        return await _dbContext.AuthorizationInitializations
            .AsNoTracking()
            .AnyAsync(i => i.Scope == AuthorizationInitialization.DefaultScope, ct);
    }

    public async Task<Result<AuthorizationInitialization>> InitializeAsync(
        IEnumerable<Guid> adminUserIds,
        CancellationToken ct = default)
    {
        var idList = adminUserIds?.ToList() ?? new List<Guid>();
        if (idList.Count == 0)
        {
            return Error.Custom("INIT_EMPTY_ADMIN_SET", "At least one administrator ID must be provided.");
        }

        if (idList.Any(id => id == Guid.Empty))
        {
            return Error.Custom("INIT_INVALID_ADMIN_ID", "One or more administrator IDs are empty or malformed.");
        }

        var sortedIds = idList.Distinct().OrderBy(id => id).ToList();

        var isNpgsql = _dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

        if (!isNpgsql)
        {
            await NonPostgresLock.WaitAsync(ct);
        }

        try
        {
            await using var tx = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            if (isNpgsql)
            {
                await _dbContext.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_xact_lock({AdvisoryLockKey})", ct);
            }

            // 1. Check if already initialized
            var alreadyInitialized = await _dbContext.AuthorizationInitializations
                .AnyAsync(i => i.Scope == AuthorizationInitialization.DefaultScope, ct);

            if (alreadyInitialized)
            {
                await tx.RollbackAsync(ct);
                return Error.Custom("AUTHORIZATION_ALREADY_INITIALIZED", "Authorization has already been initialized. Repeat initialization is prohibited.");
            }

            // 2. Verify protected role and grant seed integrity
            var adminRole = await _dbContext.Roles
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.Id == RoleDefaults.AdminRoleId, ct);

            if (adminRole == null || !adminRole.IsProtected)
            {
                await tx.RollbackAsync(ct);
                return Error.Custom("INIT_SEED_CORRUPT", "Protected Admin role seed is missing or corrupted.");
            }

            var adminPerms = adminRole.Permissions.Select(p => p.PermissionKey).ToHashSet(StringComparer.Ordinal);
            var requiredPerms = new[]
            {
                PermissionKeys.ViewAdminDashboard,
                PermissionKeys.ManageCatalog,
                PermissionKeys.ManageAccounts,
                PermissionKeys.ReadAudit,
                PermissionKeys.ManageDocumentOperations,
                PermissionKeys.ManageRoles
            };

            if (!requiredPerms.All(p => adminPerms.Contains(p)))
            {
                await tx.RollbackAsync(ct);
                return Error.Custom("INIT_SEED_CORRUPT", "Protected Admin role permissions are missing or corrupted.");
            }

            // 3. Lock candidate users in deterministic GUID order
            if (isNpgsql)
            {
                foreach (var id in sortedIds)
                {
                    await _dbContext.Database.ExecuteSqlRawAsync(
                        "SELECT \"Id\" FROM \"Users\" WHERE \"Id\" = {0} FOR UPDATE",
                        new object[] { id },
                        ct);
                }
            }

            var candidates = await _dbContext.Users
                .IgnoreQueryFilters()
                .Where(u => sortedIds.Contains(u.Id))
                .ToListAsync(ct);
            // 4. Validate existence
            if (candidates.Count != sortedIds.Count)
            {
                await tx.RollbackAsync(ct);
                return Error.Custom("INIT_USER_NOT_FOUND", "One or more administrator identities were not found.");
            }

            // 5. Validate non-deleted
            if (candidates.Any(u => u.IsDeleted))
            {
                await tx.RollbackAsync(ct);
                return Error.Custom("INIT_USER_DELETED", "One or more administrator identities are deleted.");
            }

            // 6. Validate at least one active administrator
            var activeCount = candidates.Count(u => u.Status == AccountStatus.Active);
            if (activeCount == 0)
            {
                await tx.RollbackAsync(ct);
                return Error.Custom("INIT_NO_ACTIVE_ADMIN", "At least one administrator must be active. Zero active administrators cannot satisfy initialization.");
            }

            // 7. Promote selected accounts and advance versions
            foreach (var user in candidates)
            {
                var changed = user.RoleId != RoleDefaults.AdminRoleId;
                if (changed)
                {
                    user.RoleId = RoleDefaults.AdminRoleId;
                    user.AuthenticationVersion++;
                    user.AdminControlVersion++;
                    user.MarkUpdated();
                    await _tokenService.RevokeAllForUserAsync(user.Id, ct);
                }
            }

            // 8. Create and persist immutable receipt
            var receipt = AuthorizationInitialization.Create(sortedIds);
            await _dbContext.AuthorizationInitializations.AddAsync(receipt, ct);

            await _dbContext.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return receipt;
        }
        finally
        {
            if (!isNpgsql)
            {
                NonPostgresLock.Release();
            }
        }
    }
}
