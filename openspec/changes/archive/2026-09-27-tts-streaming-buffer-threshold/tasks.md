# Tasks

## 1. Frontend Pre-Roll Playback Buffer Threshold

- [x] 1.1 Update `frontend/composables/useSliceAudio.ts` to compute an adaptive pre-roll buffer target (`sentences.length <= 3 ? sentences.length : Math.max(2, Math.ceil(sentences.length / 3))`).
- [x] 1.2 Concatenate and play the initial pre-roll audio block (`concatFloat32(buffers.slice(0, targetBufferCount))`) when `buffers.length === targetBufferCount` instead of triggering on chunk 0.
- [x] 1.3 Advance sequential chunk playback for subsequent sentences starting at index `targetBufferCount` on `'ended'` events without stuttering or restarting.

## 2. Frontend Player Status Presentation

- [x] 2.1 Update `frontend/components/reader/ReaderAudioPlayer.vue` to show buffering status while `synthIndex < targetBufferCount` before audio playback begins.

## 3. Testing & Verification

- [x] 3.1 Update unit tests in `frontend/tests/composables/useSliceAudio.spec.ts` verifying that slices buffer $\ge 33\%$ of sentences before playback begins and short slices buffer 100%.
- [x] 3.2 Run full frontend test suite (`npm test`) to ensure 100% test pass rate and absence of regressions.
