# Design: Authoritative COEP Header for Worker Assets in Nginx

## Context

See `proposal.md` - Why.

In production deployments (`docker-compose.prod.yml`), Nginx operates as the front-facing reverse proxy handling TLS termination and routing:
- `location /read/`: Routes reader document requests to the frontend container, emitting `Cross-Origin-Opener-Policy: same-origin` and `Cross-Origin-Embedder-Policy: credentialless` to establish a cross-origin isolated context (`window.crossOriginIsolated === true`).
- `location /_nuxt/`: Routes client bundle chunks, Web Workers, and Vite assets to `frontend_upstream`.

In `nginx/nginx.conf`, `location /_nuxt/` includes:
```nginx
proxy_hide_header Cross-Origin-Resource-Policy;
proxy_hide_header Cross-Origin-Embedder-Policy;
```
This intentionally hides upstream Nuxt Nitro headers to prevent header duplication. However, while Nginx re-appends `add_header Cross-Origin-Resource-Policy "cross-origin" always;`, it omits `add_header Cross-Origin-Embedder-Policy "credentialless" always;`.

When an isolated reader document instantiates a module Web Worker (`new Worker('/_nuxt/ttsSynth.worker-*.js', { type: 'module' })`), modern browser engines enforce COEP on the worker script request. Without `Cross-Origin-Embedder-Policy: credentialless` on the response, the browser blocks the script with `net::ERR_BLOCKED_BY_RESPONSE` (`coep-frame-resource-needs-coep-header`).

## Goals / Non-Goals

**Goals:**
- Provide authoritative, canonical `Cross-Origin-Embedder-Policy: credentialless` and `Cross-Origin-Resource-Policy: cross-origin` headers on all static scripts and workers served under `/_nuxt/` in Nginx.
- Unblock on-device Web Worker initialization on both mobile and desktop browsers on production.
- Keep Nginx as the single authoritative source of cross-origin response headers, preventing header repetition or browser parsing failures.

**Non-Goals:**
- Modifying frontend application code, composables, or worker source files (`nuxt.config.ts` and `ttsSynth.worker.ts` are already correctly structured).
- Modifying backend APIs or database schemas.
- Applying cross-origin isolation to authentication routes (`/login`, `/`), preserving Google OAuth popup window communication.

## Decisions

### 1. Add `Cross-Origin-Embedder-Policy "credentialless"` to Nginx `location /_nuxt/`

**Decision:**
In `nginx/nginx.conf`, add `add_header Cross-Origin-Embedder-Policy "credentialless" always;` inside `location /_nuxt/`.

```nginx
location /_nuxt/ {
    proxy_pass http://frontend_upstream;
    ...
    proxy_hide_header Cross-Origin-Resource-Policy;
    proxy_hide_header Cross-Origin-Embedder-Policy;
    add_header Content-Security-Policy "frame-ancestors 'self'; frame-src 'self' https://accounts.google.com;" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header Permissions-Policy "camera=(), geolocation=(), payment=()" always;
    add_header Cross-Origin-Resource-Policy "cross-origin" always;
    add_header Cross-Origin-Embedder-Policy "credentialless" always;
}
```

*Rationale:*
- Dedicated Web Workers inherit the embedder policy of the spawning document context. Supplying `COEP: credentialless` allows the browser to load and instantiate the worker script without violation.
- Paired with `proxy_hide_header`, Nginx ensures exactly one COEP header is returned per request, avoiding duplicate headers that could trigger HTTP syntax errors.

*Alternatives Considered:*
- *Remove `proxy_hide_header Cross-Origin-Embedder-Policy` and rely on Nuxt Nitro*: Nitro's `routeRules` in `nuxt.config.ts` already specifies COEP for `/_nuxt/**`. However, having Nginx hide and re-add headers explicitly ensures consistency across all location blocks (`/read/` and `/_nuxt/`) and prevents upstream changes or proxies from creating duplicate headers.

## Risks / Trade-offs

- **Risk**: Could third-party scripts or iframes under `/_nuxt/` fail due to COEP?
  - *Mitigation*: The `/_nuxt/` path contains strictly first-party compiled JavaScript and CSS assets generated during `npm run build`. Third-party assets (such as Google Fonts or Hugging Face model weights) are hosted on external CDNs, which are unaffected by headers served on `/_nuxt/`.
