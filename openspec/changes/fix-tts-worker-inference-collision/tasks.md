# Tasks: Fix TTS Worker Inference Collision and Duplicate Synthesis on Engine Switch

## 1. Web Worker Inference Serialization & Cancellation

- [x] 1.1 Add `inferenceQueue: Promise<unknown>` mutex chain to `frontend/workers/ttsSynth.worker.ts` to ensure non-overlapping calls to `entry.synth()` across asynchronous worker tasks.
- [x] 1.2 Implement monotonic `currentJobId` tracking in `ttsSynth.worker.ts` to discard stale sentences immediately if a new `synth` or `cancel` message is received.
- [x] 1.3 Validate `cancelledReqIds` and `jobId === currentJobId` both prior to sentence inference and prior to posting audio chunk data to the main thread.

## 2. Composable Cancellation & Worker Lifecycle Recovery

- [x] 2.1 Unconditionally invoke `cancelWorkerSynthesis()` at the entry of `loadAndPlay()` in `frontend/composables/useSliceAudio.ts` to clear in-flight worker requests when loading audio.
- [x] 2.2 Nullify cached `worker` instance reference on unhandled error in `worker.onerror` within `createWorkerEngine()` to enable fresh worker recreation.
- [x] 2.3 Ensure pending partial audio chunks are persisted via `await cache.savePartial()` when synthesis is cancelled to maintain IndexedDB cache fidelity.

## 3. UI Action Debouncing & Guard

- [x] 3.1 Guard `onToggle()` in `frontend/components/reader/ReaderAudioPlayer.vue` to return immediately if `isLoading.value` is true, preventing duplicate concurrent synthesis dispatch.

## 4. Verification & Testing

- [x] 4.1 Add regression unit tests in `frontend/tests/components/reader/ReaderAudioPlayer.spec.ts` verifying that `onToggle` does not call `loadAndPlay` when `isLoading` is true.
- [x] 4.2 Add regression unit tests in `frontend/tests/composables/useSliceAudio.spec.ts` verifying that `loadAndPlay` cancels existing synthesis requests before starting.
- [x] 4.3 Run Gate 1 verification: execute full test suites (`npm test` and `dotnet test`).
- [x] 4.4 Run Gate 2 visual and flow verification: execute headless Chromium browser flow at `/playground/audio-narration` for Desktop (1440x900) and Mobile (390x844).
