## MODIFIED Requirements

### Requirement: 3-Tab Import Modal Interface
The Import Modal on `/library` SHALL provide 3 selectable tabs: Markdown Series, PDF Upload with drag-and-drop zone and upload progress, and URL Crawler with content preview before ingestion confirmation.

Under the Category selector in all 3 modal tabs (Markdown Series, PDF Upload, and URL Crawler), the modal SHALL render a dedicated in-context verbatim category helper callout banner:
1. **Notice Content & Guidance:**
   - Clearly explains that selecting Category 4 ("Engineering Craft & Mindset" / "Tư Duy Kỹ Sư & Năng Suất") preserves 100% of the book's original text while AI automatically restores run-in headings and paragraph boundaries.
   - **Vietnamese (`vi`):** `"Mẹo: Chọn chuyên mục \"Tư Duy Kỹ Sư & Năng Suất\" để giữ nguyên văn 100% nội dung sách (AI khôi phục tiêu đề & ngắt dòng)."`
   - **English (`en`):** `"Tip: Select \"Engineering Craft & Mindset\" to preserve 100% verbatim book text (AI restores headings & paragraphs)."`
2. **Visual Presentation:**
   - Styled as an ambient, non-intrusive alert box with rounded corners (`rounded-xl`), soft amber background (`bg-amber-50/80 dark:bg-amber-950/30`), amber border (`border-amber-200/80 dark:border-amber-900/50`), and readable amber text (`text-xs text-amber-800 dark:text-amber-300`).
   - Accompanied by a leading lightbulb icon (`Lightbulb`, `text-amber-500`) to highlight helpful guidance.

#### Scenario: User switches import modal tabs
- **GIVEN** an authenticated user on the `/library` page
- **WHEN** user opens the import modal and selects the "PDF Upload" tab
- **THEN** UI displays the drag-and-drop dropzone with file size limit guidance, category selector, and the streamlined verbatim category helper notice.

#### Scenario: User crawls URL and previews content in modal
- **GIVEN** user is in the "URL Crawler" tab of the import modal
- **WHEN** user inputs a documentation URL and clicks "Fetch Content"
- **THEN** UI displays the article title, category selector with the streamlined verbatim category helper notice, and markdown preview before final import confirmation.

#### Scenario: Streamlined verbatim category hint displayed in Markdown Series tab
- **GIVEN** user opens the import modal on `/library`
- **WHEN** user views the "Markdown Series" tab
- **THEN** the streamlined helper notice appears directly below the Category selector
- **AND** displays the localized tip explaining that Category "Engineering Craft & Mindset" preserves 100% verbatim text with AI-restored headings and paragraphs.

#### Scenario: Streamlined verbatim category hint displayed in PDF Upload tab
- **GIVEN** user opens the import modal on `/library` and selects the "PDF Upload" tab
- **WHEN** user views the upload form fields
- **THEN** the streamlined verbatim category helper notice appears directly beneath the Category selector and above the file dropzone.

#### Scenario: Streamlined verbatim category hint displayed in URL Crawler tab
- **GIVEN** user opens the import modal on `/library` and selects the "URL Crawler" tab
- **WHEN** user views the URL crawl parameters
- **THEN** the streamlined verbatim category helper notice appears directly beneath the Category selector.

#### Scenario: Bilingual rendering of streamlined verbatim category hint
- **GIVEN** user changes locale between Vietnamese (`vi`) and English (`en`)
- **WHEN** viewing the Category selector in any import modal tab
- **THEN** the helper notice dynamically renders the corresponding localized text without truncation or container overflow.
