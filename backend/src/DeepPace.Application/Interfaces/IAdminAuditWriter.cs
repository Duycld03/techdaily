using DeepPace.Domain.Entities;

namespace DeepPace.Application.Interfaces;

public interface IAdminAuditWriter
{
    Task AppendEventAsync(AdminAuditEvent auditEvent, CancellationToken ct = default);
}
