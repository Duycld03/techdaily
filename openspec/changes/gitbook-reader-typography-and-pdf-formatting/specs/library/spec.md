## MODIFIED Requirements

### Requirement: 3-Tab Import Modal Interface
The Import Modal on `/library` SHALL provide 3 selectable tabs: Markdown Series, PDF Upload with drag-and-drop zone and upload progress, and URL Crawler with content preview before ingestion confirmation.

Under the Category selector in all 3 modal tabs (Markdown Series, PDF Upload, and URL Crawler), the modal SHALL render a dedicated verbatim category helper callout banner:
1. **Notice Content & Guidance:**
   - Explains that selecting "Engineering Craft & Mindset" ("Tư Duy Kỹ Sư & Năng Suất") guarantees 100% verbatim author text preservation without AI condensing or summarization.
   - **Vietnamese (`vi`):** `"💡 Mẹo: Chọn danh mục \"Tư Duy Kỹ Sư & Năng Suất\" nếu bạn muốn giữ nguyên văn 100% từng câu chữ của sách (không dùng AI tóm tắt ngắn lại)."`
   - **English (`en`):** `"💡 Tip: Select \"Engineering Craft & Mindset\" if you want 100% verbatim author text preserved without AI condensing."`
2. **Visual Presentation:**
   - Styled as an ambient, non-intrusive tip box with rounded corners (`rounded-xl`), soft background (`bg-amber-50/70 dark:bg-amber-950/30`), border (`border-amber-200 dark:border-amber-800/60`), and clear readable typography (`text-xs text-amber-800 dark:text-amber-300`).

#### Scenario: User switches import modal tabs
- **GIVEN** an authenticated user on the `/library` page
- **WHEN** user opens import modal on `/library` and selects "PDF Upload" tab
- **THEN** UI displays drag-and-drop dropzone with file size limit guidance, category selector, and verbatim category helper notice.

#### Scenario: User crawls URL and previews content in modal
- **GIVEN** user is in the "URL Crawler" tab of the import modal
- **WHEN** user inputs URL and clicks "Fetch Content"
- **THEN** UI displays title, category selector with verbatim category helper notice, and markdown preview before user confirms final import.

#### Scenario: Verbatim category helper hint displayed in Markdown Series tab
- **GIVEN** user opens the import modal on `/library`
- **WHEN** user views the "Markdown Series" tab
- **THEN** a visual helper notice appears directly below the Category selector
- **AND** displays the tip explaining that Category "Engineering Craft & Mindset" preserves 100% verbatim author text without AI summarization.

#### Scenario: Verbatim category helper hint displayed in PDF Upload tab
- **GIVEN** user opens the import modal on `/library` and selects the "PDF Upload" tab
- **WHEN** user views the upload form fields
- **THEN** the verbatim category helper notice appears directly beneath the Category selector and above the file dropzone.

#### Scenario: Verbatim category helper hint displayed in URL Crawler tab
- **GIVEN** user opens the import modal on `/library` and selects the "URL Crawler" tab
- **WHEN** user views the URL crawl parameters
- **THEN** the verbatim category helper notice appears directly beneath the Category selector.

#### Scenario: Bilingual rendering of verbatim category helper hint in Vietnamese and English
- **GIVEN** user changes locale between Vietnamese (`vi`) and English (`en`)
- **WHEN** viewing the Category selector in any import modal tab
- **THEN** the helper notice dynamically renders the corresponding localized text without truncation or container overflow.
