# Proposal

## Why

The Google Sign-In button on the production login page (`techdaily.duckdns.org/login`) renders as a blank/invisible element, preventing users from authenticating with Google OAuth. The same button works correctly on `localhost:3000`. This is a critical authentication regression — Google OAuth is the primary login method for many users.

## What Changes

- Remove the `X-Frame-Options "SAMEORIGIN"` header from Nginx and replace it with a `Content-Security-Policy` directive using `frame-ancestors 'self'` for equivalent clickjacking protection that does not interfere with Google's GSI iframe rendering.
- Add a `frame-src` CSP directive to explicitly allow iframes from `https://accounts.google.com` required by the Google Identity Services library (`google.accounts.id.renderButton()`).

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

_None — this is a production infrastructure configuration fix. The existing auth spec requirements (Google OAuth login, token issuance, session management) remain unchanged. The Google OAuth flow itself functions correctly; only the button's visual rendering is blocked by Nginx security headers absent in the local dev server._

## Impact

- **Nginx config** (`nginx/nginx.conf`): Security headers section (lines 56–62) will be modified to replace `X-Frame-Options` with CSP equivalents.
- **No backend/frontend code changes**: The Nuxt frontend (`login.vue`, `nuxt.config.ts`) and backend OAuth endpoints remain untouched.
- **Security posture**: Clickjacking protection is preserved via `frame-ancestors 'self'` (CSP Level 2), which is the modern replacement for `X-Frame-Options` and is supported by all current browsers. The change is a strict security improvement — CSP is more flexible and more precisely scoped.
- **Deployment**: Requires Nginx restart (`docker compose restart nginx`) after config update.
