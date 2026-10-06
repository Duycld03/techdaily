# Proposal

## Why

Users with valid refresh tokens are prematurely kicked out to `/login?redirect=/` after their short-lived access token expires (60 minutes), even when `techdaily_token` and `techdaily_user` persist in browser storage. This occurs because the backend issues the `refreshToken` cookie with `Path=/api/v1/auth`, preventing the browser from sending it on server-rendered page requests (`/`, `/today`, `/library`). Consequently, Nuxt SSR detects an expired access token and no refresh cookie, triggering an immediate server-side HTTP 302 redirect to `/login` before client hydration can execute transparent token rotation.

## What Changes

- **Backend Cookie Path Normalization**: Update `SetRefreshTokenCookie` and `ClearRefreshTokenCookie` in `AuthEndpoints.cs` to set `Path = "/"` instead of `Path = "/api/v1/auth"`, allowing the browser to deliver the HttpOnly `refreshToken` cookie on SSR document requests.
- **Frontend SSR Route Guard Coordination**: Ensure `middleware/auth.global.ts` reads the `refreshToken` cookie on SSR and defers routing decisions to client-side hydration when a refresh cookie is present.
- **Frontend Login Auto-Restore**: Update `pages/login.vue` `onMounted` lifecycle hook to proactively attempt `authStore.tryRefreshToken()` when a user lands on `/login` with an expired access token or refresh cookie, seamlessly redirecting authenticated learners to their intended destination (`redirect` query or `/today`).

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `refresh-tokens`: Update cookie scope requirements so the HttpOnly refresh token is delivered to all application routes (`Path=/`), enabling SSR deferral and seamless background rotation.

## Impact

- **User Experience**: Resolves session dropouts after 1 hour of inactivity; users stay logged in for up to 30 days without unexpected redirects to the login screen.
- **Security**: Maintains HttpOnly, SameSite=Lax, and Secure cookie attributes while expanding cookie path from `/api/v1/auth` to `/` to support full-stack SSR authentication.
