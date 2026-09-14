# Implementation Tasks: Feature Cleanup & Optimizations

## Phase 1: Locales Consolidation & i18n Standardization
- [x] 1.1 Delete redundant `frontend/locales/` directory and standardize on `frontend/i18n/locales/`.
- [x] 1.2 Streamline Vietnamese navigation and button copy to concise terminology.
- [x] 1.3 Replace all hardcoded strings in `pages/notes.vue` and `pages/read/[bookId].vue` with i18n keys.

## Phase 2: Navigation Grouping & Mobile Parity
- [x] 2.1 Refactor `AppSidebar.vue` to render 3-tier grouped structure (`Practice`, `Knowledge`, `System`).
- [x] 2.2 Refactor `AppHeader.vue` mobile drawer to render matching 3-tier grouped navigation.

## Phase 3: Cross-Feature 1-Click Integration
- [x] 3.1 Add "Quiz This Chapter" button in `pages/read/[bookId].vue`.
- [x] 3.2 Update `pages/quiz.vue` to inspect `route.query.topic` and prefill `customTopicInput`.

## Phase 4: ERD Standardization & Dead Code Pruning
- [x] 4.1 Delete `TechDaily.Domain/Entities/AiReview.cs` and prune legacy voice columns from `DailyDrill.cs`.
- [x] 4.2 Delete `LocalAudioStorageService.cs` and `AiReviewDto.cs`.
- [x] 4.3 Remove `IAiReviewService` & `IAudioStorageService` from `IServiceInterfaces.cs`, `GeminiAiService.cs`, `DependencyInjection.cs`.
- [x] 4.4 Remove `DbSet<AiReview>` from `TechDailyDbContext.cs`, `ITechDailyDbContext.cs`, and `EntityConfigurations.cs`.
- [x] 4.5 Scaffold EF Core migration `RemoveAiReviewsAndPruneDailyDrill`.
- [x] 4.6 Update `docs/database-design.md` with standardized Mermaid ERD.

## Phase 5: Verification & Testing
- [x] 5.1 Run `dotnet test backend/TechDaily.sln` (41/41 passed).
- [x] 5.2 Run `npm run build` in `frontend` (0 errors).
