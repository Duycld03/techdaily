# Design: Resilient Auth Token Refresh Lifecycle

## Context

TechDaily uses short-lived JWT access tokens (60 minutes) alongside long-lived (30 days) HttpOnly refresh tokens with rotation and reuse detection. See `proposal.md` for motivation.

In the current implementation:
1. `useAuthStore.ts`'s `init()` eagerly calls `clearSession()` if `isTokenExpired(token.value)` is true, erasing the access token, user profile, and localStorage credentials before any refresh can be initiated.
2. `auth.global.ts` route middleware runs synchronously, checking `authStore.isLoggedIn` immediately. If the token expired during a user session or between tab switches, the middleware immediately rejects the route and pushes the user to `/login`.
3. `AuthEndpoints.cs` sets `Secure = true` unconditionally on the `refreshToken` cookie. In local dev or LAN access over plain HTTP (e.g., Windows 11 client connecting to Linux dev server), browsers reject the cookie, making token refresh permanently impossible.
4. `AuthEndpoints.cs` hardcodes `AddMinutes(60)` rather than respecting `Jwt:ExpiryMinutes` from configuration.

## Goals / Non-Goals

**Goals:**
- Eliminate unexpected logouts while a valid refresh token exists in cookies.
- Ensure route navigation automatically and transparently refreshes expired tokens before rendering protected pages.
- Standardize cross-tab and in-tab refresh coordination with `navigator.locks` and single-flight in-flight deduplication.
- Ensure HttpOnly cookie flags match the protocol environment (omitting `Secure` on plain HTTP while strictly requiring it on HTTPS).
- Ensure configuration controls token lifetime (`Jwt:ExpiryMinutes`).

**Non-Goals:**
- Modifying the underlying database schema for `RefreshTokens` or the core SM-2/domain services.
- Altering the 10-second multi-tab grace window or token family reuse detection logic in `RefreshTokenService`.
- Replacing JWT access tokens with server-side sessions.

## Decisions

### 1. Dedicated Asynchronous Refresh Action in Pinia (`authStore.tryRefreshToken`)
**Decision:** Add `tryRefreshToken(): Promise<boolean>` to `useAuthStore`.
- This action acts as the high-level gate for both route middleware and proactive UI checks.
- It calls `useApiClient().refreshAuthToken()`. If successful, it updates the Pinia store with `setSession(newToken, refreshedUser)` and returns `true`.
- If refresh throws or returns empty (expired/revoked/invalid), it executes `clearSession()`, presents the session expiration notification, and returns `false`.

### 2. Asynchronous Route Guard in Global Middleware (`auth.global.ts`)
**Decision:** Make the route middleware asynchronously verify and refresh credentials before allowing or denying protected route access.
- Instead of synchronously checking `authStore.isLoggedIn`, when `isAuthRequired` is true and `!authStore.isLoggedIn`:
  ```ts
  if (isAuthRequired && !authStore.isLoggedIn) {
    const refreshed = await authStore.tryRefreshToken()
    if (!refreshed) {
      return navigateTo({
        path: '/login',
        query: { redirect: to.fullPath }
      })
    }
  }
  ```
- This ensures that if a user opens a bookmark, clicks a link, or returns to a tab after 60+ minutes, Nuxt seamlessly refreshes the access token before mounting the route.

### 3. Non-Destructive Store `init()`
**Decision:** Remove immediate `clearSession()` from `authStore.init()`.
- Synchronous `init()` should only hydrate state from cookies and `localStorage`.
- Expired tokens should NOT cause immediate state purging in `init()`; instead, `isLoggedIn` correctly computes `false` (via `!isTokenExpired(token)`), which signals to route guards and API clients that a refresh is required.
- Purging only occurs when a refresh attempt is actually rejected by the backend.

### 4. Protocol-Aware Cookie Setting in `AuthEndpoints.cs`
**Decision:** Determine cookie `Secure` attribute dynamically:
```csharp
var isHttps = context.Request.IsHttps ||
              string.Equals(context.Request.Headers["X-Forwarded-Proto"], "https", StringComparison.OrdinalIgnoreCase);

context.Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
{
    HttpOnly = true,
    Secure = isHttps,
    SameSite = SameSiteMode.Lax,
    Path = "/api/v1/auth",
    Expires = DateTimeOffset.UtcNow.AddDays(30)
});
```
- In production with Nginx terminating SSL (`X-Forwarded-Proto: https`), `isHttps` evaluates to `true`.
- In local development (`http://localhost:5000` or LAN HTTP), `isHttps` evaluates to `false`, allowing browsers to accept and store the cookie.

### 5. Dynamic Access Token Expiration
**Decision:** Read `Jwt:ExpiryMinutes` from configuration with a 60-minute fallback:
```csharp
var expiryMinutes = configuration.GetValue<int>("Jwt:ExpiryMinutes", 60);
tokenDescriptor.Expires = DateTime.UtcNow.AddMinutes(expiryMinutes);
```

## Risks / Trade-offs

- **Route Transition Latency:** Asynchronous token refresh during route middleware introduces a one-time network round-trip (~50-150ms) when navigating with an expired access token.
  - *Mitigation:* `useApiClient` already performs proactive refresh within 30 seconds of expiration during active API calls, minimizing how often the route middleware needs to execute a cold refresh.
- **SSR Cookie Forwarding:** When initial navigation occurs on the server (Nuxt SSR), the refresh token cookie must be forwarded if SSR performs API calls.
  - *Mitigation:* Nuxt SSR route middleware forwards headers using `useRequestHeaders(['cookie'])`.
