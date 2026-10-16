# Spec Delta

## MODIFIED Requirements

### Requirement: 3-Tab Import Modal Interface
The Import Modal on `/library` SHALL provide 3 selectable tabs: Markdown Series, PDF Upload with drag-and-drop zone and upload progress, and Web Article URL Ingestion with content preview before ingestion confirmation.

All three tabs SHALL load active category descriptors from the shared category endpoint, ordered by display order and slug, and require an explicit `categoryId` UUID selection. No category SHALL be preselected or inferred from source keywords. The accessible selector SHALL show stored English or Vietnamese names for the active UI locale. Empty or unavailable metadata SHALL block submission with a localized, retryable explanation.

A category-independent helper SHALL explain that source prose is preserved while structural headings and paragraph boundaries can be restored during curation. Changing a category SHALL NOT change this preservation policy.

1. **De-Jargonized Web Article Ingestion (Tab 3):**
   - Title SHALL read "Extract Article from Web" / "Trích Xuất Bài Viết Từ Web" (replacing legacy "Cào & Chuyển Đổi").
   - Action button SHALL read "Fetch Content" / "Lấy Nội Dung" (replacing legacy "Cào Dữ Liệu").
   - URL input placeholder SHALL provide broad lifelong learning and deliberate practice examples: `"https://... (Substack, Medium, Farnam Street, blog cá nhân hoặc tài liệu)"` / `"https://... (Substack, Medium, Farnam Street, blogs, or documentation)"`.
2. **Universal Markdown & Notes Placeholder (Tab 1):**
   - Title placeholder SHALL provide diverse examples encompassing both technical and deliberate practice books: `"Ví dụ: Atomic Habits — Chương 1 hoặc Designing Data-Intensive Applications — Chương 5"`.
   - Content placeholder SHALL invite pasting notes, excerpts, or markdown text naturally.

When the URL Crawler detects an embedded PDF (`IsPdfDetected == true`), the modal SHALL render an Embedded PDF Detection Preview Card:
- Highlighting the detected document title and resolved remote PDF URL.
- Displaying a dedicated one-click action button: "Import & Slice PDF Directly" (`$t('library.import_detected_pdf')`).
- Suppressing the empty markdown preview editor and directing the user directly toward automated streaming ingestion.

#### Scenario: User selects DeepPace category in any import tab
- **GIVEN** an authenticated user opens the document import modal on `/library`
- **WHEN** user opens the Category dropdown on the Markdown, PDF, or Web URL tab
- **THEN** the dropdown contains all active categories, including administrator-created subjects, in the current locale
- **AND** selecting a category sends its UUID as `categoryId`, never an ordinal or enum name.

#### Scenario: User inputs article URL with universal web placeholder
- **GIVEN** user views the Web URL ingestion tab in the import modal
- **WHEN** the tab is rendered
- **THEN** the input displays a friendly placeholder referencing Substack, Medium, Farnam Street, blogs, and documentation
- **AND** the action button displays "Lấy Nội Dung" (Vietnamese) or "Fetch Content" (English) without scraping jargon.

#### Scenario: Verbatim hint dynamically guides mindset and habits curation
- **GIVEN** user views the Category selector in any of the 3 import tabs
- **WHEN** viewing the callout banner
- **THEN** the callout explains preservation of author prose and faithful structural restoration for every category, without recommending a category to change formatting behavior.

#### Scenario: URL crawler preview reveals detected embedded PDF reader
- **WHEN** user crawls a URL and the backend responds with `IsPdfDetected = true` and a valid `DetectedPdfUrl`
- **THEN** the modal displays an embedded PDF alert card with the resolved PDF URL and a prominent "Import & Slice PDF Directly" button
- **AND** clicking the button invokes `POST /api/v1/library/import-remote-pdf` with the detected URL, closes the modal, and initiates ingestion progress polling on the library view.

