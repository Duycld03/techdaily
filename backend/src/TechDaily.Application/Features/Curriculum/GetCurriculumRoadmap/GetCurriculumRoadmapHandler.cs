using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Curriculum.DTOs;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Enums;

namespace TechDaily.Application.Features.Curriculum.GetCurriculumRoadmap;

public record GetCurriculumRoadmapRequest(Guid? UserId = null);

public class GetCurriculumRoadmapHandler : IUseCase<GetCurriculumRoadmapRequest, CurriculumRoadmapResponse>
{
    private readonly ITechDailyDbContext _dbContext;

    public GetCurriculumRoadmapHandler(ITechDailyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CurriculumRoadmapResponse>> ExecuteAsync(
        GetCurriculumRoadmapRequest request,
        CancellationToken cancellationToken = default)
    {
        var topics = await _dbContext.Topics
            .OrderBy(t => t.DayOrder)
            .ToListAsync(cancellationToken);

        int currentActiveDay = 1;
        var completedTopicIds = new Dictionary<Guid, int>(); // TopicId -> Score

        if (request.UserId.HasValue && request.UserId.Value != Guid.Empty)
        {
            var userId = request.UserId.Value;
            var streak = await _dbContext.StreakRecords
                .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

            if (streak != null)
            {
                currentActiveDay = (streak.TotalDrillsCompleted % 30) + 1;
            }

            var reviewedDrills = await _dbContext.DailyDrills
                .Include(d => d.Question)
                .Where(d => d.UserId == userId && d.Status == DrillStatus.Reviewed)
                .ToListAsync(cancellationToken);

            foreach (var drill in reviewedDrills)
            {
                var score = drill.Score ?? (drill.IsCorrect == true ? 10 : 0);
                completedTopicIds[drill.Question.TopicId] = score;
            }
        }

        var moduleDefinitions = new List<(Category Category, string Title, string Description, int StartDay, int EndDay)>
        {
            (Category.FrontendWeb, "Frontend & Browser Internals", "Vue 3 Reactivity, Rendering Strategies, Browser Rendering Pipeline, Web Vitals, State Management, WebSockets & Modern Bundlers.", 1, 7),
            (Category.BackendDotNet, ".NET 10 & C# 13 Internals", "Generational GC & LOH, Span/Memory zero-allocation, Async State Machine, Lock-Free Concurrency, Channels, DI Lifetimes, and Kestrel Pipelines.", 8, 15),
            (Category.DatabaseStorage, "PostgreSQL 17 Storage Engine", "MVCC & WAL, Isolation Levels & SSI, B-Tree/GIN Indexing, EXPLAIN ANALYZE, PgBouncer, Partitioning, and pgvector HNSW.", 16, 22),
            (Category.SystemDesign, "System Design & Distributed Patterns", "Distributed Caching, Transactional Outbox, Idempotency, Rate Limiting, Polly v8 Resilience, OpenTelemetry, and Zero-Trust OAuth 2.0 PKCE.", 23, 30)
        };

        var modules = new List<CurriculumModuleDto>();
        int totalCompletedDays = 0;

        foreach (var def in moduleDefinitions)
        {
            var moduleTopics = topics
                .Where(t => t.DayOrder >= def.StartDay && t.DayOrder <= def.EndDay)
                .OrderBy(t => t.DayOrder)
                .ToList();

            int moduleCompleted = 0;
            var dayNodes = new List<RoadmapDayNodeDto>();

            foreach (var topic in moduleTopics)
            {
                bool isCompleted = completedTopicIds.ContainsKey(topic.Id);
                int? score = isCompleted ? completedTopicIds[topic.Id] : null;
                bool isActiveToday = topic.DayOrder == currentActiveDay;
                bool isUnlocked = isCompleted || isActiveToday || topic.DayOrder <= currentActiveDay;

                if (isCompleted)
                {
                    moduleCompleted++;
                    totalCompletedDays++;
                }

                dayNodes.Add(new RoadmapDayNodeDto
                {
                    DayOrder = topic.DayOrder,
                    Slug = topic.Slug,
                    Title = topic.Title,
                    Summary = topic.Summary,
                    Difficulty = topic.Difficulty,
                    IsCompleted = isCompleted,
                    IsActiveToday = isActiveToday,
                    IsUnlocked = isUnlocked,
                    DrillScore = score
                });
            }

            modules.Add(new CurriculumModuleDto
            {
                Category = def.Category,
                ModuleTitle = def.Title,
                Description = def.Description,
                StartDay = def.StartDay,
                EndDay = def.EndDay,
                CompletedCount = moduleCompleted,
                TotalCount = moduleTopics.Count,
                Days = dayNodes
            });
        }

        var progressPercentage = Math.Round((decimal)totalCompletedDays / 30 * 100, 1);

        return new CurriculumRoadmapResponse
        {
            TotalDays = 30,
            CompletedDaysCount = totalCompletedDays,
            CurrentActiveDay = currentActiveDay,
            OverallProgressPercentage = progressPercentage,
            Modules = modules
        };
    }
}
