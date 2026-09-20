# Design

## Context

The production Nginx reverse proxy (`nginx/nginx.conf`) sets six security headers on the HTTPS server block (lines 56–62). Among them, `X-Frame-Options "SAMEORIGIN"` (line 57) is applied with the `always` keyword, attaching it to every response served through Nginx.

Google's Identity Services library (`google.accounts.id.renderButton()`) renders the Sign-In button inside a cross-origin iframe sourced from `https://accounts.google.com`. On production, the parent page's security header environment interferes with GSI's iframe rendering, causing the button to appear blank. On localhost:3000, requests bypass Nginx entirely (direct to Nuxt dev server), so the button renders normally.

No Content-Security-Policy header exists today. The frontend loads the GSI client script via `nuxt.config.ts` (`https://accounts.google.com/gsi/client`).

## Goals / Non-Goals

**Goals:**
- Fix the blank Google Sign-In button on production by adjusting Nginx security headers.
- Maintain equivalent or better clickjacking protection.
- Add a well-scoped CSP that explicitly allows the Google GSI iframe and script origins.

**Non-Goals:**
- Replacing the GSI iframe-based button with a custom redirect flow — the current `renderButton()` approach is Google's recommended integration.
- Comprehensive CSP hardening for all resource types — only `frame-src`, `frame-ancestors`, and `script-src` additions needed for this fix.
- Any backend or frontend code changes.

## Decisions

### 1. Replace `X-Frame-Options` with CSP `frame-ancestors`

**Decision:** Remove `add_header X-Frame-Options "SAMEORIGIN" always;` and add `Content-Security-Policy` with `frame-ancestors 'self'`.

**Rationale:** `X-Frame-Options` is a legacy header superseded by CSP Level 2's `frame-ancestors` directive. MDN marks `X-Frame-Options` as non-standard. `frame-ancestors 'self'` provides identical clickjacking protection with better granularity. All browsers supporting CSP Level 2 ignore `X-Frame-Options` when `frame-ancestors` is present.

**Alternative considered:** Keep `X-Frame-Options` and only add `frame-src` — rejected because `X-Frame-Options` controls whether *this page* can be framed, not outgoing iframes. The root cause involves how GSI interacts with the parent page's security posture, and replacing the header with a proper CSP policy is the clean fix.

### 2. Scope the CSP directive narrowly

**Decision:** Build a single `Content-Security-Policy` header with these directives:
- `frame-ancestors 'self'` — clickjacking protection (replaces `X-Frame-Options`)
- `frame-src 'self' https://accounts.google.com` — allows Google GSI iframes
- `script-src 'self' 'unsafe-inline' 'unsafe-eval' https://accounts.google.com` — allows the GSI client script from Google

**Rationale:** The Nuxt SSR frontend uses inline scripts and eval (Vue runtime, Vite HMR in dev). `'unsafe-inline'` and `'unsafe-eval'` are necessary to avoid breaking the frontend. The GSI client is loaded from `https://accounts.google.com/gsi/client` (configured in `nuxt.config.ts` line 66), so that origin must be explicitly allowed in `script-src`.

**Alternative considered:** Nonce-based script CSP — rejected as it requires server-side nonce generation and injection into every SSR response, which is significantly more complex and out of scope for this bug fix.

### 3. Remove deprecated `X-XSS-Protection` header

**Decision:** Remove `add_header X-XSS-Protection "1; mode=block" always;`.

**Rationale:** The `X-XSS-Protection` header is deprecated and has been removed from all modern browsers. MDN explicitly recommends removing it. In some edge cases, the XSS auditor itself could be exploited. A proper CSP replaces its purpose.

### 4. Keep all other security headers unchanged

The remaining headers (`X-Content-Type-Options`, `Referrer-Policy`, `Strict-Transport-Security`, `Permissions-Policy`) are unrelated to iframe/script loading and remain as-is.

## Risks / Trade-offs

- **[Risk] `unsafe-inline` / `unsafe-eval` weaken script CSP** → Mitigation: This matches the current implicit behavior (no CSP existed before, so everything was allowed). Moving to nonce-based CSP is a larger project tracked separately. This change is a net security improvement over the status quo.
- **[Risk] Google changes GSI iframe origins** → Mitigation: `https://accounts.google.com` is Google's stable, documented GSI domain. Unlikely to change without deprecation notice.
- **[Risk] CSP breaks other third-party embeds** → Mitigation: No other third-party iframes or scripts are used in the current codebase. The CSP is scoped to exactly what exists today.
