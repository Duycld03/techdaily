using DeepPace.Application.Common;
using DeepPace.Domain.Entities;

namespace DeepPace.Application.Interfaces;

public interface IAdministrativeUnitOfWork
{
    Task<Result<TResult>> ExecuteAccountControlCommandAsync<TResult>(
        Guid actorUserId,
        int presentedSessionVersion,
        IReadOnlyCollection<AdminPermission> requiredPermissions,
        Guid targetUserId,
        int expectedVersion,
        string commandName,
        string? correlationId,
        Func<User, Task<Result>>? preTargetValidation,
        Func<User, User, Role, Task<Result<(TResult Result, AdminAuditEvent AuditEvent)>>> command,
        CancellationToken ct = default);

    Task<Result<TResult>> ExecuteCategoryCommandAsync<TResult>(
        Guid actorUserId,
        int presentedSessionVersion,
        IReadOnlyCollection<AdminPermission> requiredPermissions,
        Guid? targetCategoryId,
        int? expectedVersion,
        string commandName,
        string? correlationId,
        Func<User, DocumentCategory?, Task<Result<(TResult Result, AdminAuditEvent? AuditEvent)>>> command,
        CancellationToken ct = default);

    Task<Result<TResult>> ExecutePrivilegedMutationAsync<TResult>(
        Guid actorUserId,
        int presentedSessionVersion,
        IReadOnlyCollection<AdminPermission> requiredPermissions,
        Func<Task<Result<TResult>>> mutation,
        CancellationToken ct = default);

    Task AppendCleanAuditEventAsync(AdminAuditEvent auditEvent, CancellationToken ct = default);
}
