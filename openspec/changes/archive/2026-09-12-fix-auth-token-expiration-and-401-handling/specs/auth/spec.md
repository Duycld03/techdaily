# Delta Spec: Auth Token Expiration & Session Recovery

## ADDED Requirements

### Requirement: Proactive JWT Expiration Validation
The client-side authentication store SHALL decode the JWT payload `exp` claim and consider tokens expired if `exp * 1000 <= Date.now()`. When initialized or checked, an expired token SHALL be purged from cookies and local storage immediately.

#### Scenario: User visits app with an expired token
- **WHEN** user loads the application after being away for longer than the token lifetime
- **THEN** `authStore.init()` detects that `exp` is in the past, purges `techdaily_token` and `techdaily_user`, and sets `isLoggedIn = false`.
- **THEN** user avatar and protected UI elements display the unauthenticated state rather than a ghost session.

#### Scenario: User evaluates login state with active token
- **WHEN** user has a valid token where `exp * 1000 > Date.now()`
- **THEN** `authStore.isLoggedIn` evaluates to `true`.

---

### Requirement: Global 401 Session Expiration Interception
The HTTP client composable (`useApiClient`) SHALL intercept any response with HTTP status `401 Unauthorized`. It SHALL purge local session credentials, show a localized notification indicating that the session has expired, and redirect the user to `/login` preserving the current route as the redirect parameter.

#### Scenario: Authenticated request fails with 401
- **WHEN** an API call returns `HTTP 401 Unauthorized`
- **THEN** client executes session cleanup via `authStore.logout()` (or internal session reset).
- **THEN** client emits a warning notification: "Your session has expired. Please sign in again."
- **THEN** client navigates to `/login?redirect=<currentUrl>`.

---

### Requirement: Extended Access Token Lifespan Matching Curriculum
The authentication endpoints (`/api/v1/auth/login`, `/register`, `/google`) SHALL issue JWT access tokens with a 30-day lifetime (`AddDays(30)`) to match the 30-day curriculum timeline and the frontend cookie retention duration.

#### Scenario: User authenticates successfully
- **WHEN** user signs in via email/password or Google OAuth
- **THEN** the returned JWT token has an expiration date set to 30 days from creation time.
