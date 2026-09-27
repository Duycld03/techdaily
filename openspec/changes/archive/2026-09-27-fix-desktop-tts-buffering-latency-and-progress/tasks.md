# Tasks

## 1. Frontend Audio Composable & Worker Engine

- [x] 1.1 In `frontend/composables/useSliceAudio.ts`, cap `targetBufferCount` to `Math.min(2, sentences.length)` to ensure low-latency audio startup within 2 sentences (~10-15s audio headroom) instead of waiting for 33% of the entire slice.
- [x] 1.2 In `frontend/workers/ttsSynth.worker.ts`, refine `isMobile` detection to avoid misclassifying desktop touchscreen laptops as mobile devices, and allow desktop multi-threaded WASM execution to allocate up to 8 CPU threads (`Math.min(8, Math.max(1, navigator.hardwareConcurrency))`) when cross-origin isolated.

## 2. Frontend Player Presentation & State Precedence

- [x] 2.1 In `frontend/components/reader/ReaderAudioPlayer.vue`, destructure `device` from `useSliceAudio()` to eliminate Vue template warnings and restore the reactive compute device badge (`[GPU]` / `[CPU]`).
- [x] 2.2 In `frontend/components/reader/ReaderAudioPlayer.vue`, reorder `statusLabel` conditions so active model download progress (`downloadProgress > 0 && downloadProgress < 100`) takes visual precedence over the sentence buffering counter (`0/N`).
- [x] 2.3 Verify and ensure cross-origin isolation headers (`Cross-Origin-Opener-Policy: same-origin`, `Cross-Origin-Embedder-Policy: credentialless`) are consistently available across reader views to prevent single-threaded WASM fallback during SPA navigation.

## 3. Testing & Visual Verification

- [x] 3.1 Update unit tests in `frontend/tests/composables/useSliceAudio.spec.ts` asserting that `targetBufferCount` is capped at 2 for multi-sentence slices and matches sentence count for 1-sentence slices.
- [x] 3.2 Update unit tests in `frontend/tests/components/reader/ReaderAudioPlayer.spec.ts` asserting that model download progress takes precedence over buffering and verifying device badge rendering.
- [x] 3.3 Execute Gate 1 automated testing: verify 100% pass rate on `npm test` and `dotnet test`.
- [x] 3.4 Execute Gate 2 visual verification: drive headless Chromium via `browser` in `eval` to verify download progress presentation, buffering startup, and device badge rendering on Desktop (1440x900) and Mobile (390x844).
