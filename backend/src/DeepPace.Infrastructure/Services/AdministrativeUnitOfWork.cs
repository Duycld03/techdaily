using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    private readonly IConfiguration _configuration;

    public AdministrativeUnitOfWork(DeepPaceDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    private HashSet<Guid> GetConfiguredAdminGuids()
    {
        var configuredIds = _configuration.GetSection("CatalogAdministration:UserIds").Get<string[]>()
            ?? _configuration.GetSection("CatalogAdministration__UserIds").Get<string[]>()
            ?? Array.Empty<string>();

        var set = new HashSet<Guid>();
        foreach (var idStr in configuredIds)
        {
            if (Guid.TryParse(idStr, out var id) && id != Guid.Empty)
            {
                set.Add(id);
            }
        }
        return set;
    }

    public async Task<Result<TResult>> ExecuteAccountControlCommandAsync<TResult>(
        Guid actorUserId,
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
            var configuredAdmins = GetConfiguredAdminGuids();
            if (actor == null || actor.IsDeleted || actor.Status != AccountStatus.Active || !configuredAdmins.Contains(actor.Id))
            {
                await tx.RollbackAsync(ct);
                return Error.Custom("AUTH_FORBIDDEN", "The actor account does not hold active administrator permission.");
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
                await RecordRejectionAsync(actorUserId, commandName, targetUserId, "ACCOUNT_CONCURRENCY_CONFLICT", correlationId, ct);
                return Error.Custom("ACCOUNT_CONCURRENCY_CONFLICT", "Account was modified by another administrator. Please reload.");
            }

            // Self-suspension protection
            if (commandName == AdminAuditActions.UserSuspend && actorUserId == targetUserId)
            {
                await tx.RollbackAsync(ct);
                await RecordRejectionAsync(actorUserId, commandName, targetUserId, "ACCOUNT_SELF_SUSPENSION_BLOCKED", correlationId, ct);
                return Error.Custom("ACCOUNT_SELF_SUSPENSION_BLOCKED", "An administrator cannot suspend their own account.");
            }

            // Last active administrator protection
            if (commandName == AdminAuditActions.UserSuspend && configuredAdmins.Contains(targetUserId))
            {
                var activeAdminCount = await _dbContext.Users
                    .CountAsync(u => configuredAdmins.Contains(u.Id) && !u.IsDeleted && u.Status == AccountStatus.Active, ct);

                if (activeAdminCount <= 1)
                {
                    await tx.RollbackAsync(ct);
                    await RecordRejectionAsync(actorUserId, commandName, targetUserId, "ACCOUNT_LAST_ADMIN_PROTECTED", correlationId, ct);
                    return Error.Custom("ACCOUNT_LAST_ADMIN_PROTECTED", "Cannot suspend the last active administrator.");
                }
            }

            // Execute the command mutation
            var commandResult = await command(actor, target);
            if (!commandResult.IsSuccess)
            {
                await tx.RollbackAsync(ct);
                await RecordRejectionAsync(actorUserId, commandName, targetUserId, commandResult.Error.Code, correlationId, ct);
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

    private async Task RecordRejectionAsync(
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

            await AppendCleanAuditEventAsync(rejectEvent, ct);
        }
        catch
        {
            // Do not mask original error if writing rejection fails
        }
    }

    public async Task AppendCleanAuditEventAsync(AdminAuditEvent auditEvent, CancellationToken ct = default)
    {
        await using var cleanTx = await _dbContext.Database.BeginTransactionAsync(ct);
        await _dbContext.AdminAuditEvents.AddAsync(auditEvent, ct);
        await _dbContext.SaveChangesAsync(ct);
        await cleanTx.CommitAsync(ct);
    }
}
