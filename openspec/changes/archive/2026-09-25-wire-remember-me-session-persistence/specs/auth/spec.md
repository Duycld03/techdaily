# Spec Delta: Auth

## ADDED Requirements

### Requirement: Remember-Me Session Persistence
The `/login` "Remember session" checkbox SHALL control whether the authenticated session survives a browser restart, end-to-end across the client and server. The email/password login and registration requests SHALL carry a `rememberMe` boolean; when the field is omitted, the server SHALL treat it as `true` to preserve backward compatibility.

1. **Persistent session (checkbox checked, the default)**:
   - The frontend SHALL persist the access token and user in a 30-day `techdaily_token` / `techdaily_user` cookie and in `localStorage`.
   - The backend SHALL issue the `refreshToken` cookie as a persistent cookie with a 30-day `Expires`/`Max-Age` (see `refresh-tokens`).
   - The session SHALL remain valid after the browser is fully closed and reopened, subject to token expiry.

2. **Session-scoped session (checkbox unchecked)**:
   - The frontend SHALL persist the access token and user in a browser **session cookie** (no `Expires`/`Max-Age`) and in `sessionStorage`, and SHALL NOT write them to `localStorage` or a persistent cookie.
   - The backend SHALL issue the `refreshToken` cookie as a browser **session cookie** (no `Expires`/`Max-Age`).
   - The session SHALL be discarded when the browser is fully closed; a reopened browser SHALL present no credentials and land on the unauthenticated state.

3. **Session clearing**: Logout and 401 session purge SHALL clear the credentials from both `localStorage` and `sessionStorage` and from the token/user cookies, regardless of the persistence mode that created them.

4. **Google OAuth**: The "Remember session" checkbox is presented only in `login` mode and not for the Google button; Google OAuth login SHALL issue a persistent session.

The refresh token's server-side absolute expiry SHALL remain 30 days in both modes; only the browser cookie persistence differs (see `refresh-tokens`).

#### Scenario: Login with Remember session checked persists across restart
- **WHEN** a user signs in via email/password with the "Remember session" checkbox checked
- **THEN** the login request body includes `"rememberMe": true`
- **AND** the client stores `techdaily_token` / `techdaily_user` in a 30-day cookie and `localStorage`
- **AND** after fully closing and reopening the browser, `authStore.init()` restores the session and `isLoggedIn` evaluates to `true` (subject to token expiry).

#### Scenario: Login with Remember session unchecked ends on browser close
- **WHEN** a user signs in via email/password with the "Remember session" checkbox unchecked
- **THEN** the login request body includes `"rememberMe": false`
- **AND** the client stores the access token and user in a session cookie and `sessionStorage`, writing neither to `localStorage` nor to a persistent cookie
- **AND** after the browser session ends, no credentials remain and the client presents the unauthenticated state.

#### Scenario: Login request omits rememberMe
- **WHEN** a client submits `POST /api/v1/auth/login` or `/register` without a `rememberMe` field
- **THEN** the server treats the request as `rememberMe: true` and issues a persistent refresh token cookie.

#### Scenario: Google OAuth login is persistent regardless of the checkbox
- **WHEN** a user authenticates via the Google button on `/login`
- **THEN** the server issues a persistent refresh token cookie and the client persists the session for 30 days.
