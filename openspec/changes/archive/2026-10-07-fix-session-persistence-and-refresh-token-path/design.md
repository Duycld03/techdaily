# Design

## Context

TechDaily issues 60-minute JWT access tokens alongside a 30-day HttpOnly refresh token. Previously, `AuthEndpoints.cs` set the refresh token cookie with `Path = "/api/v1/auth"`. Under RFC 6265, browsers only send cookies to matching path prefixes. When a learner reloads or visits page routes like `/` or `/today`, the browser does not send the `refreshToken` cookie to the Nuxt SSR server. Because the access token has expired and no refresh cookie is present in the SSR request headers, `middleware/auth.global.ts` issues an immediate HTTP 302 redirect to `/login?redirect=/`. Once on `/login`, the component's `onMounted` hook checks `authStore.isLoggedIn`, which evaluates to false due to the expired token, without attempting `authStore.tryRefreshToken()`.

## Goals / Non-Goals

**Goals:**
- Deliver the HttpOnly `refreshToken` cookie to all application routes (`Path = "/"`) so Nuxt SSR detects valid session continuity and defers route guarding to client-side hydration.
- Enable seamless background token rotation via `useApiClient.refreshAuthToken()` upon client hydration.
- Allow `login.vue` to auto-restore sessions if a user lands on the login page with an expired access token but a valid refresh cookie.
- Maintain strict security invariants: `HttpOnly = true`, `SameSite = Lax`, and `Secure = true` on HTTPS.

**Non-Goals:**
- Extending access token lifespan (access tokens remain short-lived 60-minute JWTs).
- Altering the SM-2 algorithm or other domain entity models.

## Decisions

### Decision 1: Root-Level Cookie Path Scope (`Path = "/"`)
In `backend/src/TechDaily.Api/Endpoints/AuthEndpoints.cs`:
- Change `Path = "/api/v1/auth"` to `Path = "/"` in both `SetRefreshTokenCookie` and `ClearRefreshTokenCookie`.
- Rationale: Nuxt SSR executes on the same origin (behind Nginx reverse proxy in production). Scoping to `/` allows the browser to transmit the cookie on document GET requests, providing the server with proof of an active session without exposing the token to client JavaScript.

### Decision 2: Proactive Session Auto-Restore on Login Mount
In `frontend/pages/login.vue`:
- In `onMounted()`, if `authStore.token` exists (even if expired) or if a refresh cookie is present, call `await authStore.tryRefreshToken()`.
- If rotation succeeds, automatically navigate to `getRedirectTarget()`.
- Rationale: Eliminates redundant manual logins when users re-open their browser or follow stale bookmarked `/login` links.

## Risks / Trade-offs

- **Risk**: Root-scoped cookie is sent to all subpaths under the origin.
  - **Mitigation**: The cookie is `HttpOnly` and `SameSite=Lax`, preventing XSS extraction and cross-site CSRF on third-party POST requests.
