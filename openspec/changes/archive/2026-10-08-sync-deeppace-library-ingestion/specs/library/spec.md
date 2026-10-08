# Spec Delta

## MODIFIED Requirements

### Requirement: 3-Tab Import Modal Interface
The Import Modal on `/library` SHALL provide 3 selectable tabs: Markdown Series, PDF Upload with drag-and-drop zone and upload progress, and Web Article URL Ingestion with content preview before ingestion confirmation.

Under the Category selector in all 3 modal tabs (Markdown Series, PDF Upload, and Web URL Ingestion), the modal SHALL render the full 7-category DeepPace taxonomy:
1. `FrontendWeb (0)`: "Frontend & Web" ("Frontend & Trình Duyệt")
2. `BackendRuntime (1)`: "Backend & Runtime" ("Backend & Phân Tán / Runtime")
3. `DatabaseStorage (2)`: "Database & Storage" ("Cơ Sở Dữ Liệu & Lưu Trữ")
4. `SystemDesign (3)`: "Distributed Systems" ("Thiết Kế Hệ Thống")
5. `EngineeringCraft (4)`: "Clean Code & Software Design" ("Mã Sạch & Thiết Kế Phần Mềm")
6. `MentalModels (5)`: "Mental Models & Decisions" ("Mô Hình Tư Duy & Quyết Định")
7. `HabitsProductivity (6)`: "Habits & Deep Work" ("Thói Quen & Tập Trung Sâu")

Under the Category selector in all 3 modal tabs, the modal SHALL render a dedicated in-context verbatim category helper callout banner:
1. **Notice Content & Guidance:**
   - Explains that selecting categories with verbatim text requirements (Category 4 "Clean Code & Software Design", Category 5 "Mental Models & Decisions", or Category 6 "Habits & Deep Work") preserves 100% of the book's or article's original text while AI automatically restores run-in headings and paragraph boundaries.
   - **Vietnamese (`vi`):** `"Mẹo: Chọn chuyên mục Thiết Kế Mã, Mô Hình Tư Duy hoặc Thói Quen để giữ nguyên văn 100% nội dung gốc (AI khôi phục tiêu đề & đoạn văn)."`
   - **English (`en`):** `"Tip: Select Software Design, Mental Models, or Habits to preserve 100% verbatim text (AI restores headings & paragraphs)."`
2. **De-Jargonized Web Article Ingestion (Tab 3):**
   - Title SHALL read "Extract Article from Web" / "Trích Xuất Bài Viết Từ Web" (replacing legacy "Cào & Chuyển Đổi").
   - Action button SHALL read "Fetch Content" / "Lấy Nội Dung" (replacing legacy "Cào Dữ Liệu").
   - URL input placeholder SHALL provide broad lifelong learning and deliberate practice examples: `"https://... (Substack, Medium, Farnam Street, blog cá nhân hoặc tài liệu)"` / `"https://... (Substack, Medium, Farnam Street, blogs, or documentation)"`.
3. **Universal Markdown & Notes Placeholder (Tab 1):**
   - Title placeholder SHALL provide diverse examples encompassing both technical and deliberate practice books: `"Ví dụ: Atomic Habits — Chương 1 hoặc Designing Data-Intensive Applications — Chương 5"`.
   - Content placeholder SHALL invite pasting notes, excerpts, or markdown text naturally.

#### Scenario: User selects DeepPace category in any import tab
- **GIVEN** an authenticated user opens the document import modal on `/library`
- **WHEN** user opens the Category dropdown on the Markdown, PDF, or Web URL tab
- **THEN** the dropdown contains all 7 DeepPace categories including "Mô Hình Tư Duy & Quyết Định" and "Thói Quen & Tập Trung Sâu"
- **AND** selecting either category sets the corresponding form payload value to `5` or `6`.

#### Scenario: User inputs article URL with universal web placeholder
- **GIVEN** user views the Web URL ingestion tab in the import modal
- **WHEN** the tab is rendered
- **THEN** the input displays a friendly placeholder referencing Substack, Medium, Farnam Street, blogs, and documentation
- **AND** the action button displays "Lấy Nội Dung" (Vietnamese) or "Fetch Content" (English) without scraping jargon.

#### Scenario: Verbatim hint dynamically guides mindset and habits curation
- **GIVEN** user views the Category selector in any of the 3 import tabs
- **WHEN** viewing the callout banner
- **THEN** the callout guides users that selecting Software Design, Mental Models, or Habits preserves 100% verbatim text.

---

