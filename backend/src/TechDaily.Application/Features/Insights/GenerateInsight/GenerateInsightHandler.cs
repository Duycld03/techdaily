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
        var result = await _generator.GenerateInsightAsync(
            request.PreferredCategory,
            request.PreferredTopic,
            request.Locale,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error;
        }

        var insight = result.Value;
        await _dbContext.TechInsights.AddAsync(insight, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

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
