# Design

## Context

See `proposal.md` for background and motivation.
1. **Google Cloud TTS Production Failure:**
   In `frontend/composables/useSliceAudio.ts`, `getFetchClient()` determines its base URL via:
   ```typescript
   baseUrl = (config?.public?.apiBaseUrl as string | undefined)?.trim() || 'http://localhost:5000'
   ```
   In production (`docker-compose.prod.yml`), `NUXT_PUBLIC_API_BASE_URL` is set to `""`. Because `""` is falsy in JavaScript, the expression falls back to `'http://localhost:5000'`, forcing the user's browser to make requests to `http://localhost:5000/api/v1/library/chunks/{chunkId}/audio`, which fails with `net::ERR_CONNECTION_REFUSED`.
2. **Local TTS WASM Multi-Threading Warning & Latency:**
   In `frontend/workers/ttsSynth.worker.ts`, the worker attempts to assign `onnxWasm.numThreads = Math.max(1, navigator.hardwareConcurrency)`. However, WebAssembly multi-threading with `SharedArrayBuffer` requires the browsing context to be cross-origin isolated (`self.crossOriginIsolated === true`). Because Nginx did not set COOP/COEP headers and client-side SPA navigation from unisolated routes (`/today`) maintains an unisolated document context, ONNX Runtime Web logs warnings and clamps down to single-threaded execution.

## Goals / Non-Goals

**Goals:**
- Eliminate hardcoded `localhost:5000` fallbacks across all frontend audio networking, unifying with `useApiClient.ts` URL resolution rules.
- Enable full Cross-Origin Isolation on reader routes (`/read/**`) by adding `Cross-Origin-Opener-Policy: same-origin` and `Cross-Origin-Embedder-Policy: credentialless` in Nginx and Nuxt Nitro.
- Guard `onnxWasm.numThreads` in `ttsSynth.worker.ts` so that thread scaling only executes when `self.crossOriginIsolated` is `true`, preventing console noise while gracefully scaling to multi-core CPUs.
- Validate that all existing backend and frontend tests pass without regression.

**Non-Goals:**
- Changing audio synthesis models or TTS voices.
- Imposing COOP/COEP on `/login` or auth routes where popup window communication (`window.opener`) is required for OAuth providers.

## Decisions

### 1. Unified Relative API Base URL Resolution
- **Decision:** In `frontend/composables/useSliceAudio.ts`, align `getBaseUrl()` with the canonical implementation in `useApiClient.ts`:
  - When running client-side on port `3000` (local development), resolve to `${protocol}//${hostname}:5000`.
  - When running in production (or on port 80/443 behind Nginx), return `''` (relative URL).
- **Rationale:** Behind Nginx reverse proxies, relative URLs (`/api/v1/...`) are seamlessly forwarded to the backend container without CORS preflights or mixed-content protocol errors.

### 2. Dual-Layer Cross-Origin Isolation (Nginx + Nitro)
- **Decision:**
  1. In `nginx/nginx.conf`, add a dedicated location block for `/read/` and `/_nuxt/`:
     ```nginx
     location /read/ {
         proxy_pass http://frontend_upstream;
         # Standard proxy headers ...
         add_header Cross-Origin-Opener-Policy "same-origin" always;
         add_header Cross-Origin-Embedder-Policy "credentialless" always;
     }
     ```
  2. In `frontend/nuxt.config.ts`, ensure `routeRules` configures headers for `/read/**` and `/_nuxt/**`.
- **Rationale:** Ensures that whether requests are served through Nginx in production or directly through Vite/Nitro in local development, the reader document arrives with required isolation headers.

### 3. Worker Thread Allocation Guard
- **Decision:** In `frontend/workers/ttsSynth.worker.ts`:
  ```typescript
  const isIsolated = typeof self !== 'undefined' && Boolean(self.crossOriginIsolated)
  if (onnxWasm && isIsolated && typeof navigator !== 'undefined' && typeof navigator.hardwareConcurrency === 'number') {
    onnxWasm.numThreads = Math.max(1, navigator.hardwareConcurrency)
  }
  ```
- **Rationale:** Eliminates ONNX Runtime's console warning `env.wasm.numThreads is set to 6, but this will not work unless you enable crossOriginIsolated mode` when running in unisolated fallback environments while preserving 6-thread acceleration when isolation is active.

## Risks / Trade-offs

- **Risk:** Client-side SPA navigation from unisolated routes (`/today`) to `/read/<bookId>` might not gain cross-origin isolation unless the document is hard-loaded.
  - **Mitigation:** When navigation to `/read` occurs, if `window.crossOriginIsolated` is not active and high-performance WASM narration is requested, the system continues to function cleanly via single-thread fallback, and a direct page refresh or bookmark load gains full 6-thread concurrency.
