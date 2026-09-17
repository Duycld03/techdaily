# Proposal: AI Structural Restoration and Today Reader Typography Controls

## Why

TechDaily bridges rigorous software engineering architecture manuals and impactful engineering craft literature (e.g. *Atomic Habits*, *Deep Work*, *The Staff Engineer's Path*). However, two fundamental readability and presentation challenges degrade the reading experience across the platform:

1. **Monolithic Run-in Text in Extracted Books:**
   In published books imported via PDF, Web Markdown, or Embedded Web PDF viewer shells, book publishers frequently format subheadings, chapter section titles, and narrative stories into single run-in text blocks without separate vertical baselines, distinct font sizes, or explicit margins. Pure geometric coordinate extraction (such as baseline Y grouping in `PdfPigExtractor`) cannot distinguish inline narrative introductions from section subheadings.
   Furthermore, the slice curation engine (`CurateSliceHandler.cs`) previously enforced a rigid guardrail preventing `DocumentChunk.OriginalTextMarkdown` from being updated for `Category.EngineeringCraft`. Consequently, readers in the reader view (`/read/[bookId]`) and daily focus view (`/today`) were left with raw, squashed, unformatted text blocks devoid of section breaks, clear headers, or paragraph rhythm.

2. **Fragmented Typography Controls Across Reader Surfaces:**
   While the dedicated book reader (`/read/[bookId].vue`) recently introduced novel-style typography settings (`Aa`), the daily focus reading view (`frontend/components/today/DocReaderPane.vue` on `/today`) remains locked into hardcoded font sizes and default system sans-serif typography. Software engineers spending their morning deep-focus routine on `/today` cannot adjust font size, select a high-readability serif font for long-form narrative craft or monospace for technical code, or customize line height. Additionally, typography preferences in `/read/[bookId]` are implemented as component-local state rather than a shared composable, preventing preference synchronization across reading views.

3. **Inaccurate Library Ingestion Guidance:**
   The current visual note beneath the Category selector in `/library` import modals does not accurately reflect how AI curation restores structure while keeping 100% of the author's prose intact. A concise, polished hint is required in both Vietnamese and English to reassure users that selecting "Engineering Craft & Mindset" preserves complete author text while repairing damaged layout structures.

---

## What Changes

We propose a comprehensive end-to-end solution spanning backend AI structural restoration, unified ingestion persistence, and frontend shared typography controls:

1. **AI Structural Restoration & Verbatim Book Formatting (`GeminiAiService.cs` & `CurateSliceHandler.cs`):**
   - **100% Verbatim Text Preservation Mandate:** Adapt `GeminiAiService.FormatSliceAsync` for `Category.EngineeringCraft` to strictly prohibit summarization, truncation, or omission of the author's narrative stories, anecdotes, and examples.
   - **Intelligent Structural Restoration:** Direct the Gemini model to detect run-in headings and break them out into standard Markdown headings (`### {SectionTitle}`), eliminate duplicate header echoes, format proper paragraph boundaries with clean double newlines (`\n\n`), and preserve spoken dialogue and quotes.
   - **Clean AI-Restored Text Storage:** Update `CurateSliceHandler.cs` to set `chunk.OriginalTextMarkdown = aiResult.Value.FormattedMarkdown` when `FormattedMarkdown` is present, ensuring that the restored verbatim markdown is persisted and rendered across all reader interfaces.

2. **Synchronized Ingestion Pipeline Across All Sources:**
   - Ensure Direct PDF uploads (`POST /api/v1/library/upload-pdf`), Web Markdown crawls (`POST /api/v1/library/import-document`), and Embedded Web PDFs (`POST /api/v1/library/import-remote-pdf`) all channel their slices through `CurateSliceHandler`, guaranteeing consistent AI structural restoration regardless of document origin.

3. **Streamlined In-Context Note in Library Modals (`frontend/pages/library.vue`):**
   - Update the full-width amber callout beneath the Category selector across all three import modal tabs (Markdown Series, PDF Upload, URL Crawler) to provide concise, confidence-inspiring guidance:
     - **VI:** `"Mẹo: Chọn chuyên mục \"Tư Duy Kỹ Sư & Năng Suất\" để giữ nguyên văn 100% nội dung sách (AI khôi phục tiêu đề & ngắt dòng)."`
     - **EN:** `"Tip: Select \"Engineering Craft & Mindset\" to preserve 100% verbatim book text (AI restores headings & paragraphs)."`

4. **Shared Typography Composable & Controls in `/today` (`DocReaderPane.vue`):**
   - **Shared Composable (`frontend/composables/useReaderTypography.ts`):** Extract and centralize reactive typography state (`fontSize`, `fontFamily`, `lineSpacing`, `readingWidth`) synchronized via `localStorage` under key `techdaily_reader_typography`.
   - **Refactor `/read/[bookId].vue`:** Migrate the dedicated reader to consume the unified `useReaderTypography()` composable, eliminating duplicate state logic.
   - **Typography Controls in `DocReaderPane.vue`:**
     - Add a sleek `Aa` button and popover dropdown to the header of the daily reader pane (Font Size `A-`/`A+`, Font Family Sans/Serif/Mono, Line Spacing Normal/Relaxed/Loose).
     - Bind dynamic inline styles (`fontSize`, `lineHeight`) and typography classes to the reader content container.
     - Add scoped CSS rules (`:deep(.markdown-body p), :deep(.markdown-body li) { font-size: inherit !important; line-height: inherit !important; }`) ensuring complete typography inheritance across rendered markdown elements.
     - Ensure preferences seamlessly synchronize in real-time between `/today` and `/read/[bookId]`.

---

## Capabilities

### New Capabilities

- `ai-curation`: AI-powered structural restoration, run-in heading detection, and verbatim prose formatting for imported literature without narrative summarization.
- `today-reader`: Typography controls, font scaling, and shared layout state synchronization for the daily focus reading view (`/today`).

### Modified Capabilities

- `library`: Streamlined verbatim category helper guidance callout across import tabs and unified AI structural restoration persistence across all ingestion pipelines.

---

## Impact

- **Backend:**
  - `backend/src/TechDaily.Infrastructure/Services/GeminiAiService.cs`: Enhanced system instruction for `Category.EngineeringCraft` mandating 100% verbatim text preservation, run-in heading detection, header echo suppression, and paragraph restoration.
  - `backend/src/TechDaily.Application/Features/Library/CurateSlice/CurateSliceHandler.cs`: Allow persisting `chunk.OriginalTextMarkdown = aiResult.Value.FormattedMarkdown` when `FormattedMarkdown` is present.
  - `backend/tests/TechDaily.Tests/`: Unit tests validating verbatim prompt guidelines, heading preservation, and slice curation handling.
- **Frontend:**
  - `frontend/composables/useReaderTypography.ts`: New shared composable encapsulating reactive typography settings and `localStorage` synchronization.
  - `frontend/pages/read/[bookId].vue`: Refactored to leverage `useReaderTypography()`.
  - `frontend/components/today/DocReaderPane.vue`: Integrated `Aa` typography button and dropdown popover, dynamic font/spacing binding, and deep CSS overrides.
  - `frontend/pages/library.vue`: Streamlined amber callout message below Category selector.
  - `frontend/i18n/locales/en.json` & `frontend/i18n/locales/vi.json`: Updated localization strings for `library.verbatim_category_hint` and typography controls.
- **Operations:**
  - Zero database schema migrations required (pure client-side state and backend prompt/handler enhancements).
  - Validation via `openspec validate --strict ai-structural-restoration-and-today-typography`.
