# Design: Fix On-Device TTS Narration on Windows and Mobile

## Context

See `proposal.md` - Why.

The current on-device TTS implementation in `frontend/workers/ttsSynth.worker.ts` automatically attempts to use WebGPU whenever `navigator.gpu` returns an adapter. While WebGPU is supported on modern desktop browsers (Windows, Linux, macOS) and mobile browsers (Android Chrome 121+), Facebook Meta's MMS-TTS model (VITS architecture) contains a stochastic duration predictor with a `GatherND` operator indexed by 64-bit integers (`INT64`). ONNX Runtime Web's WebGPU execution provider has no WGSL shader kernel for INT64 GatherND (error: `Unsupported data type: 7`).

Additionally, the Hugging Face repository `model_fp16.onnx` file has an invalid schema (`RandomNormalLike` node type mismatch). When `buildForDevice` attempts `fp16`, it fails and causes `@huggingface/transformers` to cache the rejection promise. Furthermore, `frontend/composables/useSliceAudio.ts` collapses any non-memory/non-network worker error into `DEVICE_INIT_FAILED` and omits `console.error` logging, masking underlying root causes and misclassifying WebKit/Safari's `TypeError: Load failed` as hardware incompatibility.

## Goals / Non-Goals

**Goals:**
- Guarantee stable, cross-platform on-device speech synthesis across Windows, macOS, Linux, Android, and iOS.
- Eliminate WebGPU probe and execution for MMS-TTS models, locking execution to CPU (WASM).
- Prevent `fp16` model weight loading for MMS-TTS, eliminating graph validation errors and model cache corruption.
- Maintain desktop multi-threaded SIMD `fp32` (highest speed/quality) and mobile single-threaded `q8` (tab stability within ~36.6 MB footprint).
- Provide transparent developer console error diagnostics and accurately distinguish network failures from device constraints.

**Non-Goals:**
- Replacing MMS-TTS with alternative TTS engines (e.g. Kokoro-TTS or Piper).
- Modifying backend Google Cloud TTS synthesis proxy, database tables, or character quota guards.
- Altering the user-facing dual-engine toggle, pre-roll streaming, or IndexedDB caching infrastructure.

## Decisions

### 1. Lock MMS-TTS Models to CPU WASM Backend
- **Decision**: In `frontend/workers/ttsSynth.worker.ts`, configure `resolveDevice()` or `pickDevice()` to resolve to `'wasm'` directly for all MMS-TTS models (`Xenova/mms-tts-*`).
- **Rationale**: WebGPU execution provider in ONNX Runtime Web cannot execute the INT64 `GatherND` operator required by Meta's VITS flow. Testing proves CPU WASM with SIMD multi-threading is 100% stable, producing high-quality 16kHz audio in ~1.5–3.5s per sentence on desktop and ~2–4s on mobile.
- **Alternatives Considered**:
  - *Keep WebGPU probe with runtime fallback on infer error*: Probing WebGPU on hybrid GPU setups creates unnecessary initialization delays, triggers `RandomNormalLike` graph failures, and can crash mobile browser GPU contexts. Direct WASM routing is deterministic and reliable.

### 2. Streamlined Precision Selection without `fp16` Probing
- **Decision**:
  - Desktop: Use `dtype: 'fp32'` (optimal for x86/x64/ARM desktop SIMD kernels).
  - Mobile: Use `dtype: 'q8'` (`model_quantized.onnx`, ~36.6 MB to fit mobile memory bounds).
  - Eliminate the `try fp16 -> catch fp32` fallback chain in `buildForDevice`.
- **Rationale**: `model_fp16.onnx` on Hugging Face is structurally defective. Calling `pipeline('text-to-speech', ..., { dtype: 'fp16' })` throws a type mismatch error and causes `@huggingface/transformers` to cache a rejected promise for that model id. Direct `fp32`/`q8` loading bypasses this failure entirely.

### 3. Comprehensive Error Diagnostics & Classification in `useSliceAudio`
- **Decision**:
  - In `frontend/composables/useSliceAudio.ts`, log `console.error('[useSliceAudio] Device TTS Error:', err)` in `worker.onerror` and `worker.onmessage` (`type === 'error'`).
  - In `categorizeAudioError`:
    - Add checks for `lower.includes('load failed')` (Safari/WebKit's exact fetch failure message), `lower.includes('failed to load')`, and `lower.includes('timeout')` -> classify as `NETWORK_ERROR`.
    - Preserve `rawMessage` in the returned `AudioErrorInfo`.
- **Rationale**: When users report issues on mobile or desktop, DevTools console must show the exact error message (e.g. model download 404/CORS, memory exhaustion, or worker initialization) rather than silently converting it into an opaque code.

### 4. Resilient Fallback Presentation in `ReaderAudioPlayer.vue`
- **Decision**:
  - Ensure the error container in `ReaderAudioPlayer.vue` displays actionable guidance:
    - If `NETWORK_ERROR`: Suggest checking connection / retrying.
    - If `DEVICE_INIT_FAILED` or `DEVICE_OOM`: Offer 1-tap switch to Google Cloud (`canFallbackToCloud`).
- **Rationale**: Users who encounter slow network downloads on mobile should not be told their device hardware is unsupported; they should be prompted to retry or switch to Cloud.

## Risks / Trade-offs

- **WASM Compute Speed vs GPU**:
  - *Trade-off*: Synthesizing on CPU WASM rather than WebGPU uses CPU cycles.
  - *Mitigation*: The pre-roll buffer target is capped at 2 sentences (`Math.min(2, sentences.length)`), so audible playback begins within 3–5 seconds while background sentences stream ahead of playback.
- **Mobile Cellular Bandwidth**:
  - *Risk*: Downloading the 36.6 MB quantized model on mobile networks can fail or time out on slow connections.
  - *Mitigation*: Accurate download percentage display, classification as `NETWORK_ERROR` upon failure, and immediate 1-tap fallback to server-side Google Cloud TTS.
