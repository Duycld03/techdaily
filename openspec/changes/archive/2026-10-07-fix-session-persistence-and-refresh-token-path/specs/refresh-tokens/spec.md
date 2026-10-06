# Spec Delta

## MODIFIED Requirements

### Requirement: Refresh token issuance alongside access token
The authentication endpoints (`/api/v1/auth/login`, `/register`, `/google`, `/refresh`) SHALL issue a refresh token alongside the JWT access token. The refresh token SHALL be a cryptographically random 256-bit value, stored as a SHA-256 hash in the database, and delivered to the client as an HttpOnly SameSite=Lax cookie scoped to the root application path (`Path=/`).
In HTTPS environments (detected via direct TLS or `X-Forwarded-Proto: https`), the cookie SHALL include the `Secure` attribute. In local or development environments running over plain HTTP, the `Secure` attribute SHALL be omitted so that browsers running in non-secure HTTP contexts correctly store and transmit the cookie. Each refresh token SHALL have a 30-day absolute expiration window (`ExpiresAt`) and track family association (`FamilyId`), generation counter (`Generation`), creation timestamp (`CreatedAt`), and revocation timestamp (`RevokedAt`).

#### Scenario: User authenticates successfully and receives root-scoped HttpOnly refresh cookie
- **WHEN** user successfully logs in, verifies registration, or signs in via Google
- **THEN** response headers include `Set-Cookie` with `refreshToken`
- **AND** the cookie specifies `Path=/`, `HttpOnly`, and `SameSite=Lax`
- **AND** `Secure` is enabled if the request was HTTPS.

---

### Requirement: Frontend transparent token refresh with cross-tab coordination
The HTTP client composable and frontend route middleware SHALL detect access token expiry and coordinate `POST /api/v1/auth/refresh` transparently across browser tabs and client-side navigations.
1. **Proactive Refresh Lead Time**: The HTTP client SHALL proactively initiate background token refresh when the access token has less than **5 minutes (300 seconds)** remaining before expiration (`exp * 1000 - Date.now() <= 300_000 ms`), ensuring seamless rotation before requests encounter hard expiration.
2. **SSR Route Guard Resilience**: The Nuxt route middleware (`auth.global.ts`) SHALL read the `refreshToken` cookie during SSR. If the access token is expired but a `refreshToken` cookie is present, the server SHALL NOT execute an immediate hard redirect to `/login` and SHALL defer verification to client-side hydration.
3. **Login View Automatic Session Restoration**: When a user navigates to `/login` with an expired access token or a valid refresh cookie, the view (`login.vue`) SHALL attempt background token refresh (`authStore.tryRefreshToken()`) and redirect to the target destination upon success instead of prompting for credentials.

#### Scenario: User visits protected page with expired access token but valid refresh cookie
- **GIVEN** the learner has an expired access token in browser storage
- **AND** a valid `refreshToken` cookie exists with `Path=/`
- **WHEN** the user reloads or navigates directly to `/today` or `/`
- **THEN** Nuxt SSR does not issue an immediate 302 redirect to `/login`
- **AND** client hydration calls `tryRefreshToken()` to rotate the token and persist the new access token
- **AND** the user remains on the requested page with active authentication.

#### Scenario: User visits /login with valid refresh cookie
- **GIVEN** the user opens `/login?redirect=/library`
- **AND** a valid `refreshToken` cookie exists
- **WHEN** the login component mounts
- **THEN** it executes `tryRefreshToken()`
- **AND** upon successful rotation, navigates automatically to `/library`.
