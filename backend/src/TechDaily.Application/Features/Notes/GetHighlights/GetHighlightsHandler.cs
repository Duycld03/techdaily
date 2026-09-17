using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Notes.DTOs;
using TechDaily.Application.Interfaces;

namespace TechDaily.Application.Features.Notes.GetHighlights;

public class GetHighlightsHandler : IUseCase<GetHighlightsRequest, GetHighlightsResponse>
{
    private readonly ITechDailyDbContext _dbContext;

    public GetHighlightsHandler(ITechDailyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetHighlightsResponse>> ExecuteAsync(
        GetHighlightsRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize > 0 ? Math.Min(request.PageSize, 100) : 15;

        // 1. Query user's non-deleted highlights metadata for global tag counts and tag matching
        var userHighlightMetadata = await _dbContext.UserHighlights
            .AsNoTracking()
            .Where(h => h.UserId == request.UserId)
            .Select(h => new { h.Id, h.Tags })
            .ToListAsync(cancellationToken);

        var tagCounts = userHighlightMetadata
            .SelectMany(h => h.Tags)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .GroupBy(tag => tag.Trim().ToLower())
            .Select(g => new TagCountDto(g.Key, g.Count()))
            .OrderByDescending(t => t.Count)
            .ThenBy(t => t.Tag)
            .ToList();

        // 2. Query filtered highlights
        var query = _dbContext.UserHighlights
            .AsNoTracking()
            .Include(h => h.DocumentChunk)
                .ThenInclude(c => c.DocumentBook)
            .Where(h => h.UserId == request.UserId);

        if (!string.IsNullOrWhiteSpace(request.Tag))
        {
            var tagLower = request.Tag.Trim().ToLower();
            var matchingIds = userHighlightMetadata
                .Where(h => h.Tags != null && h.Tags.Any(t => t.Trim().ToLower() == tagLower))
                .Select(h => h.Id)
                .ToList();

            query = query.Where(h => matchingIds.Contains(h.Id));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(h =>
                h.SelectedText.ToLower().Contains(search) ||
                (h.Note != null && h.Note.ToLower().Contains(search)) ||
                (h.DocumentChunk.DocumentBook != null && h.DocumentChunk.DocumentBook.Title.ToLower().Contains(search)));
        }

        // 3. Count total filtered highlights
        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);

        // 4. Slice page
        var pagedHighlights = await query
            .OrderByDescending(h => h.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // 5. HasFlashcard resolution
        var pagedIds = pagedHighlights.Select(h => h.Id).ToList();
        var cardHighlightIds = pagedIds.Count == 0
            ? new HashSet<Guid>()
            : await _dbContext.SpacedRepetitionCards
                .AsNoTracking()
                .Where(c => c.UserId == request.UserId && c.SourceHighlightId != null && pagedIds.Contains(c.SourceHighlightId.Value))
                .Select(c => c.SourceHighlightId!.Value)
                .ToHashSetAsync(cancellationToken);

        var dtos = pagedHighlights.Select(h => new HighlightDto
        {
            Id = h.Id,
            DocumentChunkId = h.DocumentChunkId,
            ChapterTitle = h.DocumentChunk?.ChapterTitle ?? "Reading Slice",
            BookTitle = h.DocumentChunk?.DocumentBook?.Title ?? "Core Curriculum",
            SelectedText = h.SelectedText,
            Note = h.Note,
            Tags = h.Tags,
            CreatedAt = h.CreatedAt,
            HasFlashcard = cardHighlightIds.Contains(h.Id)
        }).ToList();

        return new GetHighlightsResponse(dtos, totalCount, page, pageSize, totalPages, tagCounts);
    }
}
