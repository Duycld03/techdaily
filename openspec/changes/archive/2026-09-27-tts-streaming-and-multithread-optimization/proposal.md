# Proposal

## Why

On-device neural narration currently waits for all sentences in a slice to finish synthesis before concatenating audio and starting playback. For multi-paragraph slices (e.g. 19 sentences in Slice 2), users experience an 8-minute silent waiting period before any audio plays, even though the first sentence is synthesized within 7 seconds. Furthermore, duplicate COOP/COEP HTTP headers across Nginx and Nuxt Nitro and SPA navigation without document isolation prevent the browser from activating `crossOriginIsolated`, degrading CPU WASM inference to single-threaded execution.

Streaming audio sentence by sentence as chunks resolve and fixing cross-origin isolation headers transforms time-to-speech from 480+ seconds down to under 7 seconds while enabling multi-core CPU parallelism.

## What Changes

- **Early Streaming Audio Playback**: Introduce progressive playback in `useSliceAudio` so audio begins playing immediately when sentence 1 completes (`onChunk`), while subsequent sentences continue synthesizing in the background and are queued seamlessly.
- **Continuous Seamless Playback & Seekable Cache**: Maintain the invariant that upon completion of all sentences, the complete concatenated WAV file is assembled, cached in IndexedDB (`techdaily-audio`), and replaces the progressive stream to allow full seeking and offline replay without re-synthesizing.
- **De-duplicate Cross-Origin Isolation Headers**: Consolidate `Cross-Origin-Opener-Policy` and `Cross-Origin-Embedder-Policy` headers in Nginx reverse proxy configuration to eliminate duplicate headers in HTTP responses.
- **Cross-Origin Isolated Context on Reader Entry**: Ensure direct and SPA navigation into `/read/**` routes reliably establishes a cross-origin isolated browsing context (`window.crossOriginIsolated === true`) so ONNX Runtime Web utilizes multi-threaded WASM (`navigator.hardwareConcurrency`).

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `audio-narration`: Update requirements to specify progressive streaming playback for on-device TTS (playback starts on first sentence chunk while remaining sentences synthesize in background) and harden cross-origin isolation delivery to ensure multi-threaded WASM execution across client transitions.

## Impact

- **Frontend User Experience**: Time-to-speech for on-device narration drops from ~8 minutes to ~7 seconds on multi-sentence slices.
- **WASM Performance**: Multi-threaded execution on modern multi-core CPUs (e.g. AMD Ryzen 7, Apple Silicon, Intel Core) accelerates full-slice synthesis by 3x–6x.
- **Compatibility**: Retains full fallback to single-threaded WASM when isolation is unavailable, preserving compatibility across all environments.
