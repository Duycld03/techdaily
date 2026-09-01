using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Insights.DTOs;
using TechDaily.Application.Interfaces;

namespace TechDaily.Application.Features.Insights.GetInsightsFeed;

public class GetInsightsFeedHandler : IUseCase<GetInsightsFeedRequest, GetInsightsFeedResponse>
{
    private readonly ITechDailyDbContext _dbContext;

    public GetInsightsFeedHandler(ITechDailyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetInsightsFeedResponse>> ExecuteAsync(
        GetInsightsFeedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TechInsights
            .Where(i => i.IsPublished);

        if (request.Category.HasValue)
        {
            query = query.Where(i => i.Category == request.Category.Value);
        }

        var insights = await query.ToListAsync(cancellationToken);
        insights = insights.OrderByDescending(i => i.CreatedAt).ToList();

        // Tag filtering in memory since Tags is serialized JSON list
        if (!string.IsNullOrWhiteSpace(request.Tag))
        {
            var normalizedTag = request.Tag.Trim().ToLowerInvariant();
            insights = insights.Where(i => i.Tags.Any(t => t.ToLowerInvariant().Contains(normalizedTag))).ToList();
        }

        var totalCount = insights.Count;

        if (request.Randomize)
        {
            var rng = new Random();
            insights = insights.OrderBy(_ => rng.Next()).ToList();
        }

        int pageSize = request.PageSize > 0 ? Math.Min(request.PageSize, 50) : 10;
        int page = Math.Max(request.Page, 1);
        var pagedItems = insights
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new TechInsightDto(
                i.Id,
                i.Slug,
                i.Title,
                i.Category,
                i.Tags,
                i.SummaryMarkdown,
                i.ProblemSnippet,
                i.SolutionSnippet,
                i.UnderTheHoodMarkdown,
                i.BenchmarkStats,
                i.SourceUrl,
                i.LikesCount,
                i.BookmarksCount,
                false
            ))
            .ToList();

        var response = new GetInsightsFeedResponse(
            pagedItems,
            totalCount,
            page,
            pageSize,
            (page * pageSize) < totalCount
        );

        return response;
    }
}
