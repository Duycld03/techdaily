# Tasks: AI-Powered Technical Interview Quiz & Mastery Arena

## Phase 1: Domain & Database Foundation
- [x] 1.1 Create `QuizLevel` enum and domain entities `QuizQuestion`, `UserQuizProgress` in `TechDaily.Domain/Entities/`
- [x] 1.2 Configure EF Core entity mapping and indexes in `TechDaily.Infrastructure/Persistence/Configurations/`
- [x] 1.3 Add `DbSet<QuizQuestion>` and `DbSet<UserQuizProgress>` to `TechDailyDbContext` and `ITechDailyDbContext`
- [x] 1.4 Create and apply EF Core database migration `AddInterviewQuizTables`

## Phase 2: AI Generation & Application Use Cases
- [x] 2.1 Define `IQuizGeneratorService` and implement Gemini 3.6 Flash structured quiz generation with JSON parsing and fallback in `TechDaily.Infrastructure/Services/`
- [x] 2.2 Implement `GenerateQuizHandler` (loads unmastered questions, prompts Gemini for remainder, deduplicates titles, persists questions)
- [x] 2.3 Implement `SubmitQuizAnswerHandler` (evaluates option, idempotent upsert to `UserQuizProgress`, updates mastery status)
- [x] 2.4 Implement `GetQuizReviewQueueHandler` and `GetQuizStatsHandler`
- [x] 2.5 Register use cases and services in DI container (`DependencyInjection.cs`)

## Phase 3: API Layer & Security
- [x] 3.1 Create `QuizEndpoints.cs` with `.RequireAuthorization()` mapped to `/api/v1/quiz`
- [x] 3.2 Wire endpoints in `Program.cs` and verify OpenAPI/Swagger documentation

## Phase 4: Frontend UI & Store Implementation
- [x] 4.1 Create Pinia store `useInterviewQuizStore.ts` with generation, submission, review queue, and stats management
- [x] 4.2 Add navigation link `nav.quiz` (`/quiz`) to `components/layout/AppSidebar.vue`
- [x] 4.3 Create `pages/quiz.vue` with Topic Selector, Level Picker, Interactive Question Deck, Review Mode, and Stats Overview
- [x] 4.4 Add bilingual localization strings to `frontend/locales/en.json` and `frontend/locales/vi.json`

## Phase 5: Verification & Automated Testing
- [ ] 5.1 Write unit tests for `GenerateQuizHandler`, `SubmitQuizAnswerHandler`, `GetQuizReviewQueueHandler`
- [ ] 5.2 Write Vitest component & store tests in `frontend/tests/`
- [ ] 5.3 Run full test suites (`dotnet test`, `npm test`) and verify production build (`npm run build`)
