# Proposal: Fix Mobile Local TTS Failure and Enhance Error Diagnostics

## Why

On mobile browsers (iOS Safari, Android Chrome), on-device narration (local Web Worker TTS) fails immediately or crashes during synthesis, displaying a generic error toast ("Couldn't generate audio. Please try again." / "Không tạo được audio. Vui lòng thử lại.") with zero diagnostic details. Meanwhile, Google Cloud TTS works reliably.

This failure occurs because:
1. **Mobile WebAssembly Thread Exhaustion & Memory Ceilings**: In `ttsSynth.worker.ts`, `onnxWasm.numThreads` scales to `navigator.hardwareConcurrency` (typically 4–8 on modern phones) whenever the page is cross-origin isolated. On mobile browsers (especially WebKit/iOS and constrained mobile Chromium), spawning multiple WASM worker threads inside an ES module Web Worker exhausts process thread and memory bounds, triggering allocation failures or browser tab crashes.
2. **Heavy Unquantized Model Footprint**: The worker defaults to unquantized FP32 weights (`model.onnx`, 109 MB) on WASM, exceeding mobile memory limits and mobile bandwidth budgets compared to quantized alternatives (`model_quantized.onnx`, 36.6 MB).
3. **Fragile WebGPU to WASM Fallback**: In `buildForDevice`, the catch fallback returns an un-awaited promise (`return buildEntry(...)`), creating unhandled rejections during initialization.
4. **Swallowed Error Diagnostics**: `ReaderAudioPlayer.vue` completely discards `errorMessage` from `useSliceAudio.ts` and only presents a generic toast, hiding whether the failure was caused by memory limits, WebGPU initialization, network timeouts, or worker crashes, while failing to offer switching to the working Cloud engine.

## What Changes

- **Mobile-Safe Worker Execution (`ttsSynth.worker.ts`)**:
  - Detect mobile device environments (`/mobile|android|iphone|ipad|ipod/i.test(navigator.userAgent)`).
  - Explicitly restrict WASM execution to single-threaded (`onnxWasm.numThreads = 1`) on mobile devices, regardless of `crossOriginIsolated` state, preventing thread pool allocation failures and process crashes.
  - On mobile WASM, select quantized weights (`dtype: 'q8'` / `model_quantized.onnx`, ~36.6 MB) instead of unquantized FP32 (~109 MB) to reduce memory overhead by ~66% and eliminate tab out-of-memory crashes.
  - Properly `await` fallback pipeline construction in `buildForDevice` to prevent race conditions and unhandled rejection leaks.
- **Detailed Error Diagnostics & Cloud Fallback (`useSliceAudio.ts` & `ReaderAudioPlayer.vue`)**:
  - Capture and propagate specific error reasons (e.g., `OUT_OF_MEMORY`, `WORKER_INIT_FAILED`, `NETWORK_ERROR`, or raw exception messages) through `errorMessage`.
  - Update `ReaderAudioPlayer.vue` to display informative error feedback including actionable context (e.g., "Device memory insufficient. Try Cloud engine.").
  - Provide an automatic or one-tap option to switch to the working Google Cloud engine when on-device synthesis fails on mobile.
- **I18n Translations (`en.json`, `vi.json`)**:
  - Add localized error guidance and suggestion to switch to Cloud TTS when on-device narration encounters hardware/memory limitations on mobile devices.

## Capabilities

### Modified Capabilities
- `audio-narration`: Add mobile environment resource constraints (single-threaded WASM, quantized weights for mobile, informative error diagnostics, and 1-tap fallback to Cloud engine when on-device synthesis encounters mobile hardware limits).
