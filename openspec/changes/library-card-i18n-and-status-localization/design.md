# Design: Library Card i18n, Ingestion Status Localization, and Import Modal Parity

## Context

See `proposal.md` for user-reported defects and motivation.

TechDaily uses Nuxt 4, Vue 3, Pinia, and `@nuxtjs/i18n` with locale JSON files at `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`.
On the backend, ASP.NET Core (.NET 10) serializes domain enums such as `Category` (`FrontendWeb`, `BackendDotNet`, `DatabaseStorage`, `SystemDesign`, `EngineeringCraft`) as string values using `System.Text.Json.Serialization.JsonStringEnumConverter` in `Program.cs`.

In `frontend/pages/library.vue`, `categories` is a computed array mapping numeric IDs (`0..4`) to `$t('library.categories.<key>')`. When books are fetched from `/api/v1/library/books`, `book.category` contains string enum values from the API (e.g. `"EngineeringCraft"`), while local mock fixtures or legacy client types might assign numeric integers. Because `categories.find((c) => c.id === book.category)` performs strict equality, the lookup fails and falls back to the English string `'Engineering'`.

Furthermore, asynchronous background ingestion worker steps update `book.statusMessage` with English technical diagnostics (`"File uploaded, queued for processing..."`, `"AI is curating initial slice 1/12..."`), which are currently rendered directly to the user interface. The book card footer also includes an outdated, hardcoded `<span ...>GitBook Reader</span>` label that truncates, and the import modal contains unlocalized placeholder attributes and architecture notes.

---

## Goals / Non-Goals

**Goals:**
- Provide 100% bilingual parity (`en` and `vi`) across all visual elements in `/library`, including category badges, background status messages, import modal placeholders, and architecture notes.
- Support both string enum identifiers and numeric integers in `getCategoryLabel`, guaranteeing that category badges resolve accurately regardless of API serialization format or test mock conventions.
- Implement parameterized status message localization with regular expressions to translate dynamic backend worker updates into natural Vietnamese and English.
- Eliminate visual clutter and truncation by removing the redundant `"GitBook Reader"` badge from the card footer.
- Maintain comprehensive unit tests covering all category and status translation permutations.

**Non-Goals:**
- Modifying backend C# models, API endpoints, or database schemas. Status localization is strictly handled at the frontend presentation layer.
- Changing the existing category taxonomy or adding new categories beyond the established 5 categories (`FrontendWeb`, `BackendDotNet`, `DatabaseStorage`, `SystemDesign`, `EngineeringCraft`).
- Altering the reading page (`/read/[bookId].vue`) or reader store.

---

## Decisions

### Decision 1: Client-Side Category Normalizer Helper (`getCategoryLabel`)
- **Choice:** Implement `getCategoryLabel(category: number | string | undefined | null): string` in `frontend/pages/library.vue`.
- **Rationale:** The helper normalizes input using a lookup map or switch statement handling:
  - String enums: `"FrontendWeb"`, `"BackendDotNet"`, `"DatabaseStorage"`, `"SystemDesign"`, `"EngineeringCraft"` (case-insensitive)
  - String aliases: `"frontend"`, `"backend"`, `"database"`, `"system_design"`, `"craft"`
  - Numeric IDs: `0`, `1`, `2`, `3`, `4`
  Each branch maps directly to `$t('library.categories.<key>')`.
- **Alternatives Considered:**
  1. *Change backend to serialize numeric enum integers:* Rejected. String enums are self-describing, human-readable in HTTP inspector/logs, and standard across all other TechDaily endpoints.
  2. *Inline ternary in the Vue template:* Rejected. An inline expression is unreadable, hard to test, and duplicate logic across templates.

```typescript
function getCategoryLabel(category: number | string | undefined | null): string {
  if (category === undefined || category === null) {
    return t('library.categories.craft')
  }

  const normalized = String(category).toLowerCase()
  switch (normalized) {
    case '0':
    case 'frontendweb':
    case 'frontend':
      return t('library.categories.frontend')
    case '1':
    case 'backenddotnet':
    case 'backend':
      return t('library.categories.backend')
    case '2':
    case 'databasestorage':
    case 'database':
      return t('library.categories.database')
    case '3':
    case 'systemdesign':
    case 'system_design':
      return t('library.categories.system_design')
    case '4':
    case 'engineeringcraft':
    case 'craft':
      return t('library.categories.craft')
    default:
      return categories.value.find((c) => String(c.id) === normalized)?.label || t('library.categories.craft')
  }
}
```

---

