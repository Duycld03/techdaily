# Tasks

## 1. Frontend: On-Device TTS Worker Configuration

- [x] 1.1 In `frontend/workers/ttsSynth.worker.ts`, bypass WebGPU probing and lock device resolution to `'wasm'` for MMS-TTS voice models to prevent INT64 GatherND shader failures.
- [x] 1.2 Remove `fp16` dtype requests for MMS-TTS in `frontend/workers/ttsSynth.worker.ts`, mapping desktop directly to `fp32` (multi-threaded SIMD) and mobile to `q8` (quantized 8-bit) without fallback loops.
- [x] 1.3 Simplify pipeline construction and error handling in `frontend/workers/ttsSynth.worker.ts` to construct and cache entries directly on WASM, eliminating promise rejection cache poisoning.

## 2. Frontend: Diagnostics and Error Classification in useSliceAudio

- [x] 2.1 In `frontend/composables/useSliceAudio.ts`, add detailed `console.error('[useSliceAudio] Device TTS Error:', ...)` logging in `worker.onerror` and `worker.onmessage` (`type === 'error'`).
- [x] 2.2 Expand `categorizeAudioError` in `frontend/composables/useSliceAudio.ts` to classify WebKit/Safari `"load failed"`, network timeouts, and resource fetch rejections under `NETWORK_ERROR` rather than `DEVICE_INIT_FAILED`.
- [x] 2.3 Ensure `AudioErrorInfo` retains the full `rawMessage` context and exposes appropriate `suggestCloudFallback` flags for actionable user guidance.

## 3. Frontend: Player UI and User Guidance

- [x] 3.1 In `frontend/components/reader/ReaderAudioPlayer.vue`, ensure error display accurately differentiates network download failures from device memory or hardware constraints.
- [x] 3.2 Verify that the 1-tap Google Cloud fallback button (`canFallbackToCloud`) cleanly switches engine mode and triggers synthesis when device TTS encounters errors.

## 4. Frontend: Automated Tests and Verification

- [x] 4.1 Update or add unit tests in `frontend/tests/composables/useSliceAudio.spec.ts` covering error classification for network load failures, memory errors, and worker initialization errors.
- [x] 4.2 Verify existing reader audio player tests in `frontend/tests/components/reader/ReaderAudioPlayer.spec.ts` pass without regressions.
- [x] 4.3 Run `npm test` across the full frontend test suite and execute headless browser verification on desktop and mobile viewports.