### Requirement: Paginated Book Catalog Browsing and State Synchronization
The library system SHALL expose an authenticated endpoint `GET /api/v1/library/books` accepting optional query parameters: `categoryId` (UUID), `search` (string), `page` (integer, default 1, minimum 1), and `pageSize` (integer, default 12, minimum 1, maximum 100). The endpoint SHALL enforce `.RequireAuthorization()` and reject unauthenticated requests with `HTTP 401 Unauthorized`.

When authorized, the endpoint SHALL return a structured envelope containing:
1. `books`: A list of `BookDto` items matching filter criteria, ordered by `CreatedAt` descending.
2. `totalCount`: Total number of books matching the query across the entire catalog.
3. `page`: The current active page number (1-based).
4. `pageSize`: The page size applied to the query.
5. `totalPages`: The total number of pages calculated as $\lceil \text{totalCount} / \text{pageSize} \rceil$, or 0 if `totalCount` is 0.

The `/library` page interface SHALL render book cards in a responsive grid aligned to multiples of 12 (1 column on mobile, 2 columns on tablet, 3 columns on desktop, 4 columns on large monitors), preventing ragged trailing rows.

The `/library` page SHALL render numbered pagination controls (`< 1 2 3 ... 8 >`) when `totalPages > 1`:
1. **Numbered Page Buttons**: Direct access buttons for available pages with an active highlight indicator on the currently viewed page.
2. **Ellipsis Compaction**: Pages beyond the visible window SHALL be truncated with non-clickable ellipsis (`...`) indicators.
3. **Previous / Next Controls**: Navigational buttons to decrement or increment the active page, automatically disabled on boundary pages (`page === 1` and `page === totalPages`).
4. **Filter Reset**: Applying a new category filter or entering a search query SHALL automatically reset the active page to 1.
5. **Two-Way URL Query Synchronization**: The active `page`, `categoryId`, and `search` query SHALL synchronize bidirectionally with browser URL query parameters (`?page=N&categoryId=UUID&search=S`). Reloading the page or sharing the URL SHALL restore the exact catalog page and filter state.

#### Scenario: User browses the first page of the technical library
- **WHEN** an authenticated user navigates to `/library`
- **THEN** client calls `GET /api/v1/library/books?page=1&pageSize=12` with JWT authorization header
- **AND** backend returns up to 12 book cards along with `totalCount`, `page = 1`, `pageSize = 12`, and `totalPages`
- **AND** client renders the books in a balanced responsive grid with page 1 highlighted in the pagination controls.

#### Scenario: Anonymous request to book catalog is rejected
- **WHEN** an unauthenticated client sends `GET /api/v1/library/books` without a valid JWT bearer token
- **THEN** backend rejects the request with `HTTP 401 Unauthorized`.
#### Scenario: User navigates to a subsequent catalog page
- **GIVEN** the library catalog has 30 books (`totalPages = 3`)
- **WHEN** the user clicks page number "2" in the pagination bar
- **THEN** client updates the URL query string to `?page=2`
- **AND** fetches books via `GET /api/v1/library/books?page=2&pageSize=12`
- **AND** smoothly replaces the grid items with books 13 through 24 and highlights button "2".

#### Scenario: User filters catalog by category or search term
- **GIVEN** the user is currently viewing page 3 of the catalog (`?page=3`)
- **WHEN** the user selects the administrator-defined Backend category or enters search term "architecture"
- **THEN** client resets the active page to 1
- **AND** updates URL query string to `?categoryId=<backend-category-id>&search=architecture&page=1`
- **AND** requests page 1 of the filtered result set from the backend.

#### Scenario: User enters direct URL with pagination and filter parameters
- **WHEN** a user navigates directly to `/library?categoryId=<database-category-id>&search=postgres&page=2` via bookmark or shared link
- **THEN** client parses `categoryId = the selected category UUID`, `search = 'postgres'`, and `page = 2` from the route query
- **AND** dispatches `GET /api/v1/library/books?categoryId=<database-category-id>&search=postgres&page=2&pageSize=12`
- **AND** renders the second page of matching database books with active filter states displayed.

#### Scenario: User navigates using browser back and forward history
- **GIVEN** the user navigated from page 1 to page 2 and then page 3 in the library
- **WHEN** the user clicks the browser "Back" button
- **THEN** client responds to the URL query change to `?page=2`
- **AND** fetches and displays page 2 items without a full page reload or layout flickering.

