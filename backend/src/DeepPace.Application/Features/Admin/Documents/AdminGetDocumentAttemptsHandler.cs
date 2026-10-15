using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Accounts.DTOs;
using DeepPace.Application.Features.Admin.Documents.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Admin.Documents;

public record AdminGetDocumentAttemptsRequest(
    string Scope,
    Guid DocumentId,
    string? State = null,
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null,
    int Page = 1,
    int PageSize = 20);

public class AdminGetDocumentAttemptsHandler : IUseCase<AdminGetDocumentAttemptsRequest, AdminPagedResult<AdminDocumentAttemptItemDto>>
{
    private readonly IDeepPaceDbContext _dbContext;

    public AdminGetDocumentAttemptsHandler(IDeepPaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<AdminPagedResult<AdminDocumentAttemptItemDto>>> ExecuteAsync(
        AdminGetDocumentAttemptsRequest request,
        CancellationToken cancellationToken = default)
    {
        var scopeStr = request.Scope.Trim().ToLowerInvariant();
        if (scopeStr != "personal" && scopeStr != "catalog")
        {
            return new Error("Document.InvalidScope", $"Unsupported scope '{request.Scope}'. Must be 'personal' or 'catalog'.");
        }

        var targetScope = scopeStr == "personal" ? IngestionTargetScope.Personal : IngestionTargetScope.Catalog;

        if (request.FromUtc.HasValue && request.ToUtc.HasValue && request.FromUtc.Value >= request.ToUtc.Value)
        {
            return new Error("Document.InvalidTimeRange", "FromUtc must be earlier than ToUtc.");
        }

        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : Math.Min(request.PageSize, 100);

        var query = _dbContext.IngestionAttempts
            .AsNoTracking()
            .Where(a => a.TargetScope == targetScope && a.TargetDocumentId == request.DocumentId);

        if (!string.IsNullOrWhiteSpace(request.State) && Enum.TryParse<IngestionAttemptState>(request.State, true, out var attemptState))
        {
            query = query.Where(a => a.State == attemptState);
        }

        if (request.FromUtc.HasValue)
        {
            query = query.Where(a => a.QueuedAtUtc >= request.FromUtc.Value);
        }

        if (request.ToUtc.HasValue)
        {
            query = query.Where(a => a.QueuedAtUtc < request.ToUtc.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Deterministic ordering: QueuedAtUtc descending, Id descending
        var items = await query
            .OrderByDescending(a => a.QueuedAtUtc)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AdminDocumentAttemptItemDto(
                a.Id,
                a.TargetDocumentId,
                a.TargetScope.ToString(),
                a.Origin.ToString(),
                a.Stage.ToString(),
                a.State.ToString(),
                a.ProgressPercent,
                a.QueuedAtUtc,
                a.StartedAtUtc,
                a.EndedAtUtc,
                a.ErrorCode,
                a.SafeErrorGuidance))
            .ToListAsync(cancellationToken);

        return new AdminPagedResult<AdminDocumentAttemptItemDto>(items, totalCount, page, pageSize);
    }
}
