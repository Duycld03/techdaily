# Tasks

## 1. Frontend - Client Audio Networking & Worker Concurrency
- [x] 1.1 Refactor `getFetchClient()` in `frontend/composables/useSliceAudio.ts` to implement canonical relative base URL resolution matching `useApiClient.ts`, eliminating the `"" || 'http://localhost:5000'` falsy fallback.
- [x] 1.2 Update `frontend/workers/ttsSynth.worker.ts` to guard `onnxWasm.numThreads` behind `Boolean(self.crossOriginIsolated)`, preventing noisy console error logs in non-isolated browsing contexts.
- [x] 1.3 Ensure `frontend/composables/useSliceAudio.ts` unit tests in `frontend/tests/composables/useSliceAudio.spec.ts` validate relative URL resolution in production environments.

## 2. Infrastructure - Cross-Origin Isolation & Reverse Proxy

- [x] 2.1 Update `nginx/nginx.conf` with dedicated location routing for `/read/` and `/_nuxt/` that injects `Cross-Origin-Opener-Policy: same-origin` and `Cross-Origin-Embedder-Policy: credentialless`.
- [x] 2.2 Verify `frontend/nuxt.config.ts` routeRules properly configure COOP/COEP headers for SSR and Nitro build artifacts.

## 3. Verification & Regressions Testing

- [x] 3.1 Execute `cd frontend && npx vitest run tests/composables/useSliceAudio.spec.ts` to verify audio composable test suite passes.
- [x] 3.2 Execute `npm test` across the full frontend test suite (71 files) to confirm zero regressions.
- [x] 3.3 Execute `dotnet test backend/TechDaily.sln` to confirm backend test suite remains green.
