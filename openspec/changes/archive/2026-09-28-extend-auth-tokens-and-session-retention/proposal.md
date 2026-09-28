# Proposal

## Why

Users are frequently logged out ("kicked out") of their active sessions during normal daily usage and whenever code changes, hot reloads, or server restarts occur. Investigation reveals that the root cause is NOT an inherently broken token architecture, but rather multiple structural defects in the **Silent Refresh** lifecycle, **SSR Route Guard**, and **Transient Failure Handling**:

1. **Premature SSR Route Guard Ejection**: In `frontend/middleware/auth.global.ts`, Server-Side Rendering (SSR) immediately forces a server-side redirect to `/login` whenever `!authStore.token`. On page reloads, HMR refreshes, or when code changes are applied, Node.js SSR cannot access `localStorage`. If the access token cookie has expired or is delayed, SSR immediately ejects the user to `/login` *before* the browser client ever has a chance to execute `tryRefreshToken()` via the 30-day `refreshToken` cookie.
2. **False-Positive Session Invalidation on Transient Network Errors / Backend Restarts**: When code changes are applied during development or deployment, the backend API (`dotnet watch` or container restart) is temporarily unavailable for 1–2 seconds. When the frontend attempts an API call or token refresh during this window, the fetch throws a network exception (`TypeError: Failed to fetch` or HTTP 502/503). The client error handler currently treats *any* refresh failure as an expired session, prematurely invoking `authStore.clearSession()` and kicking the user out.
3. **Narrow Multi-Tab Refresh Token Grace Window (10 seconds)**: In `RefreshTokenService.cs`, the grace window for concurrent token rotation is only 10 seconds. When a user opens multiple tabs or when a page reload triggers multiple concurrent API requests, a secondary refresh request outside this 10-second window triggers `AUTH_TOKEN_REUSE_DETECTED`, which revokes the entire token family and forces an abrupt logout across all tabs.
4. **Proactive Refresh Window Too Tight (30 seconds) & Zero Clock Skew**: Client-side proactive refresh only triggers when less than 30 seconds remain before expiration, leaving almost zero buffer for network latency. Combined with strict `ClockSkew = TimeSpan.Zero` on the backend, minor clock drift causes instantaneous 401 rejections.

Crucially, **extending the Access Token to multiple days would violate core OAuth 2.0 security boundaries** (leaked bearer tokens would remain valid for days without server revocation ability). Instead, this proposal **preserves standard short-lived Access Tokens (60 minutes)** and fixes the silent refresh, SSR guard, and error handling mechanisms so sessions remain seamless, persistent, and secure.

## What Changes

- **Preserve Short-Lived Access Tokens (60 min) with Clock Skew Tolerance**:
  - Keep JWT access token lifespan strictly short-lived at **60 minutes** (`Jwt:ExpiryMinutes = 60`) to minimize the blast radius of token leakage.
  - Configure `ClockSkew = TimeSpan.FromMinutes(1)` in `Program.cs` `TokenValidationParameters` to absorb minor client-server clock drift and eliminate boundary-second rejections.
- **Resilient SSR Auth Route Guard**:
  - In `frontend/middleware/auth.global.ts`, do NOT execute a hard server redirect to `/login` on SSR if client-side session credentials or refresh token cookies may be present. Defer the authentication verification to client-side hydration, allowing `tryRefreshToken()` to silently renew the access token before determining if a redirect to `/login` is truly required.
- **Resilience Against Backend Restarts & Transient Network Errors**:
  - In `frontend/composables/useApiClient.ts`, distinguish authentic HTTP 401 Unauthorized responses from network errors (`Failed to fetch`, `ERR_CONNECTION_REFUSED`) and gateway errors (HTTP 502/503/504).
  - Transient network failures MUST NOT invoke `authStore.clearSession()` or redirect to `/login`. Sessions are only cleared when the backend explicitly returns `401 Unauthorized` and `refreshAuthToken()` fails.
- **Widen Refresh Token Multi-Tab Grace Window**:
  - In `backend/src/TechDaily.Infrastructure/Services/RefreshTokenService.cs`, widen the grace period for concurrent rotation from 10 seconds to **60 seconds**, eliminating false-positive token family revocations (`AUTH_TOKEN_REUSE_DETECTED`) during rapid multi-tab opens and HMR page reloads.
- **Proactive Client-Side Token Refresh (5-Minute Horizon)**:
  - In `frontend/composables/useApiClient.ts`, advance the proactive refresh window from 30 seconds to **5 minutes** (`5 * 60 * 1000 ms`), renewing tokens comfortably in the background during active user sessions before requests ever encounter an expired token.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `auth`: Preserves 60-minute JWT access token lifetime, adds 1-minute clock skew tolerance, updates SSR route middleware to prevent premature session logouts, and ensures transient network errors do not clear credentials.
- `refresh-tokens`: Widens concurrent refresh token reuse grace window to 60 seconds and establishes a 5-minute proactive client-side refresh horizon.

## Impact

- **Backend Configuration & Services**:
  - `backend/src/TechDaily.Api/appsettings.json`: Retain `Jwt:ExpiryMinutes` at 60 minutes.
  - `backend/src/TechDaily.Api/Program.cs`: Configure `ClockSkew = TimeSpan.FromMinutes(1)`.
  - `backend/src/TechDaily.Infrastructure/Services/RefreshTokenService.cs`: Widen grace window from 10s to 60s.
- **Frontend Interceptors & Middleware**:
  - `frontend/middleware/auth.global.ts`: Defer unauthenticated redirects on SSR to client-side hydration.
  - `frontend/composables/useApiClient.ts`: Extend proactive refresh horizon to 5 minutes; guard `clearSession()` against network/502 errors.
- **Unit & Contract Tests**:
  - Backend: Verify 60-minute token lifespan and 60-second rotation grace window.
  - Frontend: Verify proactive refresh at 5 minutes, network error resilience, and SSR route guard hydration.
