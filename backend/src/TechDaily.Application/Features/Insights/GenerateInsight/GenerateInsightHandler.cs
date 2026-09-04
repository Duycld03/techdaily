using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Insights.DTOs;
using TechDaily.Application.Interfaces;

namespace TechDaily.Application.Features.Insights.GenerateInsight;

public class GenerateInsightHandler : IUseCase<GenerateInsightRequest, TechInsightDto>
{
    private readonly ITechDailyDbContext _dbContext;
    private readonly ITechInsightGenerator _generator;

    public GenerateInsightHandler(
        ITechDailyDbContext dbContext,
        ITechInsightGenerator generator)
    {
        _dbContext = dbContext;
        _generator = generator;
    }

    public async Task<Result<TechInsightDto>> ExecuteAsync(
        GenerateInsightRequest request,
        CancellationToken cancellationToken = default)
    {
        // Fetch recent existing titles to prevent repetitive generation
        var existingTitles = await _dbContext.TechInsights
            .AsNoTracking()
            .Where(i => !i.IsDeleted && (request.PreferredCategory == null || i.Category == request.PreferredCategory))
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => i.Title)
            .Take(30)
            .ToListAsync(cancellationToken);

        var result = await _generator.GenerateInsightAsync(
            request.PreferredCategory,
            request.PreferredTopic,
            existingTitles,
            request.Locale,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error;
        }

        var insight = result.Value;
        var exists = await _dbContext.TechInsights
            .AsNoTracking()
            .AnyAsync(i => !i.IsDeleted && (i.Title.ToLower() == insight.Title.ToLower() || i.Slug == insight.Slug), cancellationToken);

        if (!exists)
        {
            await _dbContext.TechInsights.AddAsync(insight, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var dto = new TechInsightDto(
            insight.Id,
            insight.Slug,
            insight.Title,
            insight.Category,
            insight.Tags,
            insight.SummaryMarkdown,
            insight.ProblemSnippet,
            insight.SolutionSnippet,
            insight.UnderTheHoodMarkdown,
            insight.BenchmarkStats,
            insight.SourceUrl,
            insight.LikesCount,
            insight.BookmarksCount,
            false
        );

        return dto;
    }
}
