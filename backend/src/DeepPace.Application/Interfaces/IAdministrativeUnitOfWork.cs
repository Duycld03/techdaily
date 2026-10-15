using DeepPace.Application.Common;
using DeepPace.Domain.Entities;

namespace DeepPace.Application.Interfaces;

public interface IAdministrativeUnitOfWork
{
    Task<Result<TResult>> ExecuteAccountControlCommandAsync<TResult>(
        Guid actorUserId,
        Guid targetUserId,
        int expectedVersion,
        string commandName,
        string? correlationId,
        Func<User, User, Task<Result<(TResult Result, AdminAuditEvent AuditEvent)>>> command,
        CancellationToken ct = default);

    Task AppendCleanAuditEventAsync(AdminAuditEvent auditEvent, CancellationToken ct = default);
}
