using TechDaily.Application.Features.Notes.DTOs;

namespace TechDaily.Application.Features.Notes.GetHighlights;

public record TagCountDto(string Tag, int Count);

public record GetHighlightsResponse(
    List<HighlightDto> Highlights,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    List<TagCountDto> TagCounts)
{
    public GetHighlightsResponse(List<HighlightDto> highlights, int totalCount, int page, int pageSize, List<TagCountDto>? tagCounts = null)
        : this(highlights, totalCount, page, pageSize, totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / (pageSize > 0 ? pageSize : 15)), tagCounts ?? new())
    {
    }
}
