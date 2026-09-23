# Proposal

## Why

The Google Sign-In button on the production login page (`techdaily.duckdns.org/login`) rendered as a blank/invisible element, preventing users from authenticating with Google OAuth. Investigation revealed two contributing factors:
1. **Nginx Security Headers**: Legacy `X-Frame-Options "SAMEORIGIN"` and missing CSP `frame-src` directives interfered with iframe security policies on production.
2. **Color Scheme Propagation (Root Cause)**: Per W3C CSSWG #7493, Chromium propagates document-level `color-scheme: dark` into cross-origin iframes unless explicitly overridden, causing Google GSI's iframe to fail visual rendering when dark mode is active.

## What Changes

- **Nginx Security Headers**:
  - Remove `X-Frame-Options "SAMEORIGIN"` and deprecated `X-XSS-Protection` from `nginx/nginx.conf`.
  - Add `Content-Security-Policy: frame-ancestors 'self'; frame-src 'self' https://accounts.google.com;`. (Note: `script-src` is intentionally omitted to avoid breaking GSI secondary script loads from `apis.google.com` and `gstatic.com`).
- **Frontend Button Isolation & Theme Re-rendering** (`frontend/pages/login.vue`):
  - Set `style="color-scheme: light;"` on the Google button container to shield the GSI iframe from dark mode inheritance.
  - Clear the container (`btnContainer.innerHTML = ''`) and re-render the button via `watch(colorMode)` when toggling dark/light mode.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

_None — this is a production infrastructure and UI rendering fix. The Google OAuth authentication flow itself remains unchanged._

## Impact

- **`nginx/nginx.conf`**: Update security headers to modern CSP standards.
- **`frontend/pages/login.vue`**: Add `color-scheme: light` container style, clear container on re-render, and observe `colorMode`.
- **Security Posture**: Preserved clickjacking protection via `frame-ancestors 'self'` while allowing GSI iframes via `frame-src`.
