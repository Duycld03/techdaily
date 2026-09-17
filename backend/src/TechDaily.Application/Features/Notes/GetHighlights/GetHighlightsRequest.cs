namespace TechDaily.Application.Features.Notes.GetHighlights;

public record GetHighlightsRequest(
    Guid UserId,
    string? Tag = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 15);