#### Scenario: User requests an out-of-bounds page number
- **WHEN** a user manually enters a URL with `?page=999` exceeding `totalPages`
- **THEN** backend returns an empty `books` list with the accurate `totalCount` and `totalPages`
- **AND** client displays a friendly empty state prompting the user to return to page 1.

#### Scenario: Mobile viewport renders compact responsive pagination controls
- **GIVEN** the user accesses `/library` on a narrow mobile viewport ($< 640\text{px}$)
- **WHEN** the catalog contains multiple pages
- **THEN** pagination controls condense the numbered buttons to the current page and immediate neighbors while maintaining minimum touch target dimensions ($\ge 40\text{px} \times 40\text{px}$) and touch accessibility.

#### Scenario: Retained inactive category filter
- **WHEN** a user opens a valid `categoryId` filter for an inactive category
- **THEN** the owned documents remain filterable and the category label is resolved from shared metadata
- **AND** an unknown or malformed category reference is rejected without falling back to another category.

### Requirement: Book Card Category Badge and Ingestion Status Localization
The library catalog on `/library` SHALL display localized category badges and localized background ingestion progress indicators on each document card.

1. **Category Badge Resolution:**
   - Each book SHALL expose `categoryId` and its authoritative category descriptor.
   - The badge SHALL use `nameEn` or `nameVi` for the current locale, including inactive categories, without numeric/string enum normalization or a fixed translation map.
   - Renamed categories SHALL display current metadata after refresh without changing book identity, content, or progress.
   - A missing descriptor SHALL produce an explicit unavailable state rather than a fabricated Engineering category.
2. **Dynamic Ingestion Status Translation:**
   - When a book's processing status is `Processing` (or numeric `1`), the book card SHALL render a localized status indicator via a status message translator (`getStatusMessage`).
   - The translator SHALL detect backend progress patterns and map them to localized strings:
     - `"File uploaded, queued for processing..."` $\to$ `$t('library.status_uploaded_queued')`
     - `"Remote PDF download initiated. Processing chapters..."` $\to$ `$t('library.status_remote_download')`
     - `"Analyzing document structure and bookmarks..."` or `"Analyzing PDF structure and bookmarks..."` $\to$ `$t('library.status_analyzing_structure')`
     - `"Persisting chapters and slices..."` $\to$ `$t('library.status_persisting_slices')`
     - `"Parsing pages..."` $\to$ `$t('library.status_parsing_pages')`
     - `"Generating chunks..."` $\to$ `$t('library.status_generating_chunks')`
     - `"AI is curating initial slice {current}/{total}..."` $\to$ `$t('library.status_curating_slice', { current, total })`
     - `"Analyzing content: page {current}/{total}"` $\to$ `$t('library.status_analyzing_pages', { current, total })`
     - `"Extracting topic: {topic}"` $\to$ `$t('library.status_extracting_topic', { topic })`
     - `"Extracted {count} slices. Complete!"` $\to$ `$t('library.status_extracted_complete', { count })`
     - `"Ready for reading"` $\to$ `$t('library.status_ready')`
   - For unrecognized status strings, the translator SHALL fall back to the existing message or `$t('library.processing_pdf')`.

#### Scenario: Book card displays localized category for string enum in Vietnamese
- **GIVEN** an EngineeringCraft string reference has been migrated to a UUID descriptor with `nameVi = "Tư Duy Kỹ Sư & Năng Suất"` and the locale is Vietnamese
- **WHEN** the user views the book card in the `/library` grid
- **THEN** the category badge renders `"Tư Duy Kỹ Sư & Năng Suất"`
- **AND** the badge does NOT render `'Engineering'`.

#### Scenario: Book card displays localized category for string enum in English
- **GIVEN** a legacy BackendDotNet reference has been migrated to a UUID descriptor with `nameEn = "Backend & Distributed"` and the locale is English
- **WHEN** the user views the book card in the `/library` grid
- **THEN** the category badge renders `"Backend & Distributed"`.

