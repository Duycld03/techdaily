# Tasks

## 1. Immersive Reader Viewport Standardization

- [x] 1.1 Update `pages/read/[bookId].vue` and `DocReaderPane.vue` to replace `h-screen` with `h-dvh`
- [x] 1.2 Audit Table of Contents mobile drawer for smooth slide-over, backdrop dismiss, and safe area padding

## 2. Typography Composable Event Hygiene

- [x] 2.1 Refactor `useReaderTypography.ts` to replace manual `window.addEventListener('storage')` with VueUse `useEventListener`
- [x] 2.2 Audit typography control popover in reader header for responsive button wrapping on 320px screens

## 3. Library & Notes Studio Audit

- [x] 3.1 Audit `pages/library.vue` and book upload modal for `max-h-[85dvh]` mobile scrolling and touch button targets
- [x] 3.2 Audit `pages/notes.vue` notes list and filter chips for mobile responsive layout
- [x] 3.3 Verify unit test suite passes for reader and library pages via `npm test`
