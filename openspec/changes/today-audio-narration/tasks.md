# Tasks

## 1. Frontend Store Contracts

- [x] 1.1 In `frontend/stores/useDailyFocusStore.ts`, declare `isAiFormatted?: boolean` within the `DocumentChunk` interface to align with the backend `DocumentChunkDto.IsAiFormatted` contract and satisfy `ReaderAudioPlayer`'s `ChunkSummary` interface requirements.

## 2. Frontend Component Integration

- [x] 2.1 In `frontend/components/today/DocReaderPane.vue`, import `ReaderAudioPlayer` from `~/components/reader/ReaderAudioPlayer.vue`.
- [x] 2.2 In `frontend/components/today/DocReaderPane.vue`, mount `<ReaderAudioPlayer :chunk="chunk" />` in an un-scrolled header container directly beneath the chapter title heading (`h1`) and above the executive summary callout.
- [x] 2.3 Verify responsive spacing, border hairline treatments, and visual rhythm of the audio container within `DocReaderPane.vue` across both dark and light modes.

## 3. Frontend Testing & Type Safety

- [x] 3.1 In `frontend/tests/components/DocReaderPane.spec.ts`, update `mockDocumentChunk` and add unit test coverage asserting that `ReaderAudioPlayer` renders when `isAiFormatted: true` and is conditionally gated when `isAiFormatted: false`.
- [x] 3.2 Execute `npm test` within `frontend/` to verify 100% of Vitest test suites pass with zero regressions.
- [x] 3.3 Execute `npx tsc --noEmit` in `frontend/` to guarantee clean TypeScript type-checking across all components and stores.

## 4. Verification & Visual Inspection

- [x] 4.1 Drive automated headless Chromium via the `browser` tool in `eval` on Desktop (`1440x900`) and Mobile (`390x844`) viewports to verify visual layout integrity of `ReaderAudioPlayer` inside the `/today` daily focus workspace.
- [x] 4.2 Validate that the player controls (Play/Pause, Cloud vs Device engine toggle, Speed selector, Voice dropdown, and Progress scrub) render with proper layout boundaries without text truncation across both English and Vietnamese locales.
