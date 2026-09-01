# Implementation Tasks: Infinite Tech Insights Feed & 30-Day Curriculum Roadmap

## Phase 1: Domain, Database & Infrastructure Setup
- [ ] 1.1 Create `TechInsight` entity in `TechDaily.Domain/Entities/TechInsight.cs`.
- [ ] 1.2 Add EF Core configuration with JSON value comparer for `Tags` in `EntityConfigurations.cs`.
- [ ] 1.3 Add `DbSet<TechInsight> TechInsights` to `TechDailyDbContext` and create EF Core migration `AddTechInsightsTable`.
- [ ] 1.4 Create `tech-insights.json` dataset containing curated senior insights across .NET, Postgres, Frontend, and Distributed Systems.
- [ ] 1.5 Create `TechInsightsSeeder` in `TechDaily.Infrastructure/Persistence/Seeders/TechInsightsSeeder.cs` to upsert seed insights.

## Phase 2: Application Layer & Use Cases
- [ ] 2.1 Implement `GetCurriculumRoadmapHandler` in `Features/Curriculum/GetCurriculumRoadmap/GetCurriculumRoadmapHandler.cs`.
- [ ] 2.2 Implement `GetInsightsFeedHandler` in `Features/Insights/GetInsightsFeed/GetInsightsFeedHandler.cs`.
- [ ] 2.3 Implement `GenerateInsightHandler` calling Gemini 3.6 Flash in `Features/Insights/GenerateInsight/GenerateInsightHandler.cs`.
- [ ] 2.4 Implement `BookmarkInsightHandler` in `Features/Insights/BookmarkInsight/BookmarkInsightHandler.cs`.
- [ ] 2.5 Register all handlers and services in `DependencyInjection.cs`.

## Phase 3: API Endpoints
- [ ] 3.1 Create `CurriculumEndpoints.cs` mapping `GET /api/v1/curriculum/roadmap`.
- [ ] 3.2 Create `InsightsEndpoints.cs` mapping `GET /api/v1/insights/feed`, `POST /api/v1/insights/generate`, `POST /api/v1/insights/{id}/bookmark`.
- [ ] 3.3 Register endpoint groups in `Program.cs`.

## Phase 4: Frontend Development
- [ ] 4.1 Update `AppSidebar.vue` adding `Roadmap` (`/roadmap`) and `Insights` (`/insights`) links.
- [ ] 4.2 Update i18n locales `en.json` and `vi.json` with roadmap and insights keys.
- [ ] 4.3 Create `useRoadmapStore.ts` and build `/roadmap` page with interactive skill tree / module sections.
- [ ] 4.4 Create `useInsightsStore.ts` and build `/insights` page with card reader, keyboard navigation, category filtering, and AI generator button.

## Phase 5: Verification & Automated Tests
- [ ] 5.1 Add unit tests for `GetCurriculumRoadmapHandler` and `GetInsightsFeedHandler`.
- [ ] 5.2 Add component tests for Roadmap and Insights views.
- [ ] 5.3 Verify all backend (`dotnet test`) and frontend (`npm test`) tests pass.
