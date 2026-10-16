using DeepPace.Application.Features.Admin.Categories.DTOs;

using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Insights.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Insights.GetInsightsMeta;

public class GetInsightsMetaHandler : IUseCase<GetInsightsMetaRequest, GetInsightsMetaResponse>
{
    private readonly IDeepPaceDbContext _dbContext;


    private static readonly Regex NoisePrefixRegex = new(
        @"^(chương\s+\d+[:\-\s]*|chapter\s+\d+[:\-\s]*|tóm\s+tắt\s+chương[:\-\s]*|\(section\s+\d+\)[:\-\s]*|qui\s+luật\s+số\s+\d+[:\-\s]*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string CleanTopicTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return string.Empty;
        var cleaned = NoisePrefixRegex.Replace(title.Trim(), string.Empty).Trim();
        cleaned = Regex.Replace(cleaned, @"\s*\(section\s+\d+\)\s*$", string.Empty, RegexOptions.IgnoreCase).Trim();
        return cleaned.Length >= 3 ? cleaned : string.Empty;
    }

    public GetInsightsMetaHandler(IDeepPaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetInsightsMetaResponse>> ExecuteAsync(
        GetInsightsMetaRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.UserId == Guid.Empty)
            return Error.Custom("Unauthorized", "An authenticated user is required.");

        var catalog = await _dbContext.DocumentCategories
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Slug)
            .ToListAsync(cancellationToken);
        var insightCounts = await _dbContext.TechInsights
            .AsNoTracking()
            .Where(i => i.IsPublished && !i.IsDeleted)
            .GroupBy(i => i.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.CategoryId, g => g.Count, cancellationToken);
        var books = await _dbContext.DocumentBooks
            .AsNoTracking()
            .Where(b => !b.IsDeleted && b.CreatedByUserId == request.UserId && b.Status == ProcessingStatus.Ready)
            .OrderBy(b => b.Title)
            .ThenBy(b => b.Id)
            .Select(b => new { b.Id, b.CategoryId, b.Title })
            .ToListAsync(cancellationToken);
        var bookIds = books.Select(b => b.Id).ToList();
        var chapters = await _dbContext.DocumentChunks
            .AsNoTracking()
            .Where(c => !c.IsDeleted && bookIds.Contains(c.DocumentBookId))
            .OrderBy(c => c.DocumentBookId)
            .ThenBy(c => c.ChunkOrder)
            .Select(c => new { c.DocumentBook.CategoryId, Title = c.ChapterTitle })
            .ToListAsync(cancellationToken);
        var topics = books.Select(b => (b.CategoryId, b.Title))
            .Concat(chapters.Select(c => (c.CategoryId, c.Title)))
            .GroupBy(t => t.CategoryId)
            .ToDictionary(g => g.Key, g => g.Select(t => CleanTopicTitle(t.Title))
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToList());
        return new GetInsightsMetaResponse(
            catalog.Select(c => new InsightCategoryMetaDto(c.Id, c.ToDescriptor(), insightCounts.GetValueOrDefault(c.Id))).ToList(),
            catalog.ToDictionary(c => c.Id, c => topics.GetValueOrDefault(c.Id, [])));
    }
}
