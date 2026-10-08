# Tasks

## 1. Infrastructure Component

- [x] 1.1 Update `WebArticleCrawler.InferCategoryFromContext` in `backend/src/TechDaily.Infrastructure/Services/WebArticleCrawler.cs` to add keyword inference heuristics for `Category.MentalModels` and `Category.HabitsProductivity`.
- [x] 1.2 Update unit tests in `tests/TechDaily.Tests/Infrastructure/WebArticleCrawlerTests.cs` to verify category inference for mental models and habits keywords.

## 2. Frontend Component

- [x] 2.1 Update `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json` with keys `categories.mental_models` and `categories.habits`, de-jargonized crawler titles and buttons, universal URL placeholders, and generalized verbatim helper hints.
- [x] 2.2 Update `frontend/pages/library.vue` to include all 7 categories in `categories` (filter pills), `formCategoryOptions` (modal select across all 3 tabs), `getCategoryLabel`, and `inferCategoryFromContext`.
- [x] 2.3 Update `frontend/pages/library.vue` Markdown tab title placeholder to reflect DeepPace literature (*Atomic Habits* / deliberate practice).
- [x] 2.4 Update unit test assertions in `frontend/tests/pages/library.spec.ts` to cover all 7 categories in the catalog and import dialog.

## 3. Verification & Testing

- [x] 3.1 Execute backend unit tests via `dotnet test` to verify `WebArticleCrawlerTests` and library handlers pass.
- [x] 3.2 Execute frontend test suite via `npm test` to verify `library.spec.ts` passes with 100% assertions.
- [x] 3.3 Execute headless browser automated visual verification for Desktop (1440x900) and Mobile (390x844) viewports on `/library` and the 3-tab import modal.
