using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Notes.DTOs;
using TechDaily.Application.Interfaces;

namespace TechDaily.Application.Features.Notes.GetHighlights;

public record GetHighlightsRequest(Guid UserId, string? Tag = null);

public class GetHighlightsResponse
{
    public List<HighlightDto> Highlights { get; set; } = new();
}

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
        var query = _dbContext.UserHighlights
            .Include(h => h.DocumentChunk)
                .ThenInclude(c => c.DocumentBook)
            .Where(h => h.UserId == request.UserId)
            .OrderByDescending(h => h.CreatedAt)
            .AsNoTracking();

        var list = await query.ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Tag))
        {
            list = list.Where(h => h.Tags.Contains(request.Tag, StringComparer.OrdinalIgnoreCase)).ToList();
        }

        var dtos = list.Select(h => new HighlightDto
        {
            Id = h.Id,
            DocumentChunkId = h.DocumentChunkId,
            ChapterTitle = h.DocumentChunk?.ChapterTitle ?? "Reading Slice",
            BookTitle = h.DocumentChunk?.DocumentBook?.Title ?? "Core Curriculum",
            SelectedText = h.SelectedText,
            Note = h.Note,
            Tags = h.Tags,
            CreatedAt = h.CreatedAt
        }).ToList();

        return new GetHighlightsResponse { Highlights = dtos };
    }
}
