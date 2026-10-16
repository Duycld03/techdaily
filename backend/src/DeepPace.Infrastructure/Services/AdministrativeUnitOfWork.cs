using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
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
        Func<User, Task<Result>>? preTargetValidation,
        Func<User, User, Role, Task<Result<(TResult Result, AdminAuditEvent AuditEvent)>>> command,
        CancellationToken ct = default)
    {
        var isNpgsql = _dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
        var authorized = false;
        var auditStaged = false;

        if (!isNpgsql)
        {
            await NonPostgresLock.WaitAsync(ct);
        }

        try
        {
            Result<TResult> result;
            var succeeded = false;
            try
            {
                result = await ExecuteLockedAsync();
                succeeded = result.IsSuccess;
            }
            catch (Exception ex) when (ex is DbUpdateException or DbException or TimeoutException)
            {
                _serviceProvider?.GetService<ILogger<AdministrativeUnitOfWork>>()?.LogError(
                    ex, "Account command {Command} failed for actor {ActorId}, target {TargetUserId}, correlation {CorrelationId}",
                    commandName, actorUserId, targetUserId, correlationId);

                result = auditStaged
                    ? Error.Custom("AUDIT_PERSISTENCE_UNAVAILABLE", "The account change could not be committed with its audit history.")
                    : Error.Custom("Account.Unavailable", "Account administration is temporarily unavailable.");
            }
            finally
            {
                // ExecuteLockedAsync has rolled back and disposed its transaction before tracking is discarded.
                if (!succeeded) _dbContext.ChangeTracker.Clear();
            }

            if (result.IsFailure && authorized &&
                result.Error.Code is not ("AUTH_FORBIDDEN" or "Account.NotFound" or "AUDIT_PERSISTENCE_UNAVAILABLE" or "Account.Unavailable"))
            {
                var recorded = await RecordAccountRejectionAsync(
                    actorUserId, commandName, targetUserId, result.Error.Code, correlationId, ct);
                if (recorded.IsFailure) return recorded.Error;
            }

            return result;
        }
        finally
        {
            if (!isNpgsql)
            {
                NonPostgresLock.Release();
            }
        }

        async Task<Result<TResult>> ExecuteLockedAsync()
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
            var actorPerms = isNpgsql
                ? await _dbContext.RolePermissions
                    .FromSqlRaw("SELECT * FROM \"RolePermissions\" WHERE \"RoleId\" = {0} FOR SHARE", actor.RoleId)
                    .AsNoTracking().Select(p => p.PermissionKey).ToListAsync(ct)
                : await _dbContext.RolePermissions.AsNoTracking()
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
                AdminPermission.ManageCategories => permSet.Contains(PermissionKeys.ManageCategories),
                _ => false
            });

            if (!hasAllPermissions)
            {
                await tx.RollbackAsync(ct);
                return Error.Custom("AUTH_FORBIDDEN", "The actor account does not hold the required administrator permissions.");
            }

            authorized = true;

            // Post-actor/pre-target validation callback executed after actor admission and before target checks
            if (preTargetValidation != null)
            {
                var preValidationResult = await preTargetValidation(actor);
                if (preValidationResult.IsFailure)
                {
                    await tx.RollbackAsync(ct);
                    return preValidationResult.Error;
                }
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
                return Error.Custom("ACCOUNT_CONCURRENCY_CONFLICT", "Account was modified by another administrator. Please reload.");
            }

            // Self-action protections
            if (actorUserId == targetUserId)
            {
                if (commandName == AdminAuditActions.UserSuspend)
                {
                    await tx.RollbackAsync(ct);
                    return Error.Custom("ACCOUNT_SELF_SUSPENSION_BLOCKED", "An administrator cannot suspend their own account.");
                }
                if (commandName == AdminAuditActions.UserAssignRole)
                {
                    await tx.RollbackAsync(ct);
                    return Error.Custom("ACCOUNT_SELF_ROLE_CHANGE_BLOCKED", "An administrator cannot change their own role.");
                }
            }

            // Load target role by persisted RoleId inside account-command admission without joined navigation locking
            var targetRole = isNpgsql
                ? (await _dbContext.Roles
                    .FromSqlRaw("SELECT * FROM \"Roles\" WHERE \"Id\" = {0} AND \"IsDeleted\" = false FOR SHARE", target.RoleId)
                    .AsNoTracking().ToListAsync(ct)).FirstOrDefault()
                : await _dbContext.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == target.RoleId && !r.IsDeleted, ct);

            if (targetRole == null)
            {
                await tx.RollbackAsync(ct);
                return Error.Custom("Role.NotFound", "The target user's role was not found.");
            }

            // Last active administrator protection for suspension
            if (commandName == AdminAuditActions.UserSuspend
                && target.RoleId == RoleDefaults.AdminRoleId && target.Status == AccountStatus.Active)
            {
                var activeAdminCount = await _dbContext.Users
                    .CountAsync(u => u.RoleId == RoleDefaults.AdminRoleId && !u.IsDeleted && u.Status == AccountStatus.Active, ct);

                if (activeAdminCount <= 1)
                {
                    await tx.RollbackAsync(ct);
                    return Error.Custom("ACCOUNT_LAST_ADMIN_PROTECTED", "Cannot modify the last active administrator.");
                }
            }

            // Execute the command mutation
            var commandResult = await command(actor, target, targetRole);
            if (!commandResult.IsSuccess)
            {
                await tx.RollbackAsync(ct);
                return commandResult.Error;
            }

            var (resultPayload, auditEvent) = commandResult.Value;

            // Stage audit event and commit atomically
            auditStaged = true;
            await _dbContext.AdminAuditEvents.AddAsync(auditEvent, ct);
            await _dbContext.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return resultPayload;
        }
    }
    public async Task<Result<TResult>> ExecuteCategoryCommandAsync<TResult>(
        Guid actorUserId,
        int presentedSessionVersion,
        IReadOnlyCollection<AdminPermission> requiredPermissions,
        Guid? targetCategoryId,
        int? expectedVersion,
        string commandName,
        string? correlationId,
        Func<User, DocumentCategory?, Task<Result<(TResult Result, AdminAuditEvent? AuditEvent)>>> command,
        CancellationToken ct = default)
    {
        var isNpgsql = _dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
        var authorized = false;
        var auditStaged = false;
        var auditTargetId = targetCategoryId;

        if (!isNpgsql) await NonPostgresLock.WaitAsync(ct);
        try
        {
            Result<TResult> result;
            var succeeded = false;
            try
            {
                result = await ExecuteLockedAsync();
                succeeded = result.IsSuccess;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                LogCategoryFailure(ex);
                result = Error.Custom("Category.ConcurrencyConflict", "Category was modified by another administrator.");
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_DocumentCategories_Slug" })
            {
                LogCategoryFailure(ex);
                result = Error.Custom("Category.SlugConflict", "A category with this slug already exists.");
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected })
            {
                LogCategoryFailure(ex);
                result = Error.Custom("Category.ConcurrencyConflict", "Category was modified by another administrator.");
            }
            catch (PostgresException ex) when (ex.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected)
            {
                LogCategoryFailure(ex);
                result = Error.Custom("Category.ConcurrencyConflict", "Category was modified by another administrator.");
            }
            catch (Exception ex) when (ex is DbUpdateException or DbException or TimeoutException)
            {
                LogCategoryFailure(ex);
                result = auditStaged
                    ? Error.Custom("AUDIT_PERSISTENCE_UNAVAILABLE", "The category change could not be committed with its audit history.")
                    : Error.Custom("Category.Unavailable", "Category administration is temporarily unavailable.");
            }
            finally
            {
                // ExecuteLockedAsync has rolled back and disposed its transaction before tracking is discarded.
                if (!succeeded) _dbContext.ChangeTracker.Clear();
            }

            if (result.IsFailure && authorized &&
                result.Error.Code is not ("AUDIT_PERSISTENCE_UNAVAILABLE" or "Category.Unavailable"))
            {
                var recorded = await RecordCategoryRejectionAsync(
                    actorUserId, commandName, auditTargetId, result.Error.Code, correlationId, ct);
                if (recorded.IsFailure) return recorded.Error;
            }

            return result;
        }
        finally
        {
            if (!isNpgsql) NonPostgresLock.Release();
        }

        async Task<Result<TResult>> ExecuteLockedAsync()
        {
            await using var tx = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            var committed = false;
            try
            {
                var actor = isNpgsql
                    ? (await _dbContext.Users
                        .FromSqlRaw("SELECT * FROM \"Users\" WHERE \"Id\" = {0} AND \"IsDeleted\" = false FOR SHARE", actorUserId)
                        .AsNoTracking().ToListAsync(ct)).FirstOrDefault()
                    : await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Id == actorUserId, ct);

                if (actor == null || actor.IsDeleted || actor.Status != AccountStatus.Active)
                    return Error.Custom("AUTH_FORBIDDEN", "The actor account does not hold active administrator permission.");
                if (actor.AuthenticationVersion != presentedSessionVersion)
                    return Error.Custom("AUTH_FORBIDDEN", "The actor session is stale or has been invalidated.");

                var actorPerms = isNpgsql
                    ? await _dbContext.RolePermissions
                        .FromSqlRaw("SELECT * FROM \"RolePermissions\" WHERE \"RoleId\" = {0} FOR SHARE", actor.RoleId)
                        .AsNoTracking().Select(permission => permission.PermissionKey).ToListAsync(ct)
                    : await _dbContext.RolePermissions.AsNoTracking().Where(permission => permission.RoleId == actor.RoleId)
                        .Select(permission => permission.PermissionKey).ToListAsync(ct);
                var permissions = new HashSet<string>(actorPerms, StringComparer.Ordinal);
                var hasAllPermissions = requiredPermissions.All(permission => permission switch
                {
                    AdminPermission.ManageCatalog => permissions.Contains(PermissionKeys.ManageCatalog),
                    AdminPermission.ManageAccounts => permissions.Contains(PermissionKeys.ManageAccounts),
                    AdminPermission.ReadAudit => permissions.Contains(PermissionKeys.ReadAudit),
                    AdminPermission.ManageDocumentOperations => permissions.Contains(PermissionKeys.ManageDocumentOperations),
                    AdminPermission.ViewAdminDashboard => permissions.Contains(PermissionKeys.ViewAdminDashboard),
                    AdminPermission.ManageRoles => permissions.Contains(PermissionKeys.ManageRoles) && permissions.Contains(PermissionKeys.ManageAccounts),
                    AdminPermission.ManageCategories => permissions.Contains(PermissionKeys.ManageCategories),
                    _ => false
                });
                if (!hasAllPermissions)
                    return Error.Custom("AUTH_FORBIDDEN", "The actor account does not hold the required administrator permissions.");
                authorized = true;

                DocumentCategory? target = null;
                if (targetCategoryId.HasValue)
                {
                    target = isNpgsql
                        ? (await _dbContext.DocumentCategories
                            .FromSqlRaw("SELECT * FROM \"DocumentCategories\" WHERE \"Id\" = {0} AND \"IsDeleted\" = false FOR UPDATE", targetCategoryId.Value)
                            .AsNoTracking().ToListAsync(ct)).FirstOrDefault()
                        : await _dbContext.DocumentCategories.AsNoTracking()
                            .FirstOrDefaultAsync(category => category.Id == targetCategoryId.Value && !category.IsDeleted, ct);
                    if (target == null)
                        return Error.Custom("Category.NotFound", "The specified category was not found.");
                    if (expectedVersion is <= 0)
                        return Error.Custom("Category.ValidationFailed", "Expected version must be a positive integer.");
                    if (expectedVersion.HasValue && target.ConcurrencyVersion != expectedVersion.Value)
                        return Error.Custom("Category.ConcurrencyConflict", "Category was modified by another administrator.");

                    var tracked = _dbContext.DocumentCategories.Local.FirstOrDefault(category => category.Id == target.Id);
                    if (tracked != null) _dbContext.Entry(tracked).State = EntityState.Detached;
                    _dbContext.DocumentCategories.Attach(target);
                }

                var execution = await command(actor, target);
                auditTargetId ??= _dbContext.ChangeTracker.Entries<DocumentCategory>()
                    .Where(entry => entry.State == EntityState.Added).Select(entry => (Guid?)entry.Entity.Id).FirstOrDefault();
                if (execution.IsFailure) return execution.Error;

                var (payload, auditEvent) = execution.Value;
                if (auditEvent != null)
                {
                    auditStaged = true;
                    await _dbContext.AdminAuditEvents.AddAsync(auditEvent, ct);
                    await _dbContext.SaveChangesAsync(ct);
                }
                // Null audit is a valid no-op only after the authoritative checks above.
                await tx.CommitAsync(ct);
                committed = true;
                return Result<TResult>.Success(payload);
            }
            finally
            {
                if (!committed) await tx.RollbackAsync(CancellationToken.None);
            }
        }

        void LogCategoryFailure(Exception exception)
        {
            _serviceProvider?.GetService<ILogger<AdministrativeUnitOfWork>>()?.LogError(
                exception, "Category command {Command} failed for actor {ActorId}, category {CategoryId}, correlation {CorrelationId}",
                commandName, actorUserId, auditTargetId, correlationId);
        }
    }

    private async Task<Result<bool>> RecordCategoryRejectionAsync(
        Guid actorUserId,
        string commandName,
        Guid? targetCategoryId,
        string errorCode,
        string? correlationId,
        CancellationToken ct)
    {
        try
        {
            var rejectEvent = AdminAuditEvent.Create(
                actorUserId: actorUserId,
                action: commandName,
                targetType: "Category",
                targetId: targetCategoryId?.ToString() ?? "unassigned",
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
            else
            {
                var options = _dbContextOptions ?? (DbContextOptions<DeepPaceDbContext>)_dbContext.GetService<IDbContextOptions>();
                await using var cleanDb = new DeepPaceDbContext(options);
                await cleanDb.AdminAuditEvents.AddAsync(rejectEvent, ct);
                await cleanDb.SaveChangesAsync(ct);
            }
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _serviceProvider?.GetService<ILogger<AdministrativeUnitOfWork>>()?.LogError(
                ex, "Category rejection audit failed for {Command}, category {CategoryId}, correlation {CorrelationId}",
                commandName, targetCategoryId, correlationId);
            return Error.Custom("AUDIT_PERSISTENCE_UNAVAILABLE", "Category rejection history is temporarily unavailable.");
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
                AdminPermission.ManageCategories => permSet.Contains(PermissionKeys.ManageCategories),
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

    private async Task<Result<bool>> RecordAccountRejectionAsync(
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
            else
            {
                var options = _dbContextOptions ?? (DbContextOptions<DeepPaceDbContext>)_dbContext.GetService<IDbContextOptions>();
                await using var cleanDb = new DeepPaceDbContext(options);
                await cleanDb.AdminAuditEvents.AddAsync(rejectEvent, ct);
                await cleanDb.SaveChangesAsync(ct);
            }

            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _serviceProvider?.GetService<ILogger<AdministrativeUnitOfWork>>()?.LogError(
                ex, "Account rejection audit failed for {Command}, user {TargetUserId}, correlation {CorrelationId}",
                commandName, targetUserId, correlationId);
            return Error.Custom("AUDIT_PERSISTENCE_UNAVAILABLE", "Account rejection history is temporarily unavailable.");
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
        else
        {
            var options = _dbContextOptions ?? (DbContextOptions<DeepPaceDbContext>)_dbContext.GetService<IDbContextOptions>();
            await using var cleanDb = new DeepPaceDbContext(options);
            await cleanDb.AdminAuditEvents.AddAsync(auditEvent, ct);
            await cleanDb.SaveChangesAsync(ct);
        }
    }
}
