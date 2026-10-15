using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Audit.DTOs;
using DeepPace.Application.Interfaces;

namespace DeepPace.Application.Features.Admin.Audit;

public record AdminGetAuditEventByIdRequest(Guid Id);

public class AdminGetAuditEventByIdHandler : IUseCase<AdminGetAuditEventByIdRequest, AdminAuditEventDetailDto>
{
    private readonly IDeepPaceDbContext _dbContext;

    public AdminGetAuditEventByIdHandler(IDeepPaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<AdminAuditEventDetailDto>> ExecuteAsync(
        AdminGetAuditEventByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        var evt = await _dbContext.AdminAuditEvents.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (evt == null)
        {
            return Error.Custom("Audit.NotFound", "Audit event not found.");
        }

        // Bounded optional lookup for current actor email (ignoring soft-delete filter to preserve history)
        var actor = await _dbContext.Users.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == evt.ActorUserId, cancellationToken);

        var stateDict = new Dictionary<string, object?>();
        if (!string.IsNullOrWhiteSpace(evt.StatePayload))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(evt.StatePayload);
                if (parsed != null)
                {
                    stateDict = parsed;
                }
            }
            catch
            {
                // Discard invalid JSON safely without failing read
            }
        }

        var dto = new AdminAuditEventDetailDto(
            evt.Id,
            evt.OccurredAt,
            evt.ActorUserId,
            actor?.Email,
            evt.Action,
            evt.TargetType,
            evt.TargetId,
            evt.Outcome,
            evt.Reason,
            evt.ErrorCode,
            evt.CorrelationId,
            evt.ChangedFields,
            stateDict);

        return dto;
    }
}
