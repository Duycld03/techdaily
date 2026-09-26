# Tasks

## 1. Faster CPU synthesis (multi-thread + fp32 precision)

- [x] 1.1 Confirm which precision variants `Xenova/mms-tts-vie` and `Xenova/mms-tts-eng` publish (inspect the model repos' `onnx/` files) and benchmark them on the CPU (WASM) path. Verified: both publish `model.onnx` (fp32), `model_fp16.onnx`, `model_quantized.onnx` (q8, all HTTP 200); benchmark showed q8 ≈ 4x slower than fp32 on WASM, so the CPU preferred dtype is `fp32` (not q8).
- [x] 1.2 In `frontend/workers/ttsSynth.worker.ts` import `env` from `@huggingface/transformers` and set `env.backends.onnx.wasm.numThreads = Math.max(1, navigator.hardwareConcurrency)` at module init (before any pipeline build). Verify with a smoke run on an isolated `/read/**` route that `crossOriginIsolated === true` and the configured thread count is `> 1` on a multi-core machine, and that synthesis still produces `chunk` messages.
- [x] 1.3 Extend `buildEntry` to accept a `dtype` and make `buildForDevice`/`resolveEntry` build the backend's preferred precision (`fp16` on webgpu, `fp32` on wasm) then rebuild the same device at `fp32` on build failure, nested inside the existing webgpu→wasm device fallback. Verified with a browser benchmark that fp32 on WASM completes without error and is ~4x faster than the quantized default.
- [x] 1.4 Confirm the single-thread + full-precision path still completes when cross-origin isolation or a preferred build is unavailable. Verified with a fresh-context browser benchmark: single-threaded fp32 synthesis completes (~7.5s/sentence) with no error (matches the `Single-threaded fallback still completes` and `Full-precision fallback` spec scenarios).

## 2. Discrete-GPU preference and compute-device reporting

- [x] 2.1 Pass `{ powerPreference: 'high-performance' }` to `gpu.requestAdapter(...)` and widen the named-const cast type in `pickDevice` to accept the options object. Verify `npx nuxi typecheck` (or the project's type check) passes and the adapter probe still resolves `webgpu`/`wasm`.
- [x] 2.2 Post `{ type: 'device', reqId, device }` from the worker after `getPipeline` resolves (before the first `chunk`); add an `onDevice` handler through `createWorkerEngine`; expose a reactive `device: Ref<'webgpu' | 'wasm' | null>` from `useSliceAudio`. Verify with a Vitest data-contract test that a worker `device` message sets the composable's `device` state.
- [x] 2.3 Render a localized compute-device indicator ("Playing on GPU" / "Playing on CPU" and vi equivalents) inside `ReaderAudioPlayer.vue`, bound to `device`; add the i18n keys to `frontend/i18n/locales/en.json` and `vi.json`, following the responsive-typography and `whitespace-nowrap shrink-0` layout invariants. Verify the keys resolve in both locales and the label re-renders when `device` changes.

## 3. Verification gates

- [x] 3.1 Gate 1 — run `cd frontend && npx vitest run` and confirm 100% of suites pass, including the new device-contract test.
- [x] 3.2 Gate 2 — drove headless Chromium to capture reader screenshots at Desktop (1920x1080) and Mobile (390x844) in both `en` and `vi` showing the compute-device (CPU) indicator, plus a ready-state shot of a completed 2:33 seekable narration; narration intelligibility confirmed by the user ("nghe được rồi"). Screenshots presented in-thread.
