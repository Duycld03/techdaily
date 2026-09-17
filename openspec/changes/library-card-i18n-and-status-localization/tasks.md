# Tasks: Library Card i18n, Ingestion Status Localization, and Import Modal Parity

## 1. Locale Dictionary Expansion (Bilingual Parity)

- [x] 1.1 In `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`, add import modal keys (`pdf_replace_hint`, `title_placeholder`, `content_placeholder`, `pdf_service_note_1`, `pdf_service_note_2`) and verify valid JSON syntax.
- [x] 1.2 In `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`, add background ingestion status keys (`status_uploaded_queued`, `status_remote_download`, `status_analyzing_structure`, `status_persisting_slices`, `status_curating_slice`, `status_analyzing_pages`, `status_extracting_topic`, `status_extracted_complete`, `status_ready`, `status_parsing_pages`, `status_generating_chunks`) and verify bilingual parity.

## 2. Book Card Category Badge & Ingestion Status Localization

- [x] 2.1 In `frontend/pages/library.vue`, implement `getCategoryLabel(category: number | string | undefined | null): string` supporting string enums (`"EngineeringCraft"`, `"BackendDotNet"`, `"DatabaseStorage"`, `"FrontendWeb"`, `"SystemDesign"`), lowercase aliases, and numeric IDs (`0..4`).
- [x] 2.2 In `frontend/pages/library.vue`, update the book card template category badge from `categories.find((c) => c.id === book.category)?.label || 'Engineering'` to `getCategoryLabel(book.category)`.
- [x] 2.3 In `frontend/pages/library.vue`, implement `getStatusMessage(book: Book): string` using regex pattern matching to map raw backend worker messages into localized strings with dynamic parameter interpolation.
- [x] 2.4 In `frontend/pages/library.vue`, update the processing ingestion banner template to bind `<span class="truncate">{{ getStatusMessage(book) }}</span>`.
- [x] 2.5 In `frontend/pages/library.vue`, remove the redundant `<span class="text-xs text-slate-400 font-medium truncate hidden sm:inline">GitBook Reader</span>` element from the card footer.

## 3. Import Modal Placeholders & Service Notes Localization

- [x] 3.1 In `frontend/pages/library.vue`, update the Markdown series content textarea placeholder attribute to `:placeholder="$t('library.content_placeholder')"`.
- [x] 3.2 In `frontend/pages/library.vue`, update the PDF upload dropzone file replace hint text to `{{ $t('library.pdf_replace_hint') }}`.
- [x] 3.3 In `frontend/pages/library.vue`, update the optional PDF title input placeholder attribute to `:placeholder="$t('library.title_placeholder')"`.
- [x] 3.4 In `frontend/pages/library.vue`, update the technical architecture service note spans in the PDF upload tab to `{{ $t('library.pdf_service_note_1') }}` and `{{ $t('library.pdf_service_note_2') }}`.

## 4. Test Suite Updates & Validation

- [x] 4.1 In `frontend/tests/pages/library.spec.ts`, add test cases verifying `getCategoryLabel` maps string enums (`"EngineeringCraft"`, `"BackendDotNet"`, `"DatabaseStorage"`, `"FrontendWeb"`, `"SystemDesign"`) and numeric IDs (`0..4`) to expected translation keys without falling back to `'Engineering'`.
- [x] 4.2 In `frontend/tests/pages/library.spec.ts`, add test cases verifying `getStatusMessage` properly formats static status messages and interpolates dynamic numbers for `AI is curating initial slice {current}/{total}...`.
- [x] 4.3 In `frontend/tests/pages/library.spec.ts`, assert that the hardcoded text `"GitBook Reader"` is not present in the rendered book card footer.
- [x] 4.4 In `frontend/tests/pages/library.spec.ts`, assert that import modal inputs and service notes bind to their respective localized strings.
- [x] 4.5 Execute `npm --prefix frontend test` to verify that all frontend tests pass cleanly.
- [x] 4.6 Run `openspec validate --strict library-card-i18n-and-status-localization` to verify change proposal compliance with OpenSpec standards.
