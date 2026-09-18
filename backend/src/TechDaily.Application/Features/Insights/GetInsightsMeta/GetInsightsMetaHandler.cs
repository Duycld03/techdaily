using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Insights.DTOs;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Enums;

namespace TechDaily.Application.Features.Insights.GetInsightsMeta;

public class GetInsightsMetaHandler : IUseCase<GetInsightsMetaRequest, GetInsightsMetaResponse>
{
    private readonly ITechDailyDbContext _dbContext;

    private static readonly Dictionary<int, List<string>> DefaultTopics = new()
    {
        [0] = new()
        {
            "Vue 3 shallowRef vs reactive",
            "Component Composition vs Re-renders",
            "Web Workers Offloading",
            "Event Loop & Microtasks"
        },
        [1] = new()
        {
            "Kestrel Socket Pipeline",
            "ArrayPool<T> Memory Pooling",
            "System.Threading.Channels",
            "OutputCache Tag Eviction"
        },
        [2] = new()
        {
            "PostgreSQL Index-Only Scan & INCLUDE",
            "Heap-Only Tuples (HOT)",
            "GIN Index for JSONB",
            "PgBouncer Connection Pooling"
        },
        [3] = new()
        {
            "Transactional Outbox & CDC",
            "Cache Stampede & XFetch",
            "Token Bucket Rate Limiting",
            "Circuit Breaker with Jitter"
        }
    };

    public GetInsightsMetaHandler(ITechDailyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetInsightsMetaResponse>> ExecuteAsync(
        GetInsightsMetaRequest request,
        CancellationToken cancellationToken = default)
    {
        var insightCounts = await _dbContext.TechInsights
            .AsNoTracking()
            .Where(i => i.IsPublished)
            .GroupBy(i => i.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var countMap = insightCounts.ToDictionary(g => g.Category, g => g.Count);

        var categories = new List<InsightCategoryMetaDto>
        {
            new(
                (int)Category.FrontendWeb,
                "frontend",
                "Frontend & Web Architecture",
                "Frontend & Trình Duyệt",
                countMap.GetValueOrDefault(Category.FrontendWeb, 0)
            ),
            new(
                (int)Category.BackendRuntime,
                "backend",
                "Backend & Runtime Systems",
                "Hệ Thống Backend & Runtime",
                countMap.GetValueOrDefault(Category.BackendRuntime, 0)
            ),
            new(
                (int)Category.DatabaseStorage,
                "database",
                "Database & Storage",
                "Cơ Sở Dữ Liệu & Lưu Trữ",
                countMap.GetValueOrDefault(Category.DatabaseStorage, 0)
            ),
            new(
                (int)Category.SystemDesign,
                "system_design",
                "Distributed Systems & Architecture",
                "Thiết Kế Hệ Thống",
                countMap.GetValueOrDefault(Category.SystemDesign, 0)
            )
        };

        var activeTopics = await _dbContext.Topics
            .AsNoTracking()
            .Where(t => !t.IsDeleted)
            .Select(t => new { t.Category, t.Title })
            .ToListAsync(cancellationToken);

        var dbTopicGroups = activeTopics
            .GroupBy(t => (int)t.Category)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Title.Trim()).Where(t => !string.IsNullOrEmpty(t)).Distinct().ToList()
            );

        var suggestedTopics = new Dictionary<int, List<string>>();

        foreach (var categoryId in new[] { 0, 1, 2, 3 })
        {
            var topicsForCat = dbTopicGroups.TryGetValue(categoryId, out var dbTopics)
                ? dbTopics
                : new List<string>();

            if (topicsForCat.Count < 2)
            {
                var defaults = DefaultTopics.GetValueOrDefault(categoryId, new List<string>());
                topicsForCat = topicsForCat.Union(defaults).ToList();
            }

            suggestedTopics[categoryId] = topicsForCat;
        }

        return new GetInsightsMetaResponse(categories, suggestedTopics);
    }
}
