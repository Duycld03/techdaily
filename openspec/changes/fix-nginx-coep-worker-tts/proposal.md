# Proposal: Fix On-Device TTS Worker Script COEP Blocking in Nginx

## Why

When users attempt on-device narration (`engine === 'device'`) on production deployments (`https://techdaily.duckdns.org`), the reader immediately fails on both mobile and desktop browsers with the toast message *"Giọng đọc thiết bị không khả dụng trên thiết bị này. Hãy thử Google Cloud."* (`audio_error_device`).

This failure occurs because:
1. Reader routes (`/read/**`) enforce Cross-Origin Isolation via `Cross-Origin-Opener-Policy: same-origin` and `Cross-Origin-Embedder-Policy: credentialless` so WebAssembly can execute in multi-threaded mode (`SharedArrayBuffer`).
2. According to W3C HTML and Fetch specifications, dedicated module Web Workers spawned from a document with `COEP: credentialless` require the worker script response to also deliver a compatible `Cross-Origin-Embedder-Policy` header.
3. In `nginx/nginx.conf`, the `location /_nuxt/` block hides upstream Nuxt Nitro headers with `proxy_hide_header Cross-Origin-Embedder-Policy`, but fails to re-add `add_header Cross-Origin-Embedder-Policy "credentialless" always;` (unlike `location /read/`).
4. Consequently, the browser network stack blocks `_nuxt/ttsSynth.worker-*.js` with `net::ERR_BLOCKED_BY_RESPONSE` (`blockedReason: coep-frame-resource-needs-coep-header`), firing `worker.onerror` ("Worker initialization failed") and causing `useSliceAudio` to fail with `DEVICE_INIT_FAILED`.

Restoring the `Cross-Origin-Embedder-Policy: credentialless` header to `location /_nuxt/` in Nginx enables the worker script to load and initialize client-side speech synthesis seamlessly on both desktop and mobile browsers.

## What Changes

- **Add COEP Header to Nginx Static & Worker Location**: Configure `add_header Cross-Origin-Embedder-Policy "credentialless" always;` in `location /_nuxt/` within `nginx/nginx.conf`.
- **Preserve Cross-Origin Isolation Consistency**: Keep `proxy_hide_header Cross-Origin-Embedder-Policy` and `proxy_hide_header Cross-Origin-Resource-Policy` to avoid duplicate headers from upstream Nitro, while ensuring the authoritative Nginx response contains single canonical `Cross-Origin-Embedder-Policy: credentialless` and `Cross-Origin-Resource-Policy: cross-origin` headers.
- **Automated Verification**: Verify via headless browser and HTTP header inspection that `_nuxt/ttsSynth.worker-*.js` carries the COEP header and the worker is instantiated without `ERR_BLOCKED_BY_RESPONSE`.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `audio-narration`: Update infrastructure cross-origin isolation requirements so that static asset and worker endpoints (`/_nuxt/**`) consistently serve `Cross-Origin-Embedder-Policy: credentialless` alongside `Cross-Origin-Resource-Policy: cross-origin` on production reverse proxies.

## Impact

- **Infrastructure**: `nginx/nginx.conf` (`location /_nuxt/`).
- **Frontend**: Zero changes required in Vue components or worker source files (`nuxt.config.ts` already specifies this requirement for Nitro and Vite).
- **Compatibility**: Completely unblocks on-device TTS on production for both mobile (Android Chrome, iOS Safari) and desktop browsers.
