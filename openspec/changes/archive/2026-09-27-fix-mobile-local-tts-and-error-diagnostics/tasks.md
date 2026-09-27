# Tasks

## 1. Web Worker Mobile Resource Safeguards & Fallbacks
- [x] 1.1 Implement mobile environment detection (`isMobile`) in `frontend/workers/ttsSynth.worker.ts` checking user-agent and mobile touch capabilities.
- [x] 1.2 Restrict ONNX Runtime WASM execution to single-thread (`onnxWasm.numThreads = 1`) on mobile devices regardless of `isIsolated` status.
- [x] 1.3 Add mobile quantized model weight loading (`dtype: 'q8'` for mobile WASM) to reduce memory overhead from 109 MB to 36.6 MB.
- [x] 1.4 Fix promise awaiting in `buildForDevice` catch block (`return await buildEntry(...)`) to prevent unhandled rejection leaks.

## 2. Audio Composable Error Diagnostics & Propagation

- [x] 2.1 Refactor error handling in `frontend/composables/useSliceAudio.ts` to capture specific failure messages and categorize error reasons (`DEVICE_INIT_FAILED`, `DEVICE_OOM`, `NETWORK_ERROR`).
- [x] 2.2 Expose structured error information from `useSliceAudio.ts` to allow consumer components to present contextual diagnosis and recovery actions.

## 3. UI Error Presentation & 1-Tap Cloud Recovery

- [x] 3.1 Update `frontend/components/reader/ReaderAudioPlayer.vue` to display diagnostic error messages instead of opaque generic text.
- [x] 3.2 Provide a 1-tap action in `ReaderAudioPlayer.vue` to switch to Google Cloud TTS when on-device synthesis fails on mobile or due to hardware limits.
- [x] 3.3 Add localized translation keys in `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json` for mobile error context and Cloud fallback suggestion.

## 4. Testing & Verification

- [x] 4.1 Update and add unit tests in `frontend/tests/composables/useSliceAudio.spec.ts` for error diagnostics and Cloud fallback handling.
- [x] 4.2 Update and add unit tests in `frontend/tests/components/reader/ReaderAudioPlayer.spec.ts` for error banner/action display and 1-tap engine switch.
- [x] 4.3 Run full test suites (`npm test` and `dotnet test`) to verify 100% test pass rate.
- [x] 4.4 Run headless browser visual verification on Desktop (1440x900) and Mobile (390x844) viewports.
