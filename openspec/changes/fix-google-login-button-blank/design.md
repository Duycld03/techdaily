# Design

## Context

The production Nginx reverse proxy (`nginx/nginx.conf`) sets security headers on the HTTPS server block. Among them, `X-Frame-Options "SAMEORIGIN"` was applied with the `always` keyword.

Google Identity Services (`google.accounts.id.renderButton()`) renders inside a cross-origin iframe sourced from `https://accounts.google.com`. In production, two issues prevented proper rendering:
1. Nginx lacked explicit `frame-src` allowing Google iframe origins, and relied on legacy `X-Frame-Options`.
2. Under dark mode, Chromium propagates `color-scheme: dark` from the root document into cross-origin iframes (W3C CSSWG #7493), which causes the Google Sign-In button iframe to render completely blank.

## Goals / Non-Goals

**Goals:**
- Fix the blank Google Sign-In button on production and in dark mode.
- Maintain robust clickjacking protection via CSP `frame-ancestors 'self'`.
- Allow Google GSI iframe origins via CSP `frame-src 'self' https://accounts.google.com;`.
- Isolate the button container from dark mode color scheme inheritance.
- Support dynamic re-rendering on dark/light mode toggle without duplicating button iframes.

**Non-Goals:**
- Replacing Google GSI with custom redirect flows.
- Comprehensive CSP nonce infrastructure (out of scope for this bugfix).

## Decisions

### 1. Replace `X-Frame-Options` with CSP `frame-ancestors` and `frame-src`
Remove `X-Frame-Options "SAMEORIGIN"` and add `Content-Security-Policy: frame-ancestors 'self'; frame-src 'self' https://accounts.google.com;`.
`X-XSS-Protection` is also removed as deprecated.

### 2. Omit `script-src` from Nginx CSP
Initially, `script-src 'self' https://accounts.google.com` was tested, but GSI dynamically fetches helper scripts from subdomains (`apis.google.com`, `gstatic.com`). Rather than enumerating brittle script origins or using `unsafe-inline`/`unsafe-eval`, `script-src` is left unconstrained at the proxy level until a comprehensive nonce-based CSP architecture is introduced.

### 3. Isolate Container Color Scheme in `login.vue`
Add inline `style="color-scheme: light;"` to the Google button container `div` (`googleBtnContainer`). This prevents Chromium from injecting dark color scheme semantics into Google's cross-origin iframe.

### 4. Clean Container Before Re-rendering on Theme Toggle
`gsi.renderButton()` appends child elements to the target DOM node. To support theme switching between dark (`filled_black`) and light (`outline`), `renderGoogleButton()` must clear `btnContainer.innerHTML = ''` before invoking `renderButton()`.

## Risks / Trade-offs

- **[Risk] Container clearing removes custom markup** → Mitigation: `googleBtnContainer` is a dedicated empty host container specifically for Google GSI.
- **[Risk] Google changes GSI iframe origins** → Mitigation: `https://accounts.google.com` is Google's stable, documented GSI origin.
