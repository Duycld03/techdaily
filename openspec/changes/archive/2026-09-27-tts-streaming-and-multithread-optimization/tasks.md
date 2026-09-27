# Tasks

## 1. Infrastructure & Proxy Configuration
- [x] 1.1 Update `nginx/nginx.conf` to add `proxy_hide_header Cross-Origin-Opener-Policy` and `proxy_hide_header Cross-Origin-Embedder-Policy` on `location /read/` to eliminate duplicate response headers from upstream Nitro.
- [x] 1.2 Validate Nginx configuration syntax and verify `curl -I` on `/read/**` returns single canonical COOP and COEP headers without duplication.

## 2. Frontend Navigation & Cross-Origin Isolation Guard

- [x] 2.1 Implement an isolation entry guard in `pages/read/[bookId].vue` or reader route middleware that forces a top-level document load (`window.location.assign`) when navigating from an unisolated SPA route if `window.crossOriginIsolated` is false.
- [x] 2.2 Verify that navigating into the reader view reliably activates `window.crossOriginIsolated === true` and enables `SharedArrayBuffer` for multi-threaded WASM.

## 3. Frontend Progressive Audio Streaming

- [x] 3.1 Refactor `frontend/composables/useSliceAudio.ts` to instantiate and play chunk 0 immediately via `encodeWav` when `onChunk` arrives for the first sentence.
- [x] 3.2 Implement a sequential sentence playback queue in `useSliceAudio.ts` that chains subsequent audio chunks on `'ended'` without stalling while background synthesis continues.
- [x] 3.3 Implement seamless cutover in `useSliceAudio.ts` upon synthesis completion (`done`), persisting the full concatenated WAV into IndexedDB (`techdaily-audio`), updating player duration, and preserving current playback position.

## 4. Testing & Verification

- [x] 4.1 Add and update unit tests in `frontend/tests/composables/useSliceAudio.spec.ts` verifying progressive playback on first chunk, chunk progression, and final assembled WAV caching.
- [x] 4.2 Run full frontend test suite (`npm test`) to ensure 100% test pass rate and absence of regressions.
- [x] 4.3 Run browser benchmark to verify time-to-speech drops to under 10 seconds and multi-threaded WASM scales across CPU cores.
