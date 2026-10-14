using System.Text.Json.Serialization;
using DeepPace.Application.Features.Notes.DTOs;

namespace DeepPace.Application.Features.Notes.GetHighlights;

public record TagCountDto(string Tag, int Count);

public record GetHighlightsResponse
{
    public List<HighlightDto> Highlights { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public List<TagCountDto> TagCounts { get; init; } = new();
    public int TotalAllCount { get; init; }

    [JsonConstructor]
    public GetHighlightsResponse(
        List<HighlightDto> highlights,
        int totalCount,
        int page,
        int pageSize,
        int totalPages,
        List<TagCountDto> tagCounts,
        int totalAllCount = 0)
    {
        Highlights = highlights;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
        TotalPages = totalPages;
        TagCounts = tagCounts ?? new();
        TotalAllCount = totalAllCount;
    }

    public GetHighlightsResponse(List<HighlightDto> highlights, int totalCount, int page, int pageSize, List<TagCountDto>? tagCounts = null, int? totalAllCount = null)
        : this(highlights, totalCount, page, pageSize, totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / (pageSize > 0 ? pageSize : 15)), tagCounts ?? new(), totalAllCount ?? totalCount)
    {
    }
}
