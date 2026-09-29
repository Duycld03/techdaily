# Proposal: Fix On-Device TTS Narration on Windows and Mobile

## Why

When users attempt on-device narration (`engine === 'device'`) on Windows desktop machines or modern mobile browsers (such as Android Chrome with WebGPU or iOS Safari with network constraints), the reader immediately fails with the message *"Giọng đọc thiết bị không khả dụng trên thiết bị này. Hãy thử Google Cloud."* (`audio_error_device`).

This failure occurs because:
1. `pickDevice()` in `ttsSynth.worker.ts` unconditionally probes WebGPU and attempts execution whenever `navigator.gpu` returns an adapter (which is true on modern Windows with discrete/integrated GPUs and Android Chrome 121+).
2. The MMS-TTS model (Meta VITS architecture) uses 64-bit integer (`INT64`) indexing in `GatherND` within its stochastic duration predictor, which ONNX Runtime Web's WebGPU execution provider cannot execute (unsupported data type 7).
3. The `fp16` variant (`model_fp16.onnx`) hosted on Hugging Face contains an invalid graph schema (`RandomNormalLike` output type mismatch), triggering an immediate session build failure and poisoning the Transformers.js model cache.
4. `useSliceAudio.ts` swallows raw worker error messages into a blanket `DEVICE_INIT_FAILED` code without console diagnostics, while misclassifying WebKit/Safari network load errors (`TypeError: Load failed`) as device hardware failures.

Locking the MMS-TTS architecture to CPU (WASM) with backend-appropriate precision (`fp32` on desktop, `q8` on mobile) and refining error diagnostics restores reliable client-side speech synthesis across all platforms.

## What Changes

- **Lock MMS-TTS to CPU WASM**: Bypass WebGPU probing and execution for MMS-TTS models (`Xenova/mms-tts-*`), ensuring all synthesis executes reliably via `ort-wasm-simd-threaded` across desktop and mobile.
- **Prevent `fp16` Model Loading**: Eliminate `fp16` requests for MMS-TTS models to avoid the corrupted Hugging Face ONNX graph and prevent promise rejection cache poisoning in `@huggingface/transformers`.
- **Preserve Desktop and Mobile Precision Separation**: Maintain multi-threaded `fp32` for desktop CPUs (highest audio fidelity and speed) and single-threaded `q8` (`model_quantized.onnx`, ~36.6 MB) for mobile devices (memory bounds and tab stability).
- **Expand Error Diagnostics & Network Classification**:
  - Add explicit `console.error` diagnostics in `useSliceAudio.ts` for worker errors and lifecycle events.
  - Classify WebKit/Safari `"Load failed"` and related resource fetch errors as `NETWORK_ERROR` rather than `DEVICE_INIT_FAILED`.
  - Provide actionable error messaging and clear retry/fallback options in `ReaderAudioPlayer.vue`.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `audio-narration`: Update on-device execution requirements to lock MMS-TTS models to CPU (WASM), disallow broken `fp16` graph loading, refine mobile error classification (including WebKit network error handling), and guarantee detailed error logging.

## Impact

- **Frontend**:
  - `frontend/workers/ttsSynth.worker.ts`: Execution device selection and precision loading.
  - `frontend/composables/useSliceAudio.ts`: Error categorization and diagnostics logging.
  - `frontend/components/reader/ReaderAudioPlayer.vue`: Error messaging and fallback display.
- **Dependencies**: No new npm dependencies. Uses existing `@huggingface/transformers` v3/v4 and ONNX Runtime Web WASM binaries.
- **Performance**: Synthesis on CPU WASM with SIMD threading takes ~1.5–3.5s per sentence on desktop and ~2–4s on mobile (`q8`), avoiding runtime crashes and WebGPU shader recompilations.
- **Compatibility**: Fixes on-device audio playback on Windows (NVIDIA/AMD/Intel), Android Chrome, macOS, and iOS Safari.