### Decision 2: Regex-Driven Status Message Translation (`getStatusMessage`)
- **Choice:** Implement `getStatusMessage(book: Book): string` utilizing regex pattern matching with capture groups.
- **Rationale:** Backend ingestion messages originate from `UploadPdfHandler`, `ImportRemotePdfHandler`, `PdfPigExtractor`, and `PdfIngestionWorker`. Several messages contain dynamic numbers:
  - `"AI is curating initial slice {current}/{total}..."` $\to$ regex `/ai is curating (?:initial )?slice (\d+)\/(\d+)/i`
  - `"Analyzing content: page {current}/{total}"` $\to$ regex `/analyzing content: page (\d+)\/(\d+)/i`
  - `"Extracting topic: {topic}"` $\to$ regex `/extracting topic:\s*(.+)/i`
  - `"Extracted {count} slices. Complete!"` $\to$ regex `/extracted (\d+) slices\. complete!/i`
  Static worker messages are matched with substring/case-insensitive regex tests:
  - `"File uploaded, queued for processing..."` $\to$ `library.status_uploaded_queued`
  - `"Remote PDF download initiated. Processing chapters..."` $\to$ `library.status_remote_download`
  - `"Analyzing document structure and bookmarks..."` $\to$ `library.status_analyzing_structure`
  - `"Persisting chapters and slices..."` $\to$ `library.status_persisting_slices`
  - `"Parsing pages..."` $\to$ `library.status_parsing_pages`
  - `"Generating chunks..."` $\to$ `library.status_generating_chunks`
  - `"Ready for reading"` $\to$ `library.status_ready`
  Unmatched messages fall back safely to `book.statusMessage || t('library.processing_pdf')`.
- **Alternatives Considered:**
  1. *Transmit localized messages directly from backend:* Rejected. Backend code and database entries in TechDaily strictly follow the 100% English guideline.
  2. *Introduce backend status codes (enum for every sub-step):* Over-engineered for progress reporting. Regex pattern matching on the client requires zero backend refactoring and adapts effortlessly to worker updates.

---

### Decision 3: Removal of Redundant "GitBook Reader" Span
- **Choice:** Remove `<span class="text-xs text-slate-400 font-medium truncate hidden sm:inline">GitBook Reader</span>` from line 446 in `frontend/pages/library.vue`.
- **Rationale:**
  - The label does not provide functional value; the user is already inside TechDaily.
  - Due to flexbox constraints with action buttons and the reading link, the label frequently clips to `"GitBook Rea..."`.
  - Removing it improves card breathing room and visual hierarchy.
- **Alternatives Considered:**
  - *Localize the label to "Trình đọc GitBook":* Rejected because the label remains redundant and still suffers truncation on narrow cards.

---

### Decision 4: Full Bilingual Parity for Import Modal & Placeholders
- **Choice:** Introduce dedicated keys in `en.json` and `vi.json` for all modal placeholders, dropzone replace hints, and service architecture notes.
- **Keys & Translations:**
  - `library.pdf_replace_hint`:
    - `en`: `"Click or drop another file to replace"`
    - `vi`: `"Bấm hoặc thả tệp khác vào đây để thay thế"`
  - `library.title_placeholder`:
    - `en`: `"Optional: Auto-extracted from PDF if blank"`
    - `vi`: `"Không bắt buộc: Tự động trích xuất từ PDF nếu để trống"`
  - `library.content_placeholder`:
    - `en`: `"Paste Markdown document with # and ## headers here..."`
    - `vi`: `"Dán tài liệu Markdown có tiêu đề # và ## vào đây..."`
  - `library.pdf_service_note_1`:
    - `en`: `"300 MB Streaming • Background Service"`
    - `vi`: `"Xử lý luồng 300 MB • Dịch vụ chạy ngầm"`
  - `library.pdf_service_note_2`:
    - `en`: `"Look-Ahead Buffer Synthesis"`
    - `vi`: `"Tổng hợp bộ đệm dự đoán trước"`

---

## Risks / Trade-offs

- **[Risk] New backend worker status strings bypass translation:** If a future backend change introduces a new status message not matching any regex in `getStatusMessage`, users would see the raw English string.
  - **Mitigation:** `getStatusMessage` includes a safe fallback returning `book.statusMessage || t('library.processing_pdf')`, ensuring progress is never hidden. New status strings can be added to the regex dictionary as needed.
- **[Risk] Category ID format variations across mock fixtures:** Different tests or store actions might provide `category` as numbers, strings, or null.
  - **Mitigation:** `getCategoryLabel` coerces the input to string and performs case-insensitive normalization before looking up the translation, with a safe fallback to `t('library.categories.craft')`.

---

## Migration Plan

1. Update `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json` with new keys.
2. Refactor `frontend/pages/library.vue` to add `getCategoryLabel` and `getStatusMessage`, update card template bindings, remove the `GitBook Reader` span, and bind modal placeholders/notes.
3. Update `frontend/tests/pages/library.spec.ts` with test coverage.
4. Run test suites locally via `npm --prefix frontend test`.
5. Rollback strategy: Revert Git commit on `frontend/pages/library.vue` and locale files; zero database or backend impact.
