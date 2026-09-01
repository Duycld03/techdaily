# Technical Design: Infinite Tech Insights Feed & 30-Day Curriculum Roadmap

## Architecture Overview

```
Frontend (Nuxt 4 / Vue 3)
  ├── Pages: /roadmap, /insights
  ├── Components: RoadmapTree, ModuleSection, InsightCard, InsightActionToolbar
  └── Pinia Stores: useRoadmapStore, useInsightsStore
         │
         ▼
API Layer (.NET 10 Minimal APIs)
  ├── MapCurriculumEndpoints -> GET /api/v1/curriculum/roadmap
  └── MapInsightsEndpoints   -> GET /api/v1/insights/feed, POST /api/v1/insights/generate, POST /api/v1/insights/{id}/bookmark
         │
         ▼
Application Layer (Use Cases)
  ├── GetCurriculumRoadmapHandler
  ├── GetInsightsFeedHandler
  └── GenerateInsightHandler (Calls Gemini 3.6 Flash)
         │
         ▼
Infrastructure & Database (PostgreSQL 17 / EF Core)
  ├── Entity: TechInsight (Mapped with JSONB tags)
  ├── Seeder: TechInsightsSeeder (Loads built-in catalog from tech-insights.json)
  └── Services: GeminiInsightGeneratorService
```

---

## Data Models

### 1. `TechInsight` Entity
```csharp
public class TechInsight : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public Category Category { get; set; }
    public List<string> Tags { get; set; } = new();
    public string SummaryMarkdown { get; set; } = string.Empty;
    public string ProblemSnippet { get; set; } = string.Empty;
    public string SolutionSnippet { get; set; } = string.Empty;
    public string UnderTheHoodMarkdown { get; set; } = string.Empty;
    public string BenchmarkStats { get; set; } = string.Empty;
    public string? SourceUrl { get; set; }
    public int LikesCount { get; set; } = 0;
    public int BookmarksCount { get; set; } = 0;
}
```

### 2. Roadmap Response DTO
```csharp
public class CurriculumRoadmapResponse
{
    public int TotalDays { get; set; } = 30;
    public int CompletedDaysCount { get; set; }
    public int CurrentActiveDay { get; set; }
    public List<CurriculumModuleDto> Modules { get; set; } = new();
}

public class CurriculumModuleDto
{
    public Category Category { get; set; }
    public string ModuleTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<RoadmapDayNodeDto> Days { get; set; } = new();
}

public class RoadmapDayNodeDto
{
    public int DayOrder { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public Difficulty Difficulty { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsActiveToday { get; set; }
    public int? DrillScore { get; set; }
}
```

---

## Gemini 3.6 Flash Insight Synthesizer Prompting

When `POST /api/v1/insights/generate` is called, the backend queries Gemini with a structured system instruction:
- Persona: Principal Software Architect & Compiler/Runtime Specialist.
- Strict JSON output format matching `TechInsight` schema.
- Emphasis on concrete low-level mechanics: memory layouts, cache lines, assembly/IL lowering, WAL buffers, or VDOM diffing.
