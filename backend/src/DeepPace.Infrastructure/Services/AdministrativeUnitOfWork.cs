using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DeepPace.Application.Common;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;

namespace DeepPace.Infrastructure.Services;

public class AdministrativeUnitOfWork : IAdministrativeUnitOfWork
{
    private const long AccountControlAdvisoryLockKey = 0x4450_4143_4354_524CL; // "DPACCTRL"
    private static readonly SemaphoreSlim NonPostgresLock = new(1, 1);

    private readonly DeepPaceDbContext _dbContext;
    private readonly DbContextOptions<DeepPaceDbContext>? _dbContextOptions;
    private readonly IServiceProvider? _serviceProvider;

    public AdministrativeUnitOfWork(
        DeepPaceDbContext dbContext,
        DbContextOptions<DeepPaceDbContext>? dbContextOptions = null,
        IServiceProvider? serviceProvider = null)
    {
        _dbContext = dbContext;
        _dbContextOptions = dbContextOptions;
        _serviceProvider = serviceProvider;
    }
    public async Task<Result<TResult>> ExecuteAccountControlCommandAsync<TResult>(
        Guid actorUserId,
        int presentedSessionVersion,
        IReadOnlyCollection<AdminPermission> requiredPermissions,
        Guid targetUserId,
        int expectedVersion,
        string commandName,
        string? correlationId,
        Func<User, User, Task<Result<(TResult Result, AdminAuditEvent AuditEvent)>>> command,
        CancellationToken ct = default)
    {
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
                // Acquire coarse transaction-scoped advisory lock in PostgreSQL
                await _dbContext.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_xact_lock({AccountControlAdvisoryLockKey})", ct);
            }

            // Lock actor and target rows in consistent identifier order to prevent row-lock deadlocks
            var firstId = actorUserId.CompareTo(targetUserId) < 0 ? actorUserId : targetUserId;
            var secondId = actorUserId.CompareTo(targetUserId) < 0 ? targetUserId : actorUserId;

            User? actor;
            User? target;