#### Scenario: Book card displays localized category for numeric ID
- **GIVEN** a former ordinal category reference has been migrated to a UUID descriptor with `nameVi = "Cơ Sở Dữ Liệu"` and the locale is Vietnamese
- **WHEN** the user views the book card in the `/library` grid
- **THEN** the category badge renders `"Cơ Sở Dữ Liệu"`.

#### Scenario: Book card displays localized status for background queuing
- **GIVEN** a book is in status `Processing` with `statusMessage = "File uploaded, queued for processing..."` and locale is Vietnamese (`vi`)
- **WHEN** the user views the book card in the `/library` grid
- **THEN** the ingestion indicator displays `"Tệp đã tải lên, đang chờ xử lý..."`.

#### Scenario: Book card displays parameterized localized status for slice curation
- **GIVEN** a book is in status `Processing` with `statusMessage = "AI is curating initial slice 3/12..."` and locale is Vietnamese (`vi`)
- **WHEN** the user views the book card in the `/library` grid
- **THEN** the ingestion indicator displays the interpolated string `"AI đang tối ưu hóa lát cắt mở đầu 3/12..."`.

### Requirement: Verbatim Book Text Preservation & Curation Safety
The system SHALL guarantee 100% verbatim text preservation for imported documents in every category, including administrator-created subjects.

The source reading body SHALL NEVER be replaced with an AI summary, condensed prose, or synthetic content. A successful `FormattedMarkdown` result may replace the stored body only as faithful structural restoration under the preservation invariants below.

The system SHALL enforce the following invariants during book ingestion and slice curation:
1. **Verbatim Content Invariance:** The source reading body SHALL retain the author's words, examples, and language without omission, abbreviation, condensation, or AI rewording. During an existing ingestion or explicit curation action, faithful structural restoration MAY adjust Markdown headings, paragraph boundaries, and non-content extraction artifacts as specified by `ai-curation`; raw extraction bytes need not remain identical. Category changes SHALL never trigger this operation.
2. **AI Enrichment Scope:** The AI slice formatter (`GeminiAiService.FormatSliceAsync`) SHALL populate auxiliary fields separately from the faithful source reading body:
   - `DocumentChunk.SummaryMarkdown`: Structured executive summary and core conceptual takeaways.
   - `DocumentChunk.KeyTakeaways`: Exactly 3 high-impact engineering habits or principles.
   - `DocumentChunk.EstimatedReadMinutes`: Calculated reading time.
   - `InterviewQuestions`: Senior Scenario Drill evaluating practical trade-offs.
3. **Reader Surface Fidelity:** The client reader interface (`/read/[bookId]`) SHALL render the author's authentic prose directly from `OriginalTextMarkdown`, ensuring readers engage with the author's true words, narratives, and tone.

#### Scenario: Background PDF ingestion curates slice for Engineering Craft book
- **WHEN** `PdfIngestionWorker` curates an extracted chapter slice for a book in any retained category
- **THEN** the worker invokes the common source-faithful curation flow to restore structure and generate separate summaries, takeaways, and scenario drills
- **AND** the source reading body is never replaced by condensed or synthetic AI content
- **AND** the source reading body preserves the complete extracted author prose while permitting faithful structural Markdown restoration
- **AND** `chunk.SummaryMarkdown`, `chunk.KeyTakeaways`, and `InterviewQuestions` are populated successfully.

#### Scenario: On-demand slice curation preserves original book prose
- **WHEN** an authenticated user or worker triggers `CurateSliceHandler` for a slice in an imported book
- **THEN** the handler preserves the author's prose and source language while persisting faithful structural restoration
- **AND** updates structural Markdown and the separate summary, takeaways, reading-time, and question fields without truncating or rewriting the author's narrative prose.

#### Scenario: Reader interface renders author's true words for imported book
- **WHEN** an authenticated user opens `/read/[bookId]` for an imported book in any category
- **THEN** the reader renders `chunk.OriginalTextMarkdown` containing the author's complete, uncompressed narrative prose
- **AND** the reader does NOT render an AI-condensed summary in place of the author's words.

