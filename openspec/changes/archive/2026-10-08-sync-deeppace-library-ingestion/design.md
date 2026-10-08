# Design

## Context

TechDaily was recently pivoted to DeepPace ("Deep Learning at a Sustainable Daily Pace"), elevating personal deliberate practice, cognitive mental models, and deep work habits alongside software engineering craftsmanship. The backend domain already defines all 7 enum values in `TechDaily.Domain.Enums.Category`:
- `FrontendWeb = 0`
- `BackendRuntime = 1`
- `DatabaseStorage = 2`
- `SystemDesign = 3`
- `EngineeringCraft = 4`
- `MentalModels = 5`
- `HabitsProductivity = 6`

However, the Library catalog and document ingestion system (`pages/library.vue`, `WebArticleCrawler.cs`, and associated localization files) were left with legacy 5-category arrays and developer-only microcopy. Users importing books or articles on mindset, habits, psychology, or personal mastery cannot select appropriate categories and are confronted with developer jargon ("Cào dữ liệu", "README.md", "Designing Data-Intensive Applications").

## Goals / Non-Goals

**Goals:**
- Expose all 7 DeepPace categories across the Library page category filter and all 3 modal ingestion tabs (Markdown, PDF, Web URL).
- De-jargonize and modernize microcopy in English (`en-US`) and Vietnamese (`vi-VN`) across all 3 ingestion tabs.
- Update frontend category resolution (`getCategoryLabel`) and heuristic context inference (`inferCategoryFromContext`).
- Update backend `WebArticleCrawler.InferCategoryFromContext` to recognize keywords for Mental Models and Habits & Deep Work.
- Maintain layout stability (zero font-weight shift, clean wrapping) on the category filter bar across mobile and desktop viewports.
- Maintain 100% pass rate in frontend Vitest suites and backend xUnit tests.

**Non-Goals:**
- No database migrations: Enum values `5` and `6` are already supported by the database column and EF Core mappings.
- No modifications to the PDF extraction engine (Docnet / PDF streaming pipeline) or slice curation background worker.

## Decisions

### 1. Unified 7-Category Taxonomy Structure

In `frontend/pages/library.vue`:
```typescript
const categories = computed(() => [
  { id: undefined, label: t('library.categories.all') },
  { id: 5, label: t('library.categories.mental_models') },
  { id: 6, label: t('library.categories.habits') },
  { id: 4, label: t('library.categories.craft') },
  { id: 3, label: t('library.categories.system_design') },
  { id: 1, label: t('library.categories.backend') },
  { id: 0, label: t('library.categories.frontend') },
  { id: 2, label: t('library.categories.database') }
])

const formCategoryOptions = computed(() => [
  { value: 5, label: t('library.categories.mental_models') },
  { value: 6, label: t('library.categories.habits') },
  { value: 4, label: t('library.categories.craft') },
  { value: 3, label: t('library.categories.system_design') },
  { value: 1, label: t('library.categories.backend') },
  { value: 0, label: t('library.categories.frontend') },
  { value: 2, label: t('library.categories.database') }
])
```
*Rationale*: Ordering the Deliberate Practice categories (`MentalModels`, `HabitsProductivity`, `EngineeringCraft`) prominently establishes DeepPace's identity while preserving instant access to all core technical domains.

### 2. Localization Dictionary Alignment (`en.json` & `vi.json`)

Update `library.categories` and related ingestion keys:
- `categories.mental_models`: "Mô Hình Tư Duy & Quyết Định" / "Mental Models & Decisions"
- `categories.habits`: "Thói Quen & Tập Trung Sâu" / "Habits & Deep Work"
- `url_crawler_title`: "Trích Xuất Bài Viết Từ Web" / "Extract Article from Web"
- `fetch_url_btn`: "Lấy Nội Dung" / "Fetch Content"
- `fetching_url`: "Đang trích xuất nội dung..." / "Extracting article content..."
- `url_input_placeholder`: "https://... (Substack, Medium, Farnam Street, blog cá nhân hoặc tài liệu)" / "https://... (Substack, Medium, Farnam Street, blogs, or documentation)"
- `verbatim_category_hint`: "Mẹo: Chọn chuyên mục Thiết Kế Mã, Mô Hình Tư Duy hoặc Thói Quen để giữ nguyên văn 100% nội dung gốc (AI khôi phục tiêu đề & ngắt dòng)." / "Tip: Select Software Design, Mental Models, or Habits to preserve 100% verbatim text (AI restores headings & paragraphs)."
- `title_placeholder` (Markdown): "Ví dụ: Atomic Habits — Chương 1 hoặc Designing Data-Intensive Applications — Chương 5" / "e.g. Atomic Habits — Chapter 1 or Designing Data-Intensive Applications — Chapter 5"

### 3. Heuristic Keyword Inference Precedence

Both `WebArticleCrawler.cs` and `library.vue` need synchronized keyword matching. We prioritize Mindset & Deliberate Practice keywords first:
```
1. HabitsProductivity: atomic habits, deep work, habit, focus, flow state, procrastination, james clear, cal newport, thói quen, tập trung, năng suất
2. MentalModels: mental model, first principles, cognitive bias, decision making, psychology, stoic, farnam street, charlie munger, mô hình tư duy, tư duy, ra quyết định
3. EngineeringCraft: clean code, refactor, architecture, design pattern, solid, unit test
4. SystemDesign: distributed, microservice, system design, kafka, consensus, raft
5. DatabaseStorage: postgres, sql, database, index, redis
6. BackendRuntime: dotnet, csharp, golang, rust, python, java, runtime, backend
7. FrontendWeb: vue, react, frontend, css, html, browser, javascript, typescript
```

## Risks / Trade-offs

- **Filter Bar Wrapping on Narrow Viewports**: With 8 buttons, the category filter row wraps across 2-3 lines on mobile (390px). 
  *Mitigation*: The existing layout uses `flex flex-wrap gap-2` with `py-1.5 px-3` pill badges and `whitespace-nowrap`, which wraps ergonomically into a cohesive filter chip cloud without horizontal clipping.
- **Vitest Mock Assertion Breakage**: Existing tests in `tests/pages/library.spec.ts` assert category presence.
  *Mitigation*: Update unit test suites to assert all 7 categories and verify form payload serialization.
