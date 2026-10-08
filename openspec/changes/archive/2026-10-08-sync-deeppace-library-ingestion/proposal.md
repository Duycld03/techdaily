# Proposal

## Why

Following the platform pivot to DeepPace (uniting Deliberate Practice, Mental Models, Habits & Deep Work, and Deep Technical Craftsmanship), the `/library` workspace and its Document Ingestion Modal remain trapped in the legacy, developer-only paradigm. The Category selector restricts users to 5 software engineering specialties (omitting Mental Models and Habits & Deep Work), while input microcopy and placeholders rely heavily on developer jargon ("Cào dữ liệu", GitHub README URLs, DDIA book chapters). This creates cognitive friction and misrepresents the platform for learners importing literature on mindset, habits, decision-making, and personal mastery.

## What Changes

- **Synchronize 7 DeepPace Categories Across Library Workspace**:
  - Expose all 7 DeepPace categories across the `/library` filter bar and all 3 ingestion tabs (Markdown, PDF, Web URL): `FrontendWeb (0)`, `BackendRuntime (1)`, `DatabaseStorage (2)`, `SystemDesign (3)`, `EngineeringCraft (4)`, `MentalModels (5)`, `HabitsProductivity (6)`.
  - Update frontend category options, lookup labels (`getCategoryLabel`), and AI inference heuristics (`inferCategoryFromContext`).
- **De-Jargonize & Universalize Ingestion Microcopy (Bilingual `en-US` and `vi-VN`)**:
  - Replace technical scraping terminology ("Cào Dữ Liệu", "Cào & Chuyển Đổi") with natural extraction vocabulary ("Lấy Nội Dung", "Trích Xuất Bài Viết Từ Web" / "Fetch Content", "Extract Article from Web").
  - Update URL crawler placeholders and examples from dev-only repositories (`github.com/.../README.md`) to diverse lifelong learning and deep-thinking publications (Substack, Medium, Farnam Street, personal blogs, documentation).
  - Update Markdown tab placeholders from DDIA book chapters to universal craft & mindset examples (*Atomic Habits*, *Thinking, Fast and Slow*, or system design classics).
  - Generalize the verbatim text preservation callout (`verbatim_category_hint`) to encompass both Engineering Craft and Mindset/Habits literature (preserving author voice and original structure).
- **Backend Crawler Keyword Inference Alignment**:
  - Update `WebArticleCrawler.InferCategoryFromContext` to recognize keywords related to mental models, habits, productivity, focus, and psychology alongside existing engineering keywords.

## Capabilities

### Modified Capabilities
- `library`: Expand document ingestion categories to include Mental Models and Habits & Deep Work across Markdown, PDF, and Web URL tabs, update category filtering, and modernize ingestion microcopy.

## Impact

- **Frontend**: `frontend/pages/library.vue`, `frontend/i18n/locales/en.json`, `frontend/i18n/locales/vi.json`, `frontend/tests/pages/library.spec.ts`.
- **Backend**: `backend/src/TechDaily.Infrastructure/Services/WebArticleCrawler.cs`, crawler unit tests in `tests/TechDaily.Tests/Infrastructure/WebArticleCrawlerTests.cs`.
- **Database / Compatibility**: Zero breaking changes. Integer enum values `5` (`MentalModels`) and `6` (`HabitsProductivity`) already exist in `TechDaily.Domain.Enums.Category` and the PostgreSQL schema.
