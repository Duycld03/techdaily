# Proposal: Fix TTS Worker Inference Collision and Duplicate Synthesis on Engine Switch

## Why

When an on-device narration is paused mid-generation, switched to Google Cloud narration, and subsequently switched back to Device and resumed via the Play button, asynchronous message handling within the Web Worker can allow a newly dispatched synthesis job to overlap with an in-flight sentence inference from the prior job. Because Transformers.js and ONNX Runtime WASM / WebGPU execution sessions do not support concurrent calls to `entry.synth()` on the same instance, overlapping calls trigger `Error: Another run() is already in progress` or cause worker deadlock. Additionally, clicking the Play button while the audio player is buffering or loading re-invokes `loadAndPlay()`, spawning duplicate concurrent requests that corrupt worker state and permanently stall playback resumption.

## What Changes

- **Worker Sequential Inference Queue (Mutex)**: Introduce an asynchronous inference queue (`inferenceQueue`) in `ttsSynth.worker.ts` ensuring no two `entry.synth()` model evaluations execute concurrently on the same ONNX Runtime session.
- **Worker Job Sequencing & Token Validation**: Assign a sequential `currentJobId` token to each synthesis request; discard stale or cancelled inferences immediately without invoking the model pipeline.
- **Unconditional Worker Cancellation at `loadAndPlay`**: Call `cancelWorkerSynthesis()` unconditionally at the beginning of `loadAndPlay()` in `useSliceAudio.ts` to abort any active in-flight worker requests before starting a new device synthesis.
- **Worker Crash Recovery**: Nullify the cached worker instance (`worker = null`) inside `worker.onerror` so that unhandled worker faults allow clean recreation of a fresh Web Worker rather than reusing a dead worker thread.
- **Guarded Audio Toggle during Loading**: Guard `onToggle()` in `ReaderAudioPlayer.vue` with `if (isLoading.value) return` to prevent user clicks from spawning duplicate concurrent synthesis requests while audio is actively buffering or downloading.
- **Consistent Partial Cache Flush**: Ensure partial synthesis chunks are committed to IndexedDB by awaiting `cache.savePartial()` during cancellation before the next playback session attempts to read cached progress.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `audio-narration`: Updates requirements for on-device TTS execution to mandate serialized inference execution, mutual exclusion across concurrent worker requests, and click protection against duplicate synthesis triggers.

## Impact

- **API & Domain Contracts**: Zero breaking changes to backend APIs or database schemas.
- **Frontend Architecture**: Eliminates runtime worker crashes and playback stalls when toggling between Device and Google Cloud narration engines during active playback.
