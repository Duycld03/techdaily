# Proposal: Library Card i18n, Ingestion Status Localization, and Import Modal Parity

## Why

When switching the application language to Vietnamese (`vi`), users browsing the technical library at `/library` (`frontend/pages/library.vue`) encounter several glaring internationalization defects and untranslated English strings:

1. **Broken Category Badges on Book Cards:**
   - Every book card displays a hardcoded fallback badge `Engineering` in English instead of the localized category (`Tư Duy Kỹ Sư & Năng Suất`, `Backend & Phân Tán`, `Cơ Sở Dữ Liệu`, `Frontend & Web`, `Thiết Kế Hệ Thống`).
   - Root Cause: `categories.find((c) => c.id === book.category)?.label || 'Engineering'` fails because `c.id` is numeric (`0..4`), whereas the backend serializes `book.category` as enum strings (`"EngineeringCraft"`, `"BackendDotNet"`, `"DatabaseStorage"`, `"FrontendWeb"`, `"SystemDesign"`) via .NET's `JsonStringEnumConverter`. Consequently, `find` always yields `undefined` and falls back to the English string `'Engineering'`.

2. **Raw English Ingestion Status Messages:**
   - When a book is being processed in the background, `book.statusMessage` renders raw English worker strings (e.g. `"File uploaded, queued for processing..."`, `"Analyzing document structure and bookmarks..."`, `"AI is curating initial slice 1/12..."`, `"Parsing pages..."`, `"Generating chunks..."`).
   - These technical worker messages bypass the client-side localization system, breaking immersion for Vietnamese users during asynchronous document ingestion.

3. **Truncated Untranslated Label `GitBook Rea...`:**
   - In the card footer, `<span class="text-xs text-slate-400 font-medium truncate hidden sm:inline">GitBook Reader</span>` is hardcoded in English and truncates to `GitBook Rea...` on standard display widths.
   - The label is redundant beside the primary reading CTA ("Read Slices" / "Continue Reading") and clutters the card action row.

4. **Untranslated Import Modal Text & Placeholders:**
   - The document import modal contains multiple hardcoded English strings:
     - Dropzone file replace hint: `"Click or drop another file to replace"`
     - Optional title input placeholder: `"Optional: Auto-extracted from PDF if blank"`
     - Markdown content textarea placeholder: `"Paste Markdown document with # and ## headers here..."`
     - Technical service architecture notes: `"300 MB Streaming • Background Service"` and `"Look-Ahead Buffer Synthesis"`
   - These strings lack keys in `frontend/i18n/locales/vi.json` and `frontend/i18n/locales/en.json`.

Addressing these defects achieves 100% bilingual parity between Vietnamese and English across the entire `/library` experience, eliminates truncation defects, and ensures robust handling of both string and numeric category data formats.

---

## What Changes

We propose targeted frontend localization and template refactoring:

1. **Robust Category Label Resolver (`getCategoryLabel`):**
   - Implement `getCategoryLabel(category: number | string | undefined | null): string` in `frontend/pages/library.vue`.
   - Normalizes both backend string enum identifiers (`"EngineeringCraft"`, `"BackendDotNet"`, `"DatabaseStorage"`, `"FrontendWeb"`, `"SystemDesign"`) and integer identifiers (`0..4`), returning the corresponding `$t('library.categories.<key>')` translated string.
   - Updates the card template badge from `categories.find((c) => c.id === book.category)?.label || 'Engineering'` to `getCategoryLabel(book.category)`.

2. **Dynamic Ingestion Status Translator (`getStatusMessage`):**
   - Implement `getStatusMessage(book: Book): string` in `frontend/pages/library.vue`.
   - Uses regex pattern matching to map fixed and parameterized backend worker status strings into localized messages:
     - Fixed states: uploaded/queued, remote download, structure analysis, chapter persistence, parsing pages, generating chunks, ready.
     - Dynamic parameterized states: slice curation (`AI is curating initial slice {current}/{total}...`), page analysis (`Analyzing content: page {current}/{total}...`), topic extraction (`Extracting topic: {topic}`), and slice completion (`Extracted {count} slices. Complete!`).
   - Falls back gracefully to the raw message or generic `$t('library.processing_pdf')` if unrecognized.

3. **Removal of Redundant Brand Label:**
   - Remove `<span class="text-xs text-slate-400 font-medium truncate hidden sm:inline">GitBook Reader</span>` from the book card footer in `frontend/pages/library.vue`.
   - Eliminates layout truncation and cleans up the action button group (Delete, Export, Read CTA).

4. **Import Modal i18n Key Integration:**
   - Replace hardcoded English strings in the import modal template with `$t` bindings:
     - `"Click or drop another file to replace"` $\to$ `$t('library.pdf_replace_hint')`
     - `"Optional: Auto-extracted from PDF if blank"` $\to$ `$t('library.title_placeholder')`
     - `"Paste Markdown document with # and ## headers here..."` $\to$ `$t('library.content_placeholder')`
     - `"300 MB Streaming • Background Service"` $\to$ `$t('library.pdf_service_note_1')`
     - `"Look-Ahead Buffer Synthesis"` $\to$ `$t('library.pdf_service_note_2')`

5. **Locale Dictionary Parity:**
   - Add all new keys to both `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json` with accurate technical translations.

6. **Automated Unit & Integration Tests:**
   - Add comprehensive tests in `frontend/tests/pages/library.spec.ts` verifying category label resolution, ingestion status translation, absence of the truncated label, and localized modal placeholders.

---

## Capabilities

### Modified Capabilities

- `library`: Extends the technical library specification to mandate localized category badges on book cards across both string and numeric enum representations, dynamic localization of background ingestion status messages, removal of redundant reader brand labels, and 100% bilingual parity for import modal placeholders and service notes.

---

## Impact

- **Frontend Application (`frontend/pages/library.vue`):** Updated template bindings, new helper functions (`getCategoryLabel`, `getStatusMessage`), and removal of the hardcoded GitBook Reader span.
- **Frontend Locales (`frontend/i18n/locales/en.json`, `frontend/i18n/locales/vi.json`):** New translation keys for status messages, modal placeholders, and architecture notes.
- **Frontend Tests (`frontend/tests/pages/library.spec.ts`):** New assertions verifying category badge resolution, status message translation, and modal placeholder rendering.
- **Backend & APIs:** Zero changes required. The backend API contracts, serialization configurations, and worker pipelines remain untouched.
