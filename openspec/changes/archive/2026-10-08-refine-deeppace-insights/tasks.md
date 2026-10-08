# Tasks

## 1. Application & Backend Logic

- [x] 1.1 Update `GetInsightsMetaHandler.cs` to filter out chapter title prefix noise (e.g. `CHƯƠNG X:`, `Tóm tắt chương`, `(Section Y)`) and cap suggested topics to at most 8 items per category.
- [x] 1.2 Update category 4 display metadata in `GetInsightsMetaHandler.cs` to "Clean Code & Software Design" / "Mã Sạch & Thiết Kế Mã".
- [x] 1.3 Update backend unit tests in `TechDaily.Tests` to verify `GetInsightsMetaHandler` cleans title noise, handles empty categories, and caps topics at 8.

## 2. Infrastructure & Seed Data

- [x] 2.1 Enrich `backend/src/TechDaily.Infrastructure/Data/tech-insights.json` with production-grade seed insights for `EngineeringCraft` (Category 4), `MentalModels` (Category 5), and `HabitsProductivity` (Category 6).
- [x] 2.2 Ensure `TechInsightsSeeder.cs` successfully seeds all 7 categories without overwriting existing user data or bookmarks.

## 3. Frontend Localization & Copy

- [x] 3.1 Update `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json` with complete category names for all 7 categories (`cat_frontend`, `cat_backend`, `cat_database`, `cat_system`, `cat_craft`, `cat_mental`, `cat_habits`).
- [x] 3.2 Update page title and subtitle in `en.json` and `vi.json` to reflect DeepPace mastery and deliberate practice instead of legacy runtime tips.
- [x] 3.3 Add localized category-specific AI generator placeholders and guidance descriptions in `en.json` and `vi.json`.

## 4. Frontend Component & Layout Refinement

- [x] 4.1 Refactor AI generator modal in `frontend/pages/insights.vue` with `max-h-[85dvh]` flex containment and scrollable body (`overflow-y-auto`) to prevent vertical viewport overflow.
- [x] 4.2 Constrain the suggested topics chips container in `frontend/pages/insights.vue` with `max-h-36 overflow-y-auto`.
- [x] 4.3 Implement dynamic computed placeholder and description in `frontend/pages/insights.vue` that reacts to `insightsStore.selectedCategory`.
- [x] 4.4 Mount Lucide `<Lightbulb>` and `<BookmarkCheck>` icon components inside the empty state containers in `frontend/pages/insights.vue`.
- [x] 4.5 Refactor `getCategoryBadge` in `frontend/pages/insights.vue` to bind to localized category labels and sanitize hashtag whitespace (`#.NET` instead of `# .NET`).

## 5. Verification & Testing

- [x] 5.1 Execute backend test suite via `dotnet test` to verify metadata handler logic.
- [x] 5.2 Execute frontend test suite via `npm test` to verify page rendering, Pinia stores, and modal triggers.
- [x] 5.3 Conduct automated visual verification on Desktop (1440x900) and Mobile (390x844) viewports using headless browser to inspect modal containment and empty state icons.