#### Scenario: End-to-end local validation with Vietnamese Atomic Habits PDF fixture
- **WHEN** a Vietnamese narrative PDF fixture with chapter boundaries, dialogue, and examples is processed through the local ingestion pipeline
- **THEN** `PdfPigExtractor` segments the book into chunks aligned with the source chapter boundaries
- **AND** every generated `DocumentChunk.OriginalTextMarkdown` retains the complete author prose across all chapters without AI compression, allowing only faithful structural restoration.

#### Scenario: Category metadata changes do not curate historical prose
- **WHEN** an administrator renames, reorders, or deactivates a category or an owner reclassifies an existing book
- **THEN** source prose, summaries, audio, and learning progress remain unchanged and no AI formatting is scheduled.

### Requirement: Category Filter Bar Layout Stability
The `/library` catalog page SHALL provide a unified, responsive search and filter toolbar layout synchronized across both the "My Library" and "Discover" tabs.

On both "My Library" and "Discover" tabs, the primary toolbar SHALL present:
1. A search input (`sm:w-72 sm:flex-initial`) with dedicated clear trigger (`X`) and search execution on Enter or clear.
2. An All pill and a bounded quick subset selected by configured category order, not hardcoded subject names. Every category SHALL remain reachable through the advanced filter even when omitted from the quick subset.
3. An Advanced Filter trigger button (`[ 🎛️ Bộ Lọc ]` / "Filter") displaying an active filter badge counter when any non-default filter is applied.

Clicking the Advanced Filter trigger button SHALL open an accessible filter dialog modal permitting users to filter documents by all nondeleted categories from shared metadata, including inactive categories for historical filtering, content language, and document format, with dedicated reset and apply actions.

Filter pills SHALL maintain `font-semibold` across active and inactive states with `transition-colors duration-150` to guarantee zero layout shift when switching categories.

#### Scenario: User filters library by Mental Models category
- **WHEN** the user selects the stored Mental Models category from the available category controls, opening the advanced filter when it is outside the quick subset
- **THEN** the book catalog updates to display only books and articles categorized under the selected Mental Models category UUID
- **AND** the filter button transitions active styling with zero horizontal layout shift or width jumping.

#### Scenario: User filters library by Habits & Deep Work category
- **WHEN** the user selects the stored Habits category from the available category controls, opening the advanced filter when it is outside the quick subset
- **THEN** the book catalog updates to display only books and articles categorized under the selected Habits category UUID.

#### Scenario: User switches category filter tab in library
- **WHEN** user clicks a category tab (e.g., "Tất Cả", "Frontend & Web", "Backend & Phân Tán", "Thiết Kế Mã", "Mô Hình Tư Duy")
- **THEN** the active tab updates its background, border, and text colors via `transition-colors`
- **AND** the button maintains its exact text width and baseline geometry without any border jitter, horizontal position jumping, or layout shifts across adjacent tabs.

#### Scenario: User clears or toggles category filter
- **WHEN** user clicks "Tất Cả" (All) after viewing a specific category
- **THEN** the category filter state is cleared
- **AND** the tab switches cleanly with smooth color transitions and zero width shift.

#### Scenario: User filters My Library with quick category chips
- **WHEN** user clicks "Backend & Runtime" in My Library
- **THEN** the book list updates to show only personal books under the selected Backend category UUID
- **AND** the button styling transitions with zero layout shift or width jumping.

#### Scenario: User opens advanced filter modal in My Library
- **WHEN** user clicks the "Bộ Lọc" button in My Library
- **THEN** the advanced filter modal opens, displaying all nondeleted categories, language options, and source formats
- **AND** the trigger button displays the active filter count badge when filters are applied.

#### Scenario: User applies deep category filter from modal in My Library
- **WHEN** the user selects the Engineering Craft or Mental Models category UUID from the advanced filter and clicks Apply
- **THEN** the modal closes, the catalog updates to display the matching personal books, and the filter counter badge increments.

