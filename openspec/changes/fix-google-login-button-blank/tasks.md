# Tasks

## 1. Infrastructure — Nginx Security Headers (secondary improvement)

- [x] 1.1 Remove `add_header X-Frame-Options "SAMEORIGIN" always;` — replaced by CSP `frame-ancestors`
- [x] 1.2 Remove `add_header X-XSS-Protection "1; mode=block" always;` — deprecated header
- [x] 1.3 Add `Content-Security-Policy` header: `frame-ancestors 'self'; frame-src 'self' https://accounts.google.com;`

## 2. Frontend — Root Cause Fix (color-scheme propagation)

- [x] 2.1 Set `color-scheme: light` on Google button container in `login.vue` to prevent `color-scheme: dark` from propagating into Google GSI cross-origin iframe (W3C CSSWG #7493)
- [x] 2.2 Extract `renderGoogleButton()` and add `watch(colorMode)` to re-render button on dark/light toggle without page refresh

## 3. Verification

- [x] 3.1 Validate Nginx config syntax
- [x] 3.2 Verify CSP response headers on production (`frame-ancestors 'self'; frame-src 'self' https://accounts.google.com;`)
- [x] 3.3 Verify Google Sign-In button renders correctly on localhost in dark mode
- [x] 3.4 Verify button switches theme on dark/light mode toggle without refresh
