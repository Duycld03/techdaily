# Auth Specification

## Purpose
Provides hybrid authentication, Google OAuth account linkage, and password initialization workflows enabling users on mobile devices or alternative environments to seamlessly authenticate with email and password.

## Requirements

### Requirement: Google OAuth Password Setup & State Visibility
The system SHALL expose whether an authenticated user account has an active password configured (`hasPassword`) and allow users with Google OAuth accounts to establish a password without providing an existing password.

#### Scenario: User with Google account checks password status
- **WHEN** an authenticated user calls `GET /api/v1/user/profile`
- **THEN** the system returns `hasPassword: false` and `isGoogleLinked: true` when no PBKDF2 password hash is present.

#### Scenario: User with Google account creates first password
- **WHEN** user sends `PUT /api/v1/user/change-password` with `newPassword` (length >= 6) and no `currentPassword`
- **THEN** the system hashes the new password with PBKDF2 (100,000 iterations, 16-byte random salt), updates `user.PasswordHash`, and returns `200 OK`.
- **THEN** subsequent profile requests return `hasPassword: true`.

---

### Requirement: In-App Mobile Handoff Guidance
The system SHALL provide contextual guidance to Google OAuth users prompting them to set a password for seamless mobile login.

#### Scenario: User visits Profile Security tab without a password
- **WHEN** user navigates to `/profile` and selects the "Security & Password" tab with `hasPassword: false`
- **THEN** the UI displays an informative alert highlighting that setting a password allows logging in with email and password on mobile or other devices.
- **THEN** the UI hides the "Current Password" input field and changes the submit action button to "Set Password" (Thiết Lập Mật Khẩu).

#### Scenario: Google user logs in on desktop for first time
- **WHEN** user signs in via Google OAuth on desktop
- **THEN** a non-intrusive banner appears suggesting the user create a password for easy access on mobile devices.

---

### Requirement: Email-Based Password Setup for Stranded Mobile Users
The system SHALL allow users on devices where Google OAuth cannot be used to request a secure password setup link or code via their registered Google email.

#### Scenario: Unauthenticated visitor requests password initialization on login page
- **WHEN** an unauthenticated visitor on `/login` submits their Google email via the password setup flow
- **THEN** the system verifies the email exists and generates a password setup instruction token.

### Requirement: Machine-Readable Authentication Error Codes
The authentication and user account management endpoints (`/api/v1/auth/*` and `/api/v1/user/*`) SHALL return standardized error codes for all credential and validation failures:
- `AUTH_EMAIL_PASSWORD_REQUIRED`: Email or password omitted.
- `AUTH_PASSWORD_TOO_SHORT`: Password length less than 6 characters.
- `AUTH_EMAIL_EXISTS`: Registered account with email already present.
- `AUTH_INVALID_CREDENTIALS`: Email not found or password verification failed.
- `AUTH_GOOGLE_TOKEN_INVALID`: Google ID token invalid or signature verification failed.
- `AUTH_GOOGLE_NOT_CONFIGURED`: Google Client ID unconfigured on server.
- `USER_CURRENT_PASSWORD_INCORRECT`: Supplied current password failed verification.
- `USER_NEW_PASSWORD_TOO_SHORT`: New password length less than 6 characters.

#### Scenario: User attempts login with wrong password
- **WHEN** user submits invalid credentials to `POST /api/v1/auth/login`
- **THEN** server returns HTTP 400 with `{ "code": "AUTH_INVALID_CREDENTIALS", "error": "Invalid email or password." }`.

#### Scenario: User registers with an existing email
- **WHEN** user submits duplicate email to `POST /api/v1/auth/register`
- **THEN** server returns HTTP 400 with `{ "code": "AUTH_EMAIL_EXISTS", "error": "An account with this email already exists." }`.

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

### Requirement: Route Middleware Token Expiry Validation
The client-side route guard SHALL check token validity against expiration time before allowing access to protected routes or redirecting away from guest-only pages like `/login`. A raw unvalidated cookie string SHALL NOT be considered proof of active authentication.

#### Scenario: User visits /login with expired token
- **WHEN** visitor navigates to `/login` with an expired token cookie
- **THEN** middleware recognizes the token as expired, clears session, and allows the visitor to stay on `/login` without redirecting back to `/today`.

#### Scenario: Active logged-in user visits /login
- **WHEN** authenticated user with a valid non-expired token navigates to `/login`
- **THEN** middleware redirects to `/today`.
