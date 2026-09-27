# Proposal

## Why

On production deployments behind Nginx reverse proxy, Google Cloud TTS synthesis fails with `ERR_CONNECTION_REFUSED` because `useSliceAudio.ts` contains a hardcoded fallback (`"" || 'http://localhost:5000'`) that directs audio synthesis requests to `http://localhost:5000` instead of relative paths. Concurrently, on-device local WebAssembly TTS operates in degraded single-threaded mode because the browser lacks Cross-Origin Isolation (`crossOriginIsolated: false`), throwing runtime console warnings and resulting in high synthesis latency.

## What Changes

- **Eliminate Hardcoded `localhost:5000` in Audio Composable**: Refactor `getFetchClient()` in `frontend/composables/useSliceAudio.ts` to utilize canonical relative URLs when running in production (matching `useApiClient.ts`), completely removing the fragile falsy fallback to `http://localhost:5000`.
- **Infrastructure Cross-Origin Isolation Enforcement**: Configure `Cross-Origin-Opener-Policy: same-origin` and `Cross-Origin-Embedder-Policy: credentialless` at both the Nginx reverse proxy layer (`nginx/nginx.conf`) and Nuxt Nitro server layer for reader routes, guaranteeing that `window.crossOriginIsolated` is `true` for WASM execution.
- **Top-Level Navigation Context Isolation**: Ensure that navigating into `/read/**` establishes an isolated browsing context so that WebAssembly can spawn multi-threaded workers (`SharedArrayBuffer` with up to 6 CPU threads) without falling back to single-threading.
- **WASM Thread Clamping Resilience**: Update `ttsSynth.worker.ts` to gracefully check `self.crossOriginIsolated` before assigning `onnxWasm.numThreads`, suppressing noisy console error traces when running in non-isolated contexts.

## Capabilities

### New Capabilities
<!-- None -->

### Modified Capabilities
- `audio-narration`: Update reader audio narration and Google Cloud TTS synthesis requirements to mandate canonical relative API path resolution in production and verified cross-origin isolation headers for multi-threaded WASM execution.

## Impact

- **Affected Files**: `frontend/composables/useSliceAudio.ts`, `frontend/workers/ttsSynth.worker.ts`, `frontend/nuxt.config.ts`, `nginx/nginx.conf`, `openspec/specs/audio-narration/spec.md`.
- **User Experience**: Google Cloud narration immediately works on production with zero connection errors; local on-device TTS synthesizes audio up to 4-5x faster using full hardware concurrency across multi-core CPUs.
- **Breaking Changes**: None.
