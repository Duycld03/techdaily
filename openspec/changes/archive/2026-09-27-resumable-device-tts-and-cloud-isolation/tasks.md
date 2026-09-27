# Tasks

## 1. Web Worker Task Cancellation & Engine Isolation

- [x] 1.1 Update `frontend/workers/ttsSynth.worker.ts` to handle `{ type: 'cancel', reqId }` messages and check cancellation flags between sentence inference loops.
- [x] 1.2 Update `frontend/composables/useSliceAudio.ts` to expose `cancel` on `TtsEngine` and actively cancel in-flight worker synthesis when switching to Cloud engine or starting Cloud playback.
- [x] 1.3 Update `frontend/components/reader/ReaderAudioPlayer.vue` to strictly gate the synthesis progress badge behind `engineMode === 'device' && synthTotal > 0 && synthIndex > 0 && synthIndex < synthTotal`.

## 2. Partial Chunk Caching & Resumable Synthesis

- [x] 2.1 Extend `SliceAudioCache` in `frontend/composables/useSliceAudio.ts` with `getPartial`, `savePartial`, and `deletePartial` methods for IndexedDB persistence.
- [x] 2.2 Update `synthesizeOnDevice` in `frontend/composables/useSliceAudio.ts` to check for cached partial chunks matching `(chunkId, voice, contentHash)` and resume synthesis from sentence $K$.
- [x] 2.3 Persist intermediate chunks to the partial cache as each sentence finishes, assemble full slice audio on completion, and clean up the partial cache.
- [x] 2.4 Verify content hash validation: ensure slice text changes produce a cache miss and trigger fresh synthesis from sentence 0.

## 3. Testing & Verification

- [x] 3.1 Add unit tests in `frontend/tests/composables/useSliceAudio.spec.ts` verifying partial chunk restoration, hash invalidation, and worker cancellation.
- [x] 3.2 Add unit tests in `frontend/tests/components/reader/ReaderAudioPlayer.spec.ts` verifying synthesis badges are strictly hidden during Cloud mode and visible in Device mode.
- [x] 3.3 Run full frontend test suite (`npm test`) and backend test suite (`dotnet test`) ensuring 100% pass rate.
