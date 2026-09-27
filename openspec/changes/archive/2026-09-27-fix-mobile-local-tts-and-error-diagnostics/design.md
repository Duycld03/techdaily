# Design: Mobile Local TTS Resilience & Diagnostic Error Recovery

## Context

See `proposal.md` for background and problem motivation.

In the current implementation:
1. `ttsSynth.worker.ts` configures ONNX Runtime WebAssembly thread concurrency directly to `navigator.hardwareConcurrency` if `isIsolated` is true. On mobile devices (iOS Safari, mobile Chromium), creating multi-threaded WASM workers inside an ES module Web Worker exhausts process limits or triggers OOM failures.
2. The model weights downloaded for WASM default to unquantized FP32 (`model.onnx`, 109 MB). On mobile networks and mobile RAM budgets, this 109 MB allocation frequently causes browser tab crashes.
3. In `buildForDevice`, the catch block returns `buildEntry(model, device, 'fp32', reqId)` without `await`, causing unhandled rejections if the fallback rejects.
4. `ReaderAudioPlayer.vue` watches `errorMessage` but completely ignores its value, displaying a generic `t('reader.audio_error')` toast. When mobile synthesis fails, users see an unexplained failure with no path forward, even though Google Cloud TTS is fully functional on their device.

## Goals / Non-Goals

**Goals:**
- Guarantee stable execution of on-device TTS on mobile browsers by restricting WASM to single-threaded mode (`numThreads = 1`).
- Lower the mobile memory and network footprint by loading quantized model weights (`q8`, 36.6 MB vs 109 MB) on mobile devices.
- Fix promise chaining in `buildForDevice` so fallbacks cleanly resolve or reject with detailed error context.
- Surface specific error reasons in `ReaderAudioPlayer.vue` and provide a 1-tap fallback action to switch to Google Cloud TTS upon local failure.

**Non-Goals:**
- Modifying backend API endpoints or Google Cloud TTS synthesis logic (backend is healthy and confirmed functional).
- Altering the SM-2 spaced repetition algorithm or database entities.
- Redesigning the reader layout or changing desktop audio player dimensions.

## Decisions

### 1. Mobile Detection & Single-Threaded WASM Cap (`ttsSynth.worker.ts`)
- **Decision**: Detect mobile environments in the worker using:
  ```typescript
  const isMobile = typeof navigator !== 'undefined' && (
    /android|iphone|ipad|ipod|mobile/i.test(navigator.userAgent) ||
    (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1) // iPadOS Safari
  )
  ```
- **Execution Policy**:
  - If `isMobile`: `onnxWasm.numThreads = 1` always, even if `isIsolated` is true.
  - If desktop: `onnxWasm.numThreads = Math.min(4, Math.max(1, navigator.hardwareConcurrency))` to prevent excessive thread thrashing while retaining high desktop throughput.
- **Rationale**: On mobile WebKit and mobile Chrome, spawning helper workers from a Web Worker or allocating large thread stacks causes process termination or WebAssembly memory allocation failures. Single-threading guarantees stability.

### 2. Quantized Weights on Mobile WASM (`ttsSynth.worker.ts`)
- **Decision**: Define `PREFERRED_DTYPE` adaptively:
  - Desktop WASM: `fp32` (benchmarked faster on desktop CPUs with large caches).
  - Mobile WASM: `q8` (quantized 8-bit, 36.6 MB instead of 109 MB).
- **Fallback Chain**: If `q8` fails to load, gracefully fall back to `fp32`. If WebGPU fails or lacks optional shader features (`shader-f16`), cleanly fall back to WASM with the appropriate precision.
- **Rationale**: 36.6 MB is well within mobile browser memory limits and downloads ~3x faster over mobile networks.

### 3. Await Fallback Pipeline Construction (`ttsSynth.worker.ts`)
- **Decision**: Update `buildForDevice` to:
  ```typescript
  async function buildForDevice(model: string, device: Backend, reqId: number): Promise<PipelineEntry> {
    try {
      const dtype = isMobile && device === 'wasm' ? 'q8' : PREFERRED_DTYPE[device]
      return await buildEntry(model, device, dtype, reqId)
    }
    catch (err) {
      // Cleanly await universal fallback
      return await buildEntry(model, device, 'fp32', reqId)
    }
  }
  ```
- **Rationale**: Ensures the returned promise is awaited within the `try/catch` context, catching asynchronous constructor rejections and propagating structured errors up to `resolveEntry`.

### 4. Error Diagnostics & 1-Tap Cloud Fallback (`useSliceAudio.ts` & `ReaderAudioPlayer.vue`)
- **Decision**:
  - In `useSliceAudio.ts`, capture the exact failure error message and expose a formatted error reason.
  - In `ReaderAudioPlayer.vue`, when `errorMessage` is set and `engineMode === 'device'`:
    - Display an actionable toast or player banner explaining that device synthesis encountered an error (with reason details).
    - Provide a direct button in the notification/banner: **"Switch to Google Cloud"** / **"Chuyển sang Google Cloud"**.
    - Clicking the button switches `engineMode` to `'cloud'` and immediately re-triggers `loadAndPlay()`.
- **Rationale**: Directly solves the user's frustration ("không rõ nguyên nhân") and leverages the fact that Google Cloud TTS works reliably on mobile.

## Risks / Trade-offs

- **Synthesis Speed on Mobile WASM**: Single-threaded quantized WASM runs slightly slower per sentence than multi-threaded desktop execution. However, with pre-roll buffering ($\ge 33\%$ of sentences) and sequential queueing, playback begins as soon as the initial buffer is ready, and it guarantees execution without tab crashes.
- **Cloud Quota Consumption**: When falling back to Cloud TTS, users consume their monthly cloud character quota. The fallback notification will clearly indicate that Cloud TTS is being selected.
