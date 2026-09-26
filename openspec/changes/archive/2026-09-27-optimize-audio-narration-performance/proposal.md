# Proposal

## Why

Reader narration falls back to CPU on machines whose browser exposes no WebGPU adapter (common on Linux Chrome, where WebGPU is gated behind flags), and on that CPU path synthesis is slow for two compounding reasons: the worker never configures ONNX Runtime Web to use more than its default thread count, and it lets Transformers.js pick its default WASM weights — the *quantized* (int8) variant, which (measured in a cross-origin-isolated reader) runs ~4x slower per sentence than full precision because ONNX Runtime Web has no fast int8 kernels for this VITS model. Users with a capable machine also see a raw `No available adapters.` console notice and no in-product signal of what is happening, so they read the slow CPU run as a defect. This change makes the CPU fallback substantially faster (measured ~19s/sentence → ~2s/sentence: single-threaded quantized default → multi-threaded fp32) and makes the active compute device visible in the reader.

## What Changes

- Configure the synthesis worker's CPU (WASM) backend to actually execute across multiple threads, scaled to `navigator.hardwareConcurrency`, using the cross-origin isolation already enabled on reader routes (`SharedArrayBuffer`). Today `env.backends.onnx.wasm.numThreads` is never set, so isolation is enabled but the extra threads go unused.
- Select the lowest-latency weight precision per backend instead of Transformers.js's default: full precision (fp32) on the CPU (WASM) path — where the default quantized (int8) weights are ~4x slower because ONNX Runtime Web lacks fast int8 kernels for this model — and half precision (fp16) on WebGPU (native to the GPU). fp32 is the universal fallback if a preferred build fails. Produced audio and all user-facing controls are unchanged.
- Prefer a high-performance GPU adapter when the browser exposes a choice, and report the resolved execution device (GPU vs CPU) from the worker to the reader so the audio control can surface a localized, production-appropriate indicator (e.g. "Playing on CPU"). This replaces the bare console notice as the user's signal and explains why a GPU-less browser runs on CPU.
- No change to audio output, caching keys, playback controls, the AI-formatting gate, or the lazy download-on-first-use guarantee.

Out of scope (considered, deliberately excluded): forcing WebGPU when the browser exposes no adapter is not possible from application code (it is a browser/driver flag). Warming up (preloading) the model on reader entry is excluded because it conflicts with the existing "downloaded lazily on first use" requirement and with the platform rule that heavy model work stays user-triggered rather than automatic on view mount.

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `audio-narration`: `GPU-Accelerated On-Device Synthesis with CPU Fallback` gains a high-performance adapter preference and reporting/display of the resolved execution device; `Multi-Threaded Synthesis Enabled on Reader Routes Only` gains the requirement that the CPU backend is actually configured to run across multiple threads scaled to hardware concurrency (not merely that isolation is enabled). A new requirement selects the lowest-latency weight precision per backend (fp32 on CPU/WASM, fp16 on WebGPU) rather than the framework's slower quantized default.

## Impact

- `frontend/workers/ttsSynth.worker.ts`: import and set `env.backends.onnx.wasm.numThreads`; build the pipeline with the fastest `dtype` per backend (fp32 on WASM, fp16 on WebGPU) instead of Transformers.js's slow quantized WASM default, falling back to fp32 on build failure; request the adapter with `powerPreference: 'high-performance'`; post the resolved device to the main thread.
- `frontend/composables/useSliceAudio.ts`: plumb a device/ready message into reactive state exposed by `useSliceAudio`.
- `frontend/components/reader/ReaderAudioPlayer.vue`: render the localized compute-device indicator within the existing control (respecting responsive-typography and `whitespace-nowrap shrink-0` invariants, en + vi).
- `frontend/i18n/locales/en.json` and `vi.json`: add device-indicator strings.
- No backend, API, database, or dependency changes (`@huggingface/transformers` 4.3.0 already provides `env` and `dtype`).
