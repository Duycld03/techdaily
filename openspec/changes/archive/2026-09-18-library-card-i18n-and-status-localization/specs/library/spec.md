## MODIFIED Requirements

### Requirement: Internationalization (i18n)
All new modal tabs, dropzones, upload limits, crawl buttons, loading states, error alerts, input placeholders, technical service notes, and book card badges SHALL have full `en` and `vi` translations in locale files.

The document import modal SHALL display localized placeholders and service notices across all tabs:
1. **Markdown Tab Content Placeholder:** The content textarea SHALL bind to `$t('library.content_placeholder')`.
2. **PDF Upload Tab Replace Hint:** The dropzone replace guidance SHALL bind to `$t('library.pdf_replace_hint')`.
3. **PDF Upload Tab Title Placeholder:** The optional title input SHALL bind to `$t('library.title_placeholder')`.
4. **PDF Upload Tab Architecture Notes:** Technical service badges SHALL bind to `$t('library.pdf_service_note_1')` ("300 MB Streaming • Background Service" / "Xử lý luồng 300 MB • Dịch vụ chạy ngầm") and `$t('library.pdf_service_note_2')` ("Look-Ahead Buffer Synthesis" / "Tổng hợp bộ đệm dự đoán trước").

#### Scenario: Vietnamese user views import modal
- **GIVEN** an authenticated user has selected the Vietnamese (`vi`) locale
- **WHEN** user opens the document import modal on `/library`
- **THEN** all tab labels, upload instructions, button labels, input placeholders, and technical service notes render in Vietnamese without raw English text.

#### Scenario: English user views import modal
- **GIVEN** an authenticated user has selected the English (`en`) locale
- **WHEN** user opens the document import modal on `/library`
- **THEN** all tab labels, upload instructions, button labels, input placeholders, and technical service notes render in English.

---

## ADDED Requirements

### Requirement: Book Card Category Badge and Ingestion Status Localization
The library catalog on `/library` SHALL display localized category badges and localized background ingestion progress indicators on each document card.

1. **Category Badge Resolution:**
   - The UI SHALL implement a category label resolver (`getCategoryLabel`) that normalizes both backend string enum identifiers (`"EngineeringCraft"`, `"BackendDotNet"`, `"DatabaseStorage"`, `"FrontendWeb"`, `"SystemDesign"`) and numeric identifiers (`0..4`).
   - The resolver SHALL map each normalized category to its corresponding translation key under `library.categories`:
     - `FrontendWeb` / `0` $\to$ `$t('library.categories.frontend')` ("Frontend & Web")
     - `BackendDotNet` / `1` $\to$ `$t('library.categories.backend')` ("Backend & Phân Tán" / "Backend & Distributed")
     - `DatabaseStorage` / `2` $\to$ `$t('library.categories.database')` ("Cơ Sở Dữ Liệu" / "Database & Storage")
     - `SystemDesign` / `3` $\to$ `$t('library.categories.system_design')` ("Thiết Kế Hệ Thống" / "System Design")
     - `EngineeringCraft` / `4` $\to$ `$t('library.categories.craft')` ("Tư Duy Kỹ Sư & Năng Suất" / "Engineering Craft & Mindset")
   - The UI SHALL NOT default to the hardcoded English string `'Engineering'`.

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
- **GIVEN** a book entity returned from the API has `category = "EngineeringCraft"` and the user's active locale is Vietnamese (`vi`)
- **WHEN** the user views the book card in the `/library` grid
- **THEN** the category badge renders `"Tư Duy Kỹ Sư & Năng Suất"`
- **AND** the badge does NOT render `'Engineering'`.

#### Scenario: Book card displays localized category for string enum in English
- **GIVEN** a book entity returned from the API has `category = "BackendDotNet"` and the user's active locale is English (`en`)
- **WHEN** the user views the book card in the `/library` grid
- **THEN** the category badge renders `"Backend & Distributed"`.

#### Scenario: Book card displays localized category for numeric ID
- **GIVEN** a book entity in the store has numeric `category = 2` and the user's active locale is Vietnamese (`vi`)
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

---

### Requirement: Book Card Action Row Presentation
The book card footer SHALL present primary management and navigation actions without redundant branding or truncated labels.

1. **Removal of Redundant Brand Badge:**
   - The book card footer SHALL NOT render the hardcoded `<span ...>GitBook Reader</span>` element.
   - The action row SHALL cleanly partition secondary actions (Delete button, Export to Obsidian button) on the left and the primary navigation CTA ("Read Slices" / "Continue Reading") on the right.

#### Scenario: Book card footer renders cleanly without truncated brand label
- **GIVEN** any book card rendered in the `/library` grid
- **WHEN** the user inspects the card footer action row
- **THEN** the footer contains the Delete button, the Export to Obsidian button, and the primary reading navigation CTA
- **AND** the truncated text `"GitBook Rea..."` or `"GitBook Reader"` is completely absent from the DOM.
