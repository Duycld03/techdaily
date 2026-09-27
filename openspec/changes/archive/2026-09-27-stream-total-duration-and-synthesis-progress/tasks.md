# Tasks

## 1. Engine & Cache Invariants (`sliceAudioCache.ts` & `useSliceAudio.ts`)

- [x] 1.1 In `frontend/utils/sliceAudioCache.ts`, bump `buildAudioKey` to version `v2` (`${chunkId}::${voice}::v2::${contentHash}`) to invalidate any stale or prematurely cached partial audios.
- [x] 1.2 In `frontend/composables/useSliceAudio.ts`, ensure `createWorkerEngine().cancel()` rejects with `new Error('Synthesis cancelled')` instead of resolving, preventing cancellation from falling through to full cache save.
- [x] 1.3 In `frontend/composables/useSliceAudio.ts`, guard full cache save (`cache.set`) so it strictly executes ONLY when all sentences are synthesized (`buffers.length === sentences.length`), preserving partial progress in IndexedDB on cancellation.

## 2. Frontend Player Presentation (`ReaderAudioPlayer.vue`)

- [x] 2.1 In `frontend/components/reader/ReaderAudioPlayer.vue`, compute `estimatedTotalDuration` based on sentence progress during on-device streaming and display it alongside the currently buffered duration (e.g. `2:08 (~6:40)`).
- [x] 2.2 In `frontend/components/reader/ReaderAudioPlayer.vue`, update `onToggle()` so clicking "Listen" when audio is already loaded and ready (`loadedId === source.chunkId && status === 'ready'`) calls `play()` directly, allowing background worker synthesis to continue streaming without being restarted.

## 3. Testing & Dual-Gate Verification

- [x] 3.1 Update unit tests in `frontend/tests/components/reader/ReaderAudioPlayer.spec.ts` asserting that estimated total duration is displayed during on-device streaming and that clicking "Listen" on a loaded slice invokes `play()` directly.
- [x] 3.2 Update unit tests in `frontend/tests/composables/useSliceAudio.spec.ts` and `frontend/tests/utils/sliceAudioCache.spec.ts` for cache key v2, cancellation rejection, and partial cache preservation.
- [x] 3.3 Execute Gate 1 automated testing: verify 100% pass rate on `npm test` and `dotnet test`.
- [x] 3.4 Execute Gate 2 visual verification: drive headless Chromium via `browser` in `eval` to verify estimated duration display and progress indicator on Desktop (1440x900) and Mobile (390x844).
