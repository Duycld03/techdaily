## 1. Shell & Layout Isolation

- [x] 1.1 In `frontend/app.vue`, compute `isReaderMode` from `route.path.startsWith('/read')` to conditionally hide `AppHeader` and `AppSidebar`, and verify via `npm test`.

## 2. Reader Header & Mobile Navigation

- [x] 2.1 Refactor reader top bar in `frontend/pages/read/[bookId].vue` into a unified compact responsive header with `< Library`, book title, slice progress, mobile drawer toggle, theme toggle, and quiz action.
- [x] 2.2 Enhance mobile Table of Contents drawer in `frontend/pages/read/[bookId].vue` with smooth slide-over, backdrop dismiss, and instant closure on chapter selection.
- [x] 2.3 Redesign slice bottom navigation in `frontend/pages/read/[bookId].vue` with a full-width, thumb-friendly next slice card on mobile viewports.

## 3. Markdown Formatting & Typography

- [x] 3.1 Implement heading deduplication in `frontend/pages/read/[bookId].vue` to strip duplicate initial headings matching `ChapterTitle` before markdown rendering.
- [x] 3.2 In `frontend/composables/useMarkdownRenderer.ts`, fix `window.__copyCode` variable scoping bug by extracting and decoding `data-code` from the clicked button.
- [x] 3.3 Add `prose-code:before:content-none prose-code:after:content-none` and style inline code badges cleanly in `frontend/pages/read/[bookId].vue`.
- [x] 3.4 Sanitize indented blocks and metadata artifacts in markdown rendering so accidental whitespace does not create spurious code fences.

## 4. Terminology & Localization Parity

- [x] 4.1 Update `frontend/i18n/locales/en.json` and `vi.json` to provide accurate slice badge terminology ("Slice X of Y" / "Lát cắt X / Y") and mobile navigation labels.
- [x] 4.2 Verify slice badge and navigation labels across English and Vietnamese locales.

## 5. Verification & Tests

- [x] 5.1 Run frontend test suite (`npm test`) and verify all tests pass without errors.
- [x] 5.2 Validate layout ergonomics on both desktop and mobile viewports.