#### Scenario: User clears or resets filters in My Library
- **WHEN** user clicks Reset in the filter modal or clicks the "Tất Cả" quick pill
- **THEN** all category and search filters are cleared, and the book list reloads all documents.

#### Scenario: New category outside the quick subset
- **WHEN** an administrator creates a category that is outside the toolbar's bounded quick subset
- **THEN** it appears in the advanced filter with its localized stored name and filtering sends its `categoryId` UUID.

### Requirement: Book Metadata and Category Update API
The system SHALL expose an authenticated HTTP PATCH endpoint `PATCH /api/v1/library/books/{id}` allowing users to update the metadata and domain category of books they have imported into their library.

1. **Authentication & Authorization**:
   - The endpoint SHALL require valid JWT Bearer authentication (`.RequireAuthorization()`) and return `HTTP 401 Unauthorized` for unauthenticated requests.
   - The endpoint SHALL verify that the requesting user is the owner of the book (`CreatedByUserId == currentUserId`). If the user does not own the book or `CreatedByUserId` is null, the endpoint SHALL return `HTTP 403 Forbidden` with RFC 7807 problem details and error code `LIBRARY_FORBIDDEN`.
2. **Resource Existence & Soft Delete**:
   - If the book ID does not exist or has `IsDeleted == true`, the endpoint SHALL return `HTTP 404 Not Found`.
3. **Payload Validation**:
   - The payload SHALL accept `Title` (string, required, length 1–255), `AuthorOrSourceUrl` (string, optional, max length 500), `CategoryId` (required UUID referencing a nondeleted category), and `Language` (string, optional, allowed values: `"en"`, `"vi"`).
   - If `Title` is whitespace or exceeds 255 characters, `CategoryId` is malformed or does not identify a nondeleted category, or `Language` is specified with an unsupported locale, the system SHALL return `HTTP 400 Bad Request` with validation error details.
4. **Cascade Taxonomy and Language Alignment**:
   - Changing `CategoryId` SHALL require an active category, persist the new reference, and realign linked learning artifacts without changing the book identifier, slug, prose, progress, or source-ingestion state. Retaining the current inactive category during unrelated edits SHALL remain permitted.
   - Subsequent calls to `GET /api/v1/graph` for the user SHALL immediately reflect the new pillar connection (`pillar-{categoryId}`), moving the book and its chunks to the updated DeepPace constellation without dangling edges.
   - Updating `Language` SHALL cascade and persist `chunk.Language = request.Language` on all surviving `DocumentChunk` records belonging to the book, ensuring subsequent AI curation (`CurateSliceHandler`) and TTS narration (`GetOrSynthesizeChunkAudioHandler`) operate under the updated language code.
5. **Response Contract**:
   - Upon successful update, the endpoint SHALL return `HTTP 200 OK` with the updated `BookDto`.

#### Scenario: Owner updates book title, author, and category
- **WHEN** an authenticated user who owns book `123` submits `PATCH /api/v1/library/books/123` with `Title = "Atomic Habits (Bản Đầy Đủ)"`, `AuthorOrSourceUrl = "James Clear"`, and `CategoryId` set to the active Habits category UUID
- **THEN** the system returns `HTTP 200 OK` with updated `BookDto`
- **AND** the book's category UUID is updated to the selected category
- **AND** the book's `Title` is updated in the database.

#### Scenario: Non-owner attempts to update book
- **WHEN** an authenticated user attempts to update a book created by a different account
- **THEN** the system returns `HTTP 403 Forbidden` with error code `LIBRARY_FORBIDDEN`
- **AND** no book data is modified.

#### Scenario: Update with invalid title or category
- **WHEN** user submits `PATCH /api/v1/library/books/{id}` with an empty title or an unknown or malformed category UUID
- **THEN** the system returns `HTTP 400 Bad Request` with descriptive validation errors.

#### Scenario: Owner updates book language and cascades to document chunks
- **WHEN** an authenticated user who owns book `123` submits `PATCH /api/v1/library/books/123` with `Language = "vi"`
- **THEN** the system returns `HTTP 200 OK`
- **AND** all existing `DocumentChunk` records belonging to book `123` have their `Language` updated to `"vi"`
- **AND** subsequent TTS audio generation requests for chunks in this book select the Vietnamese neural voice.

