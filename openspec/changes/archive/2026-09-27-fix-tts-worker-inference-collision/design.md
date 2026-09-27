# Technical Design: Worker Inference Serialization & Race-Condition Prevention

## Context

See `proposal.md` for motivation.

The on-device TTS architecture consists of three layers:
1. `ReaderAudioPlayer.vue`: UI component providing user playback controls, engine selection (Google Cloud vs On-Device), and speed controls.
2. `useSliceAudio.ts`: Composable managing audio chunk buffering, IndexedDB caching, and engine coordination.
3. `ttsSynth.worker.ts`: Dedicated Web Worker running Transformers.js pipelines and ONNX Runtime sessions (WASM or WebGPU).

In an asynchronous browser environment, switching engines during active playback or clicking the Listen button while chunks are buffering generates concurrent message dispatches. Because `ctx.onmessage` in the worker handles events asynchronously without a mutual exclusion lock, multiple calls to `entry.synth(sentence)` can run simultaneously on the underlying ONNX Runtime session, causing fatal `Another run() is already in progress` errors.

## Goals / Non-Goals

**Goals:**
- Guarantee strict serial execution of `entry.synth()` inside the worker via an asynchronous promise chain mutex.
- Invalidate stale sentences and canceled synthesis jobs using a monotonic job identifier (`currentJobId`).
- Unconditionally abort active worker tasks at the start of any new audio load or engine switch.
- Discard faulted worker instances and cleanly recreate fresh workers upon `worker.onerror`.
- Debounce user play/listen clicks while the player is in a loading or buffering state.
- Guarantee that partial audio synthesis progress is committed to IndexedDB before switching engines or cancelling.

**Non-Goals:**
- Concurrent multi-worker model execution (prohibited due to mobile and browser memory limits).
- Altering audio format, sample rate (16 kHz), or backend Google Cloud TTS endpoints.

## Decisions

### Decision 1: Worker-Side Asynchronous Inference Mutex (`inferenceQueue`)
- **Choice**: Maintain a module-scoped `let inferenceQueue: Promise<unknown> = Promise.resolve()` inside `ttsSynth.worker.ts`.
- **Implementation**: Wrap each `entry.synth(sentence)` call inside `inferenceQueue = inferenceQueue.catch(() => {}).then(async () => { ... })`.
- **Rationale**: Promises in JavaScript run sequentially on the event loop. Chaining guarantees that regardless of how many async message handlers are triggered, inference calls never execute concurrently on the underlying ONNX session.

### Decision 2: Monotonic Job Token Invalidation (`currentJobId`)
- **Choice**: Increment a worker-level `currentJobId` counter for every new `synth` message and assign each synthesis loop its own `jobId`.
- **Implementation**: Before awaiting inference, verify `if (jobId !== currentJobId || cancelledReqIds.has(reqId)) return null;`. After inference finishes, verify the token again before posting the synthesized chunk to the main thread.
- **Rationale**: Eliminates race conditions where a canceled job's delayed sentence inference reaches the main thread after a new job has already started.

### Decision 3: Unconditional Worker Cancellation on `loadAndPlay`
- **Choice**: Call `cancelWorkerSynthesis()` at the very top of `loadAndPlay()` in `useSliceAudio.ts` before branching on engine mode.
- **Implementation**: Post a `cancel` message to the worker and clear pending Promise handlers for in-flight requests.
- **Rationale**: Prevents previously active local worker synthesis from leaking into a newly selected Cloud or local playback session.

### Decision 4: UI Click Guard During Loading State
- **Choice**: In `ReaderAudioPlayer.vue`, insert `if (isLoading.value) return;` at the beginning of `onToggle()`.
- **Implementation**: Prevents user clicks on the Play button while the player is loading or buffering from dispatching duplicate `loadAndPlay()` calls.

### Decision 5: Clean Worker Lifecycle Recovery on Crash
- **Choice**: Inside `createWorkerEngine` in `useSliceAudio.ts`, add `worker = null` in the `worker.onerror` handler.
- **Rationale**: If the WebAssembly runtime aborts or runs out of memory, reusing the existing worker instance guarantees repeated failures. Nullifying the reference forces `ensureWorker()` to instantiate a fresh Web Worker for subsequent requests.

## Risks / Trade-offs

- **Sequential Latency**: Serialization means that if a job is canceled mid-sentence, the currently executing sentence inference must finish its forward pass before the next job can start. This is mitigated by immediately skipping all subsequent sentences in the canceled job's queue.
- **Cache Consistency**: Flushing partial synthesis via `await cache.savePartial()` during cancellation incurs a minor asynchronous delay (~5-10ms) but ensures resume fidelity.
