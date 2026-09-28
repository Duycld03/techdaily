# Spec Delta

## MODIFIED Requirements

### Requirement: Standard Email & Password Authentication
The system SHALL allow users to register and authenticate using email and password, issuing a JWT access token and a refresh token upon successful verification.

The JWT access token SHALL have a lifespan of **60 minutes** (`Jwt:ExpiryMinutes = 60`), limiting the blast radius of credential exposure. Token validation parameters SHALL incorporate a clock skew tolerance of **1 minute** (`ClockSkew = TimeSpan.FromMinutes(1)`) to absorb minor client-server clock drift without causing instantaneous authentication rejections at boundary seconds.

#### Scenario: User receives standard short-lived access token on login
- **WHEN** user authenticates via `POST /api/v1/auth/login`
- **THEN** the issued JWT access token has an expiration claim (`exp`) set to 60 minutes into the future
- **AND** the token validation middleware accepts requests within a 1-minute clock skew window of expiration.

### Requirement: Remember-Me Session Persistence
The `/login` "Remember session" checkbox SHALL control whether the authenticated session survives a browser restart, end-to-end across client storage and server cookies.

The frontend client SHALL persist the `techdaily_token` and `techdaily_user` cookies with `maxAge: 2592000` (30 days) and in `localStorage` when `rememberSession` is enabled. On page reloads and browser restarts, client hydration SHALL prioritize stored credentials from cookies and `localStorage`, preserving active sessions across code updates, hot module replacements (HMR), and server restarts.

#### Scenario: Client session persists across page reloads and HMR
- **WHEN** an authenticated user with a persistent session experiences a page reload or hot module replacement (HMR) update
- **THEN** the client preserves stored access token and user metadata
- **AND** maintains `isLoggedIn = true` without forcing an unprompted logout or redirection to `/login`.
