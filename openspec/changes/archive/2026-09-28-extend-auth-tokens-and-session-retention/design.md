# Design

## Context

See `proposal.md` for motivation.
TechDaily uses ASP.NET Core Minimal APIs for backend authentication and Nuxt 3 (SSR + client hydration) with Pinia for frontend state:
- **Backend Token Issuance**: `AuthEndpoints.cs` issues a JWT access token using `GenerateJwtToken(user, jwtSecret, jwtIssuer, jwtAudience, jwtExpiryMinutes)`. The expiration is configured by `Jwt:ExpiryMinutes` (60 minutes).
- **Backend Token Validation**: `Program.cs` registers `JwtBearerDefaults` with `ValidateLifetime = true` and `ClockSkew = TimeSpan.Zero`.
- **Backend Refresh Token Rotation**: `RefreshTokenService.cs` issues 256-bit refresh tokens with 30-day expiry (`ExpiresAt = UtcNow.AddDays(30)`), delivered as HttpOnly cookies (`SameSite=Lax; Path=/api/v1/auth`). It maintains a multi-tab grace window currently hardcoded to 10 seconds.
- **Frontend Interception & SSR Guard**:
  - `useApiClient.ts` intercepts 401s, attempts silent refresh via `navigator.locks`, and has a proactive refresh threshold of 30 seconds.
  - `auth.global.ts` evaluates route access. On SSR (`import.meta.server`), it immediately executes `navigateTo('/login')` if `!authStore.token`, ignoring client `localStorage` and pending cookie hydration.

## Goals / Non-Goals

**Goals:**
- Provide seamless, uninterrupted sessions so users are never ejected during daily reading, learning sessions, or local development reloads.
- Preserve standard short-lived JWT access token lifespan (60 minutes) to maintain OAuth 2.0 security boundaries and minimize credential blast radius.
- Configure 1-minute clock skew tolerance (`ClockSkew = TimeSpan.FromMinutes(1)`) in `Program.cs`.
- Widen multi-tab token rotation grace window in `RefreshTokenService` from 10 seconds to 60 seconds to eliminate false-positive `AUTH_TOKEN_REUSE_DETECTED` revocations.
- Increase proactive refresh lead time in `useApiClient` from 30 seconds to 5 minutes so active users refresh before ever encountering a 401.
- Prevent SSR route guard in `auth.global.ts` from forcing premature redirects before client-side hydration can inspect local storage and attempt token refresh.
- Prevent transient server connection failures or 502/503 responses from clearing user session credentials.

**Non-Goals:**
- Extending Access Token lifespan to multiple days (explicitly rejected for security reasons).
- Modifying PBKDF2 password hashing (strictly preserved at 600,000 iterations).
- Replacing the per-device refresh token family rotation architecture.

## Decisions

### 1. Preserve 60-Minute Access Token with 1-Minute Clock Skew
- **Decision**: Retain `Jwt:ExpiryMinutes = 60` in `backend/src/TechDaily.Api/appsettings.json` and `AuthEndpoints.cs`. Set `ClockSkew = TimeSpan.FromMinutes(1)` in `Program.cs`.
- **Rationale**: Keeping access tokens short-lived (60 minutes) is the core security pillar of OAuth 2.0. If a bearer token is leaked, its exploitation window is tightly bounded to 60 minutes. Adding 1 minute of clock skew tolerance absorbs client-server time drift without opening an extended vulnerability window.

### 2. Widen Concurrent Token Reuse Grace Window to 60 Seconds
- **Decision**: In `backend/src/TechDaily.Infrastructure/Services/RefreshTokenService.cs`:
  ```csharp
  // Change from 10 seconds to 60 seconds:
  if (now - token.UsedAt.Value <= TimeSpan.FromSeconds(60) && token.ReplacedByTokenId.HasValue)
  ```
- **Rationale**: When users have multiple tabs open (e.g. Reading view, Dashboard, Notes), or during hot module reloading where several tabs refresh in parallel, network latency or request serialization can easily exceed 10 seconds. Expanding to 60 seconds provides sufficient buffer while still detecting authentic token theft.

### 3. Proactive Client-Side Refresh at 5-Minute Horizon
- **Decision**: In `frontend/composables/useApiClient.ts`, trigger proactive background token refresh when:
  ```ts
  if (exp && exp * 1000 - Date.now() <= 5 * 60 * 1000)
  ```
- **Rationale**: Refreshing 5 minutes prior to expiration guarantees that requests in flight are never sent with an expired bearer token, completely eliminating unneeded 401 roundtrips.

### 4. Client-Hydration First SSR Route Guard
- **Decision**: In `frontend/middleware/auth.global.ts`:
  ```ts
  // On SSR, do not force-redirect to login when token is missing from SSR cookies,
  // because the token may exist in localStorage or be renewable via silent refresh.
  if (import.meta.server && isAuthRequired) {
    if (authStore.token) {
      return
    }
    // Defer check to client hydration instead of server-side abort
    return
  }
  ```
  On client-side (`import.meta.client`), the middleware checks `authStore.isLoggedIn`. If false, it invokes `await authStore.tryRefreshToken()`. If that returns false, only then does it redirect to `/login`.
- **Rationale**: This eliminates premature SSR redirects that occur when cookies are slightly out of sync with localStorage during development reloads or page refreshes.

### 5. Resilient Error Handling for Server Restarts
- **Decision**: In `frontend/composables/useApiClient.ts`, ensure that network errors (catch block on `fetch`, e.g. `TypeError: Failed to fetch`) or HTTP 502/503/504 errors do NOT invoke `authStore.clearSession()`. Only an explicit HTTP 401 response where subsequent `refreshAuthToken()` fails shall purge the session.
- **Rationale**: During local development (`dotnet watch`) or staging container rebuilds, the backend is temporarily unreachable for 1-2 seconds. Treating network drops as session revocation kicks the developer/user out of their account unnecessarily.

## Risks / Trade-offs

- **Risk: Multi-tab Grace Window Expansion**:
  - *Risk*: Expanding grace window to 60 seconds allows an attacker who intercepts an in-flight refresh token up to 60 seconds to use it concurrently before family revocation triggers.
  - *Mitigation*: The refresh token is stored in an `HttpOnly` cookie with `SameSite=Lax`, preventing JavaScript extraction (XSS) and cross-origin leakage (CSRF).
- **Risk: Deferring SSR Redirect to Client**:
  - *Risk*: An unauthenticated user visiting a protected route directly will render an initial SSR HTML frame before the client navigates to `/login`.
  - *Mitigation*: The root layout does not render protected user data until the client-side store is initialized; sensitive data is only fetched by client-side lifecycle hooks which require an active token.