            if (firstId == secondId)
            {
                // Actor is targeting self
                actor = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == firstId && !u.IsDeleted, ct);
                target = actor;
            }
            else
            {
                if (isNpgsql)
                {
                    var lockedUsers = await _dbContext.Users
                        .FromSqlRaw("SELECT * FROM \"Users\" WHERE \"Id\" IN ({0}, {1}) AND \"IsDeleted\" = false ORDER BY \"Id\" FOR UPDATE", firstId, secondId)
                        .ToListAsync(ct);

                    actor = lockedUsers.FirstOrDefault(u => u.Id == actorUserId);
                    target = lockedUsers.FirstOrDefault(u => u.Id == targetUserId);
                }
                else
                {
                    actor = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == actorUserId && !u.IsDeleted, ct);
                    target = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == targetUserId && !u.IsDeleted, ct);
                }
            }

            // Recheck authoritative actor state inside the locked transaction
            if (actor == null || actor.IsDeleted || actor.Status != AccountStatus.Active)
            {
                await tx.RollbackAsync(ct);
                return Error.Custom("AUTH_FORBIDDEN", "The actor account does not hold active administrator permission.");
            }

            // Presented session version check: reject stale actor tokens
            if (actor.AuthenticationVersion != presentedSessionVersion)
            {
                await tx.RollbackAsync(ct);
                return Error.Custom("AUTH_FORBIDDEN", "The actor session is stale or has been invalidated.");
            }

            // Verify actor holds all required permissions from authoritative database grants
            var actorPerms = await _dbContext.RolePermissions.AsNoTracking()
                .Where(p => p.RoleId == actor.RoleId)
                .Select(p => p.PermissionKey)
                .ToListAsync(ct);

            var permSet = new HashSet<string>(actorPerms, StringComparer.Ordinal);
            var hasAllPermissions = requiredPermissions.All(perm => perm switch
            {
                AdminPermission.ManageCatalog => permSet.Contains(PermissionKeys.ManageCatalog),
                AdminPermission.ManageAccounts => permSet.Contains(PermissionKeys.ManageAccounts),
                AdminPermission.ReadAudit => permSet.Contains(PermissionKeys.ReadAudit),
                AdminPermission.ManageDocumentOperations => permSet.Contains(PermissionKeys.ManageDocumentOperations),
                AdminPermission.ViewAdminDashboard => permSet.Contains(PermissionKeys.ViewAdminDashboard),
                // Conjunction: ManageRoles requires BOTH ManageRoles AND ManageAccounts
                AdminPermission.ManageRoles => permSet.Contains(PermissionKeys.ManageRoles) && permSet.Contains(PermissionKeys.ManageAccounts),
                _ => false
            });

            if (!hasAllPermissions)
            {
                await tx.RollbackAsync(ct);
                return Error.Custom("AUTH_FORBIDDEN", "The actor account does not hold the required administrator permissions.");
            }

            // Recheck authoritative target existence
            if (target == null || target.IsDeleted)
            {
                await tx.RollbackAsync(ct);
                return Error.Custom("Account.NotFound", "Target account was not found.");
            }

            // Stale concurrency version check
            if (target.AdminControlVersion != expectedVersion)
            {
                await tx.RollbackAsync(ct);
                var recRes = await RecordRejectionAsync(actorUserId, commandName, targetUserId, "ACCOUNT_CONCURRENCY_CONFLICT", correlationId, ct);
                if (recRes.IsFailure) return recRes.Error;
                return Error.Custom("ACCOUNT_CONCURRENCY_CONFLICT", "Account was modified by another administrator. Please reload.");
            }

            // Self-action protections
            if (actorUserId == targetUserId)
            {
                if (commandName == AdminAuditActions.UserSuspend)
                {
                    await tx.RollbackAsync(ct);
                    var recRes = await RecordRejectionAsync(actorUserId, commandName, targetUserId, "ACCOUNT_SELF_SUSPENSION_BLOCKED", correlationId, ct);
                    if (recRes.IsFailure) return recRes.Error;
                    return Error.Custom("ACCOUNT_SELF_SUSPENSION_BLOCKED", "An administrator cannot suspend their own account.");
                }
                if (commandName == AdminAuditActions.UserAssignRole)
                {
                    await tx.RollbackAsync(ct);
                    var recRes = await RecordRejectionAsync(actorUserId, commandName, targetUserId, "ACCOUNT_SELF_ROLE_CHANGE_BLOCKED", correlationId, ct);
                    if (recRes.IsFailure) return recRes.Error;
                    return Error.Custom("ACCOUNT_SELF_ROLE_CHANGE_BLOCKED", "An administrator cannot change their own role.");
                }
            }

            // Last active administrator protection
            if ((commandName == AdminAuditActions.UserSuspend || commandName == AdminAuditActions.UserAssignRole)
                && target.RoleId == RoleDefaults.AdminRoleId)
            {
                var activeAdminCount = await _dbContext.Users
                    .CountAsync(u => u.RoleId == RoleDefaults.AdminRoleId && !u.IsDeleted && u.Status == AccountStatus.Active, ct);

                if (activeAdminCount <= 1)
                {
                    await tx.RollbackAsync(ct);
                    var recRes = await RecordRejectionAsync(actorUserId, commandName, targetUserId, "ACCOUNT_LAST_ADMIN_PROTECTED", correlationId, ct);
                    if (recRes.IsFailure) return recRes.Error;
                    return Error.Custom("ACCOUNT_LAST_ADMIN_PROTECTED", "Cannot modify the last active administrator.");
                }
            }

            // Execute the command mutation
            var commandResult = await command(actor, target);
            if (!commandResult.IsSuccess)
            {
                await tx.RollbackAsync(ct);
                _dbContext.ChangeTracker.Clear();
                var recRes = await RecordRejectionAsync(actorUserId, commandName, targetUserId, commandResult.Error.Code, correlationId, ct);
                if (recRes.IsFailure) return recRes.Error;
                return commandResult.Error;
            }

            var (resultPayload, auditEvent) = commandResult.Value;

            // Stage audit event and commit atomically
            await _dbContext.AdminAuditEvents.AddAsync(auditEvent, ct);
            await _dbContext.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return resultPayload;
        }
        finally
        {
            if (!isNpgsql)
            {
                NonPostgresLock.Release();
            }
        }
    }

    public async Task<Result<TResult>> ExecutePrivilegedMutationAsync<TResult>(
        Guid actorUserId,
        int presentedSessionVersion,
        IReadOnlyCollection<AdminPermission> requiredPermissions,
        Func<Task<Result<TResult>>> mutation,
        CancellationToken ct = default)
    {
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
                // Acquire shared row lock on the actor in PostgreSQL (conflicts with exclusive row locks on actor)
                await _dbContext.Database.ExecuteSqlRawAsync(
                    "SELECT \"Id\" FROM \"Users\" WHERE \"Id\" = {0} FOR SHARE",
                    new object[] { actorUserId },
                    ct);
            }

            // Read fresh authoritative actor state from database
            var actor = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == actorUserId, ct);

            if (actor == null || actor.IsDeleted || actor.Status != AccountStatus.Active)
            {
                await tx.RollbackAsync(ct);
                return Error.Custom("AUTH_FORBIDDEN", "The actor account does not hold active administrator permission.");
            }

            if (actor.AuthenticationVersion != presentedSessionVersion)
            {
                await tx.RollbackAsync(ct);
                return Error.Custom("AUTH_FORBIDDEN", "The actor session is stale or has been invalidated.");
            }

            // Verify actor holds all required permissions from authoritative database grants
            var actorPerms = await _dbContext.RolePermissions
                .AsNoTracking()
                .Where(p => p.RoleId == actor.RoleId)
                .Select(p => p.PermissionKey)
                .ToListAsync(ct);

            var permSet = new HashSet<string>(actorPerms, StringComparer.Ordinal);
            var hasAllPermissions = requiredPermissions.All(perm => perm switch
            {
                AdminPermission.ManageCatalog => permSet.Contains(PermissionKeys.ManageCatalog),
                AdminPermission.ManageAccounts => permSet.Contains(PermissionKeys.ManageAccounts),
                AdminPermission.ReadAudit => permSet.Contains(PermissionKeys.ReadAudit),
                AdminPermission.ManageDocumentOperations => permSet.Contains(PermissionKeys.ManageDocumentOperations),
                AdminPermission.ViewAdminDashboard => permSet.Contains(PermissionKeys.ViewAdminDashboard),
                AdminPermission.ManageRoles => permSet.Contains(PermissionKeys.ManageRoles) && permSet.Contains(PermissionKeys.ManageAccounts),
                _ => false
            });

            if (!hasAllPermissions)
            {
                await tx.RollbackAsync(ct);
                return Error.Custom("AUTH_FORBIDDEN", "The actor account does not hold the required administrator permissions.");
            }

            // Execute the privileged mutation under the transaction
            var mutationResult = await mutation();
            if (!mutationResult.IsSuccess)
            {
                await tx.RollbackAsync(ct);
                _dbContext.ChangeTracker.Clear();
                return mutationResult;
            }

            await tx.CommitAsync(ct);
            return mutationResult;
        }
        finally
        {
            if (!isNpgsql)
            {
                NonPostgresLock.Release();
            }
        }
    }

    private async Task<Result<bool>> RecordRejectionAsync(
        Guid actorUserId,
        string commandName,
        Guid targetUserId,
        string errorCode,
        string? correlationId,
        CancellationToken ct)
    {
        try
        {
            var rejectEvent = AdminAuditEvent.Create(
                actorUserId: actorUserId,
                action: commandName,
                targetType: "User",
                targetId: targetUserId.ToString(),
                outcome: AdminAuditOutcome.Rejected,
                errorCode: errorCode,
                correlationId: correlationId);

            if (_serviceProvider != null)
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                var cleanDb = scope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
                await cleanDb.AdminAuditEvents.AddAsync(rejectEvent, ct);
                await cleanDb.SaveChangesAsync(ct);
            }
            else if (_dbContextOptions != null)
            {
                await using var cleanDb = new DeepPaceDbContext(_dbContextOptions);
                await cleanDb.AdminAuditEvents.AddAsync(rejectEvent, ct);
                await cleanDb.SaveChangesAsync(ct);
            }
            else
            {
                await _dbContext.AdminAuditEvents.AddAsync(rejectEvent, ct);
                await _dbContext.SaveChangesAsync(ct);
            }

            return true;
        }
        catch (Exception ex)
        {
            return Error.Custom("AUDIT_PERSISTENCE_UNAVAILABLE", $"Audit history persistence failed: {ex.Message}");
        }
    }

    public async Task AppendCleanAuditEventAsync(AdminAuditEvent auditEvent, CancellationToken ct = default)
    {
        if (_serviceProvider != null)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var cleanDb = scope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
            await cleanDb.AdminAuditEvents.AddAsync(auditEvent, ct);
            await cleanDb.SaveChangesAsync(ct);
        }
        else if (_dbContextOptions != null)
        {
            await using var cleanDb = new DeepPaceDbContext(_dbContextOptions);
            await cleanDb.AdminAuditEvents.AddAsync(auditEvent, ct);
            await cleanDb.SaveChangesAsync(ct);
        }
        else
        {
            await _dbContext.AdminAuditEvents.AddAsync(auditEvent, ct);
            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
