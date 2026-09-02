using TechDaily.Domain.Enums;

namespace TechDaily.Application.Features.Insights.DTOs;

public record TechInsightDto(
    Guid Id,
    string Slug,
    string Title,
    Category Category,
    List<string> Tags,
    string SummaryMarkdown,
    string ProblemSnippet,
    string SolutionSnippet,
    string UnderTheHoodMarkdown,
    string BenchmarkStats,
    string? SourceUrl,
    int LikesCount,
    int BookmarksCount,
    bool IsBookmarkedByUser = false
);

public record GetInsightsFeedRequest(
    Category? Category = null,
    string? Tag = null,
    int Page = 1,
    int PageSize = 10,
    bool Randomize = false,
    Guid? UserId = null,
    bool OnlyBookmarked = false
);

public record GetInsightsFeedResponse(
    List<TechInsightDto> Insights,
    int TotalCount,
    int Page,
    int PageSize,
    bool HasMore
);

public record GenerateInsightRequest(
    Category? PreferredCategory = null,
    string? PreferredTopic = null,
    string Locale = "en"
);

public record BookmarkInsightRequest(
    Guid InsightId,
    Guid UserId
);

public record BookmarkInsightResponse(
    Guid InsightId,
    bool IsBookmarked,
    int TotalBookmarks
);
