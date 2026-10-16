using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Categories.DTOs;
using DeepPace.Application.Features.Insights.DTOs;
using DeepPace.Application.Interfaces;

namespace DeepPace.Application.Features.Insights.GenerateInsight;

public class GenerateInsightHandler(
    IDeepPaceDbContext dbContext,
    ITechInsightGenerator generator,
    ICategoryAssignmentGuard categoryGuard) : IUseCase<GenerateInsightRequest, TechInsightDto>
{
    public async Task<Result<TechInsightDto>> ExecuteAsync(
        GenerateInsightRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await categoryGuard.ValidateAsync(request.CategoryId, cancellationToken);
        if (!validation.IsSuccess) return validation.Error;

        var existingTitles = await dbContext.TechInsights
            .AsNoTracking()
            .Where(i => !i.IsDeleted && i.IsPublished && i.CategoryId == request.CategoryId)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => i.Title)
            .Take(30)
            .ToListAsync(cancellationToken);

        var generated = await generator.GenerateInsightAsync(
            request.CategoryId, request.PreferredTopic, existingTitles, request.Locale, cancellationToken);
        if (!generated.IsSuccess) return generated.Error;
        if (!generated.Value.IsPublished)
            return Error.Custom("AiService.GenerationFailed", "No published insight could be generated.");

        return await categoryGuard.ExecuteAsync<TechInsightDto>(request.CategoryId, async category =>
        {
            var insight = generated.Value;
            var existing = await dbContext.TechInsights
                .AsNoTracking()
                .FirstOrDefaultAsync(i => !i.IsDeleted && i.IsPublished && i.CategoryId == category.Id &&
                    (i.Title.ToLower() == insight.Title.ToLower() || i.Slug == insight.Slug), cancellationToken);
            if (existing != null)
            {
                insight = existing;
            }
            else
            {
                if (await dbContext.TechInsights.AnyAsync(i => !i.IsDeleted && i.Slug == insight.Slug, cancellationToken))
                {
                    const int maxSlugLength = 255;
                    var suffix = $"-{insight.Id:N}";
                    var prefixLength = Math.Min(insight.Slug.Length, maxSlugLength - suffix.Length);
                    insight.Slug = insight.Slug[..prefixLength] + suffix;
                }
                insight.CategoryId = category.Id;
                insight.Category = category;
                await dbContext.TechInsights.AddAsync(insight, cancellationToken);
            }

            return new TechInsightDto(
                insight.Id, insight.Slug, insight.Title, category.Id, category.ToDescriptor(),
                insight.Tags, insight.SummaryMarkdown, insight.ProblemSnippet, insight.SolutionSnippet,
                insight.UnderTheHoodMarkdown, insight.BenchmarkStats, insight.SourceUrl,
                insight.BookmarksCount, false);
        }, cancellationToken);
    }
}