#### Scenario: Reclassification races with deactivation
- **WHEN** the requested category is deactivated before reclassification admission commits
- **THEN** the endpoint returns HTTP 409 `Category.Inactive` and leaves the original assignment and linked learning state unchanged
- **AND** existing queued/running source-operation concurrency protections remain enforced.

### Requirement: Book Management and Edit Interface in Library
The Document Library interface (`pages/library.vue`) SHALL provide an accessible, responsive book editing modal dialog complying with the **Dev-Learning Studio** UI design system and Pillar 2 component governance.

1. **Card Action Trigger**:
   - Each book card owned by the user SHALL display an Edit button (`Pencil` icon, `library.edit_book`) alongside the Delete button.
   - The button SHALL have responsive padding and an accessible label/tooltip.
2. **Accessible Edit Modal (`EditBookModal`)**:
   - Clicking Edit SHALL open a teleported dialog adhering to `AppModal.vue` conventions (strictly prohibiting native browser `prompt` or `confirm` dialogs).
   - **Category & Language Dropdowns**: The modal MUST use custom accessible `AppSelect.vue` to render active category descriptors plus the book's current inactive category when applicable, and document language choices (`en` / `vi`), strictly prohibiting raw HTML `<select><option>` elements.
   - **Form Fields**: Accessible text inputs for Book Title and Author / Source URL.
   - **Taxonomy Callout**: A responsive informational banner explaining: *"Thay đổi chuyên mục sẽ cập nhật lại Cột trụ Đồ thị Tri thức và phân loại thẻ nhớ tương ứng"* (Vietnamese) / *"Updating the category re-aligns Knowledge Graph constellations and spaced repetition cards"* (English).
   - **Action Buttons**: "Hủy" (`library.cancel`) and "Lưu Thay Đổi" (`library.save_changes`) with `whitespace-nowrap shrink-0` and responsive gap, displaying a loading spinner during submission (`isSaving`).
3. **Optimistic / Immediate UI Synchronization**:
   - Upon successful save, the modal closes, a success toast notification appears (`toast.success`), and the book card on `/library` updates its title, category badge, and metadata immediately without requiring a full page refresh.

#### Scenario: User opens edit modal and updates category
- **WHEN** user clicks the Edit button on a book card in `/library`
- **THEN** the modal opens with current book Title, Author, and Category pre-populated in `AppSelect`
- **WHEN** user selects the active category whose stored Vietnamese name is "Thói Quen & Tập Trung Sâu" and clicks "Lưu Thay Đổi"
- **THEN** the modal shows a loading spinner
- **AND** upon API success, the modal closes, a success toast is displayed, and the book card badge updates to "Thói Quen & Tập Trung Sâu".

#### Scenario: Responsive and bilingual button layout
- **WHEN** the edit modal renders in both English and Vietnamese locales across Mobile ($390\text{px}$) and Desktop ($1440\text{px}$) viewports
- **THEN** all action buttons maintain `whitespace-nowrap shrink-0` with zero text truncation or button collision.

#### Scenario: Editing a book with an inactive category
- **WHEN** the owner opens the edit dialog for a book assigned to an inactive category
- **THEN** its current category remains visible and retaining it is permitted
- **AND** inactive categories are not offered as new assignments to other books.

## REMOVED Requirements

### Requirement: Engineering Mindset & Productivity Category Support
**Reason**: Categories are administrator-managed metadata, not a closed enum or a hidden AI formatting mode. Source-grounded enrichment and source fidelity apply to every category.
**Migration**: Migrate existing Engineering Craft references to the seeded stable UUID and retain their content/history; remove category-specific prompt and persistence branches under the common preservation contract.

### Requirement: DeepPace Category Keyword Inference
**Reason**: A fixed keyword table cannot represent administrator-created categories and silently misclassifies user sources.
**Migration**: Require explicit active category selection on every new document path; crawler previews retain the user selection and no classifier or fallback category replaces it.
