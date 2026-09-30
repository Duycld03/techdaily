# Spec Delta: auth

## MODIFIED Requirements

### Requirement: Route Middleware Token Expiry Validation
The client-side route guard (`frontend/middleware/auth.global.ts`) SHALL enforce a strict **Default-Deny** security posture: all platform routes require active authentication by default, with only explicit guest authentication routes (`/login`, `/register`, `/forgot-password`, `/reset-password`) and local development playgrounds (`/playground/*`, `/showcase`) exempted.

1. **Server-Side Rendering (SSR) Route Guard & Expiration Verification**:
   - On Server-Side Rendering (`import.meta.server`), when an incoming HTTP request targets any protected route (such as `/library`, `/today`, `/notes`), the middleware SHALL inspect request cookies for session credentials (`techdaily_token` and `refreshToken`).
   - If **neither** `techdaily_token` nor `refreshToken` cookie is present, the SSR middleware SHALL immediately redirect the HTTP request to `/login` with `redirect={targetUrl}`.
   - If `techdaily_token` is present, the SSR middleware SHALL decode the JWT payload `exp` claim. If `exp * 1000 <= Date.now()` (the token has expired) and no `refreshToken` cookie is available to renew it, the SSR middleware SHALL immediately issue an HTTP 302 redirect to `/login` with `redirect={targetUrl}` before rendering the protected application shell (`<AppHeader>`, `<AppSidebar>`).
   - Under no circumstances SHALL the server render a protected application shell with user telemetry and then delegate expired token rejection to client-side hydration, which triggers DOM hydration mismatch collisions.

2. **Client-Side Hydration & Resilient Token Refresh**:
   - When an unauthenticated visitor attempts to navigate to any protected route (including `/`, `/today`, `/library`, `/quiz`, `/review`, `/roadmap`, `/settings`, `/profile`, `/insights`, `/notes`), the middleware SHALL immediately redirect the visitor to `/login`.
   - The middleware SHALL preserve the attempted destination in the `redirect` query parameter (e.g. `/login?redirect=%2Flibrary`).
   - When an authenticated user with a valid, non-expired token navigates to `/login`, the middleware SHALL redirect the user to the target specified by the `redirect` query parameter, or to `/today` if no redirect parameter is present.
   - If a visitor arrives at `/login` with an expired token, the middleware SHALL purge credentials and allow the visitor to stay on `/login`.
   - If a route requires authentication and `authStore.isLoggedIn` is false:
     - The middleware SHALL attempt silent background renewal via `authStore.tryRefreshToken()`.
     - If refresh fails due to transient network disconnection, server restart (HTTP 5xx), or local cross-origin cookie restrictions, the platform SHALL NOT wipe valid user credentials or access tokens from local storage.
     - The user SHALL only be redirected to `/login` with cleared session credentials if the refresh request is explicitly rejected by the authentication authority with HTTP 401 or 403, or if no stored token exists.

#### Scenario: User visits /login with expired token
- **WHEN** visitor navigates to `/login` with an expired token cookie
- **THEN** middleware recognizes the token as expired, clears session, and allows the visitor to stay on `/login` without redirecting back to `/today`.

#### Scenario: Active logged-in user visits /login
- **WHEN** authenticated user with a valid non-expired token navigates to `/login`
- **THEN** middleware redirects to `/today`.

#### Scenario: Unauthenticated visitor attempts to access protected platform route
- **WHEN** an unauthenticated visitor navigates directly to `/library` or `/quiz`
- **THEN** the route middleware intercepts navigation and redirects to `/login?redirect=%2Flibrary` (or respective path)
- **AND** zero protected content or layout chrome is rendered to the visitor.

#### Scenario: Unauthenticated visitor arrives at root URL
- **WHEN** an unauthenticated visitor navigates to `/`
- **THEN** the route middleware redirects to `/login`
- **AND** the executive dashboard is completely inaccessible until authentication completes.

#### Scenario: Pure guest visits protected route directly on SSR
- **WHEN** an unauthenticated visitor without any session cookies makes a direct HTTP request to `/library` or `/today`
- **THEN** the SSR route middleware immediately issues an HTTP 302 redirect to `/login` with the `redirect` query parameter set to the requested path
- **AND** zero HTML of the protected page or its layout container is rendered in the server response.

#### Scenario: Returning user with expired access token visits protected route on SSR
- **WHEN** a visitor with an expired `techdaily_token` cookie visits `/library`
- **THEN** the SSR route middleware decodes JWT expiration on the server, issues an immediate HTTP 302 redirect to `/login?redirect=/library` if no refresh token is present, and suppresses rendering the protected application shell.

#### Scenario: Server-side request with an expired access token
- **WHEN** a visitor makes an HTTP request to `/today` with an expired `techdaily_token` cookie and no refresh token cookie
- **THEN** the server SHALL respond with an HTTP 302 redirect to `/login?redirect=/today`
- **AND** the server SHALL NOT render `<AppHeader>` or `<AppSidebar>` into the HTML response body
- **AND** client-side hydration SHALL mount cleanly on `/login` with zero hydration node mismatch errors.

#### Scenario: Silent token refresh across local development ports
- **WHEN** client-side route navigation occurs with an expired access token in a local environment where refresh cookies cannot be sent across ports
- **AND** the refresh attempt fails with an unauthorized status
- **THEN** the client SHALL redirect cleanly to `/login?redirect={targetUrl}` without corrupting DOM tree structures.
