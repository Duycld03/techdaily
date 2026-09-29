# Tasks

## 1. Infrastructure (Nginx Configuration)

- [x] 1.1 In `nginx/nginx.conf`, add `add_header Cross-Origin-Embedder-Policy "credentialless" always;` to `location /_nuxt/` alongside the existing `Cross-Origin-Resource-Policy "cross-origin"` directive.
- [x] 1.2 Validate `nginx/nginx.conf` syntax using `docker run --rm -v $(pwd)/nginx/nginx.conf:/etc/nginx/nginx.conf:ro nginx:alpine nginx -t` (or local equivalent) to guarantee valid configuration grammar.

## 2. Verification & Regression Testing

- [x] 2.1 Verify HTTP response headers for `/_nuxt/` assets to ensure `Cross-Origin-Embedder-Policy: credentialless` and `Cross-Origin-Resource-Policy: cross-origin` are emitted cleanly without duplicate headers.
- [x] 2.2 Execute frontend unit tests (`npm test` in `frontend/`) to ensure all data contracts and state behavior suites pass with 100% success.
- [x] 2.3 Verify in headless browser that on-device narration (`engine === 'device'`) instantiates `ttsSynth.worker.ts` without `net::ERR_BLOCKED_BY_RESPONSE` or `coep-frame-resource-needs-coep-header` failures.
