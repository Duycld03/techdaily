using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Accounts.DTOs;
using DeepPace.Application.Features.Admin.Audit.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Admin.Audit;

public record AdminGetAuditEventsRequest(
    Guid? ActorUserId = null,
    string? Action = null,
    string? TargetType = null,
    string? TargetId = null,
    AdminAuditOutcome? Outcome = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Page = 1,
    int PageSize = 20);

public class AdminGetAuditEventsHandler : IUseCase<AdminGetAuditEventsRequest, AdminPagedResult<AdminAuditEventDto>>
{
    private readonly IDeepPaceDbContext _dbContext;

    public AdminGetAuditEventsHandler(IDeepPaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<AdminPagedResult<AdminAuditEventDto>>> ExecuteAsync(
        AdminGetAuditEventsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.From.HasValue && request.To.HasValue && request.From.Value > request.To.Value)
        {
            return Error.Custom("Audit.InvalidTimeRange", "From timestamp must be earlier than or equal to To timestamp.");
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : (request.PageSize > 100 ? 100 : request.PageSize);

        var query = _dbContext.AdminAuditEvents.AsNoTracking();

        if (request.ActorUserId.HasValue)
        {
            query = query.Where(e => e.ActorUserId == request.ActorUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            var action = request.Action.Trim();
            query = query.Where(e => e.Action == action || e.Action.ToLower() == action.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(request.TargetType))
        {
            var targetType = request.TargetType.Trim();
            query = query.Where(e => e.TargetType == targetType || e.TargetType.ToLower() == targetType.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(request.TargetId))
        {
            var targetId = request.TargetId.Trim();
            query = query.Where(e => e.TargetId == targetId);
        }

        if (request.Outcome.HasValue)
        {
            query = query.Where(e => e.Outcome == request.Outcome.Value);
        }

        if (request.From.HasValue)
        {
            query = query.Where(e => e.OccurredAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(e => e.OccurredAt < request.To.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new AdminAuditEventDto(
                e.Id,
                e.OccurredAt,
                e.ActorUserId,
                e.Action,
                e.TargetType,
                e.TargetId,
                e.Outcome,
                e.Reason,
                e.ErrorCode,
                e.CorrelationId))
            .ToListAsync(cancellationToken);

        return new AdminPagedResult<AdminAuditEventDto>(items, totalCount, page, pageSize);
    }
}
