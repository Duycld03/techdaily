# Design

## Context

See proposal.md — Why. Current state (read from source):

- `frontend/workers/ttsSynth.worker.ts` builds one Transformers.js `text-to-speech` pipeline per model, resolving the device once via `navigator.gpu.requestAdapter()` (webgpu when an adapter exists, else wasm; wasm fallback on build/inference failure). It imports only `pipeline` from `@huggingface/transformers` — it never touches `env`, so `env.backends.onnx.wasm.numThreads` is unset and ONNX Runtime Web picks its default thread count. `requestAdapter()` is called with no options. Weights load at default precision (fp32).
- Reader routes are already cross-origin isolated (prior change set COOP/COEP on `/read/**` and CORP/COEP on `/_nuxt/**`), so `SharedArrayBuffer` is available and multi-threaded WASM is *possible* — but not *used*, because thread count is never configured.
- `frontend/composables/useSliceAudio.ts` owns a worker engine (`createWorkerEngine`) that maps worker messages (`progress`/`chunk`/`done`/`error`) to per-request handlers. It exposes no notion of the resolved compute device.
- `@huggingface/transformers` is pinned at `4.3.0`, which supports both the `dtype` pipeline option and the `env.backends.onnx.wasm` config surface.

## Goals / Non-Goals

**Goals:**
- Fully utilize a many-core CPU (no usable GPU) by configuring the WASM backend thread count to `navigator.hardwareConcurrency`.
- Reduce synthesis latency and first-download size via quantized model weights, with a full-precision fallback.
- Prefer a discrete GPU adapter and make the resolved compute device (GPU/CPU) visible in the reader.

**Non-Goals:**
- Forcing WebGPU on a browser that exposes no adapter (a browser/OS/driver flag, not reachable from app code).
- Warm-up/preload of the model on reader mount (conflicts with the existing "downloaded lazily on first use" requirement and the platform rule that heavy model work stays user-triggered).
- Any change to the produced audio format, the `(chunkId, voice, contentHash)` cache key, playback controls, or backend/server code.

## Decisions

### 1. Configure WASM threads to hardware concurrency (many-core CPU utilization)
Set `env.backends.onnx.wasm.numThreads = navigator.hardwareConcurrency` (guarded `>= 1`) at worker module init, before any pipeline build. This is the direct answer to a strong-CPU, GPU-less machine: ORT Web then runs inference across that many threads when `SharedArrayBuffer` is available. Without cross-origin isolation ORT clamps threads to 1 automatically, so no explicit guard for the non-isolated case is needed — synthesis still completes single-threaded.
- **Alternative — cap at `min(4, cores)`**: rejected, and validated by benchmark — fp32 synthesis scales past 4 threads on an 8-core box (~7.5s @1 → ~2.6s @4 → ~1.8s @8 per sentence). Synthesis runs in a Worker, so saturating cores does not block the UI thread. (The *quantized* path did not scale and even regressed at 8 threads — a further reason to drop it; see Decision 2.)
- SIMD needs no flag: ORT Web already selects the SIMD+threaded artifact when isolated.

### 2. Backend-appropriate weight precision — fp32 on CPU, fp16 on GPU (reverses the original "quantized" plan)
Extend `buildEntry(model, device, dtype)` with a `dtype` argument. `buildForDevice` builds the backend's preferred precision and, on build failure, rebuilds that same device at fp32 — nested inside the existing device fallback (webgpu → wasm), at most two dtype attempts per device.
- Preferred dtype by device: `fp16` on webgpu (GPU-native half precision), **`fp32` on wasm**. `fp32` is the universal fallback.
- **Why not quantized on CPU:** benchmarked per-sentence synthesis of `Xenova/mms-tts-eng` in a cross-origin-isolated reader (8-core box): **q8 (int8) ≈ 10.5s vs fp32 ≈ 2.5s at 4 threads — ~4x slower**. ONNX Runtime Web has no fast int8 kernels for this VITS model, so q8 dequantization overhead dominates. Critically, **q8 is also Transformers.js's default WASM dtype**, so the pre-change reader was already on the slow path; the original "quantized is faster/smaller" assumption is false for CPU and would have *regressed* latency. fp32 + all cores is the CPU fast path: ~12.7s/sentence (single-threaded quantized default) → ~1.8s/sentence — a ~7x improvement.
- **fp16 on WebGPU** is the standard GPU half-precision variant the model ships; not benchmarked here (no adapter in the test browser) but low-risk, with fp32 fallback on build failure.

### 3. Report resolved device to the reader; render a production indicator
The worker posts a dedicated `{ type: 'device', reqId, device }` message once `getPipeline` resolves (before the first `chunk`). `createWorkerEngine` maps it to a new `onDevice(device)` handler; `useSliceAudio` exposes a reactive `device: Ref<'webgpu' | 'wasm' | null>`. `ReaderAudioPlayer.vue` renders a localized label ("Playing on GPU" / "Playing on CPU", vi equivalents) inside the existing control.
- This is production transparency for **all** users, not a local-dev banner (Pillar 1): it applies to every deployment and simply names the active backend. It replaces the raw `No available adapters.` console notice as the user's signal.
- **Alternative — silent (console only)**: rejected; leaving users to read a browser console message is the exact confusion reported.

### 4. Prefer a high-performance adapter
Call `gpu.requestAdapter({ powerPreference: 'high-performance' })`. The named-const cast type in `pickDevice` widens to `requestAdapter: (o?: { powerPreference?: string }) => Promise<unknown>`. Cheap, correct, and picks the discrete GPU on hybrid-graphics machines.

## Risks / Trade-offs

- **q8 quantized is ~4x slower than fp32 on WASM for this model** (benchmarked) → resolved by Decision 2: force fp32 on the CPU path instead of the framework's quantized WASM default.
- **fp16 on WebGPU is unverified in this environment** (no GPU adapter in the test browser) → it is the GPU-native half-precision variant the model ships; the fp32 fallback covers a build failure, and the CPU path (the reported user scenario) is fully benchmarked.
- **`numThreads = all logical cores` can oversubscribe hyperthreads** → for fp32 it scaled cleanly to 8 threads on the test box (no regression); bounded to reported concurrency and off the UI thread, so acceptable; can be capped later without a spec change.
- **Threads are a no-op without cross-origin isolation** → ORT clamps to 1; reader routes are already isolated, and the single-thread path is covered by an explicit spec scenario.
- **Testing boundary** → the reactive `device` contract (worker `device` message → composable state) is unit-testable in Vitest; WASM thread count and per-backend synthesis latency are not (no ONNX runtime in Happy-DOM) and are verified via a browser benchmark plus the Gate 2 visual check and the user's confirmed listening pass.

## Migration Plan

Pure frontend change; no data or API migration. Deploy is a normal frontend build. Rollback is a straight revert — dtype, thread-count, adapter-option, and the added `device` message are all backward compatible with cached audio and existing callers.