### Requirement: Category Filter Bar Layout Stability
The `/library` catalog page SHALL provide responsive category filter pills to filter documents across all 7 DeepPace categories plus the "All" pill:
1. `All`: "Tất Cả" / "All"
2. `MentalModels (5)`: "Mô Hình Tư Duy" / "Mental Models"
3. `HabitsProductivity (6)`: "Thói Quen & Tập Trung" / "Habits & Focus"
4. `EngineeringCraft (4)`: "Thiết Kế Mã" / "Software Design"
5. `SystemDesign (3)`: "Hệ Thống Phân Tán" / "System Design"
6. `BackendRuntime (1)`: "Backend & Runtime" / "Backend & Runtime"
7. `FrontendWeb (0)`: "Frontend & Web" / "Frontend & Web"
8. `DatabaseStorage (2)`: "Cơ Sở Dữ Liệu" / "Database & Storage"

Filter pills SHALL maintain `font-semibold` across active and inactive states with `transition-colors duration-150` to guarantee zero layout shift when switching categories.

#### Scenario: User filters library by Mental Models category
- **WHEN** user clicks the "Mô Hình Tư Duy" filter button on `/library`
- **THEN** the book catalog updates to display only books and articles categorized under `MentalModels (5)`
- **AND** the filter button transitions active styling with zero horizontal layout shift or width jumping.

#### Scenario: User filters library by Habits & Deep Work category
- **WHEN** user clicks the "Thói Quen & Tập Trung" filter button on `/library`
- **THEN** the book catalog updates to display only books and articles categorized under `HabitsProductivity (6)`.

---

### Requirement: Book Card Category Badge and Ingestion Status Localization
The library catalog on `/library` SHALL display localized category badges on each document card supporting all 7 DeepPace categories:
- Value `0` / `frontend`: resolved to `$t('library.categories.frontend')`
- Value `1` / `backend`: resolved to `$t('library.categories.backend')`
- Value `2` / `database`: resolved to `$t('library.categories.database')`
- Value `3` / `system_design`: resolved to `$t('library.categories.system_design')`
- Value `4` / `craft`: resolved to `$t('library.categories.craft')`
- Value `5` / `mental_models`: resolved to `$t('library.categories.mental_models')`
- Value `6` / `habits`: resolved to `$t('library.categories.habits')`

#### Scenario: Document card displays badge for Mental Models book
- **WHEN** a document card in the library represents a book with Category `5` (Mental Models)
- **THEN** the card renders a badge with text "Mô Hình Tư Duy & Quyết Định" in Vietnamese and "Mental Models & Decisions" in English.

---

## ADDED Requirements

### Requirement: DeepPace Category Keyword Inference
The system SHALL infer the most appropriate DeepPace category when crawling web documentation or importing unstructured documents based on title, URL, and excerpt content:
1. `MentalModels (5)`: Keywords matching `mental model`, `first principles`, `cognitive bias`, `decision making`, `psychology`, `stoic`, `charlie munger`, `farnam street`, `tư duy`, `mô hình tư duy`, `tâm lý học`, `ra quyết định`.
2. `HabitsProductivity (6)`: Keywords matching `atomic habits`, `deep work`, `productivity`, `focus`, `flow state`, `habit`, `procrastination`, `cal newport`, `james clear`, `thói quen`, `tập trung`, `năng suất`, `trì hoãn`.
3. `EngineeringCraft (4)`: Keywords matching `clean code`, `refactor`, `design pattern`, `craftsmanship`, `solid`, `unit test`, `architecture`.
4. `SystemDesign (3)`: Keywords matching `distributed`, `system design`, `microservice`, `scalability`, `kafka`, `event-driven`, `consensus`, `raft`.
5. `DatabaseStorage (2)`: Keywords matching `database`, `postgres`, `sql`, `indexing`, `query optimization`, `redis`, `storage engine`.
6. `BackendRuntime (1)`: Keywords matching `aspnet`, `dotnet`, `csharp`, `golang`, `rust`, `java`, `runtime`, `concurrency`, `backend`.
7. `FrontendWeb (0)`: Keywords matching `vue`, `react`, `frontend`, `css`, `html`, `browser`, `javascript`, `typescript`.

#### Scenario: Crawler ingests article from Farnam Street or psychology source
- **WHEN** a web article title contains "Mental Models: The Best Way to Make Intelligent Decisions"
- **THEN** `InferCategoryFromContext` returns `Category.MentalModels (5)`.

#### Scenario: Crawler ingests article on focus and habits
- **WHEN** a web article title contains "Deep Work: Rules for Focused Success" or URL contains "atomic-habits"
- **THEN** `InferCategoryFromContext` returns `Category.HabitsProductivity (6)`.
