# Spec Delta: Refresh Tokens

## MODIFIED Requirements

### Requirement: Refresh token issuance alongside access token
The authentication endpoints (`/api/v1/auth/login`, `/register`, `/google`, `/refresh`) SHALL issue a refresh token alongside the JWT access token. The refresh token SHALL be a cryptographically random 256-bit value, stored as a SHA-256 hash in the database, and delivered to the client as an HttpOnly SameSite=Lax cookie scoped to the auth endpoint path (`Path=/api/v1/auth`).

In HTTPS environments (detected via direct TLS or `X-Forwarded-Proto: https`), the cookie SHALL include the `Secure` attribute. In local or development environments running over plain HTTP, the `Secure` attribute SHALL be omitted so that browsers running in non-secure HTTP contexts correctly store and transmit the cookie. Each refresh token SHALL have a 30-day absolute server-side expiry (`ExpiresAt`).

The browser persistence of the `refreshToken` cookie SHALL reflect the authentication request's `rememberMe` flag:
- When `rememberMe` is `true` (or the field is omitted), the cookie SHALL be a persistent cookie with `Max-Age=2592000` (30 days).
- When `rememberMe` is `false`, the cookie SHALL be a browser **session cookie** carrying no `Expires`/`Max-Age`, so the browser discards it when the session ends.

`POST /api/v1/auth/google` SHALL always issue a persistent cookie. The refresh token's server-side absolute expiry (`ExpiresAt`) SHALL remain 30 days regardless of `rememberMe`; only the browser cookie persistence differs. The chosen persistence SHALL be recorded on the refresh token family so that rotation preserves it (see the rotation requirement).

#### Scenario: Successful login returns access token and sets refresh cookie
- **WHEN** user authenticates via `POST /api/v1/auth/login` with valid credentials and `rememberMe: true` (or the field omitted)
- **THEN** the response body contains `accessToken` (JWT, configurable expiry) and the response includes a `Set-Cookie` header for the refresh token with `HttpOnly; SameSite=Lax; Path=/api/v1/auth; Max-Age=2592000`

#### Scenario: Login with Remember session disabled sets a session refresh cookie
- **WHEN** user authenticates via `POST /api/v1/auth/login` with valid credentials and `rememberMe: false`
- **THEN** the `Set-Cookie` header for `refreshToken` includes `HttpOnly; SameSite=Lax; Path=/api/v1/auth` and carries no `Expires` or `Max-Age` attribute (a browser session cookie)

#### Scenario: Google OAuth returns access token and sets refresh cookie
- **WHEN** user authenticates via `POST /api/v1/auth/google` with a valid Google ID token
- **THEN** the response body contains `accessToken` and the response includes a persistent refresh token cookie (`HttpOnly; SameSite=Lax; Path=/api/v1/auth; Max-Age=2592000`)

#### Scenario: Login over plain HTTP in local/LAN development
- **WHEN** client authenticates via `POST /api/v1/auth/login` over plain HTTP (e.g., `http://localhost:5000` or `http://192.168.x.x:5000`)
- **THEN** the `Set-Cookie` header for `refreshToken` does NOT include the `secure` flag, allowing the browser to accept and persist the cookie

#### Scenario: Login over HTTPS in production
- **WHEN** client authenticates via `POST /api/v1/auth/login` over HTTPS or through a reverse proxy supplying `X-Forwarded-Proto: https`
- **THEN** the `Set-Cookie` header for `refreshToken` includes `Secure; HttpOnly; SameSite=Lax; Path=/api/v1/auth`

### Requirement: Concurrency-safe refresh token rotation endpoint
The system SHALL expose `POST /api/v1/auth/refresh` that reads the refresh token from the HttpOnly cookie. Rotation SHALL be performed as an atomic database operation ensuring that a given refresh token can be successfully rotated into a new successor token.

The rotation operation SHALL use a conditional database update (`SET UsedAt = @now, ReplacedByTokenId = @newId WHERE Id = @id AND UsedAt IS NULL AND RevokedAt IS NULL AND ExpiresAt > @now`). The `ExpiresAt > @now` condition guarantees that an expired token can never be rotated.

The successor token SHALL inherit the presented token family's persistence, and the re-issued `refreshToken` cookie SHALL match that persistence: a persistent family SHALL re-issue a persistent cookie (`Max-Age=2592000`), and a session-scoped family SHALL re-issue a browser session cookie (no `Expires`/`Max-Age`). Rotation SHALL NOT promote a session-scoped session to a persistent one, nor demote a persistent session.

If the update affects zero rows, the system SHALL inspect the token state:
- Token has `RevokedAt != null` → return `HTTP 401`
- Token has `ExpiresAt <= @now` → return `HTTP 401` with `AUTH_REFRESH_TOKEN_EXPIRED`
- Token has `UsedAt != null`:
  - **Within Grace Window (`@now - UsedAt <= 10 seconds`)**: The system SHALL treat this as a legitimate concurrent request (e.g. from racing browser tabs or network retries). The system SHALL NOT revoke the family; it SHALL issue and return a valid access token corresponding to the successor token (`ReplacedByTokenId`).
  - **Outside Grace Window (`@now - UsedAt > 10 seconds`)**: The system SHALL treat this as token reuse/theft. It SHALL revoke all tokens in the family (including any successor tokens) by setting `RevokedAt` on all unrevoked family members and return `HTTP 401` with `AUTH_TOKEN_REUSE_DETECTED`.

#### Scenario: Valid refresh token rotation
- **WHEN** client sends `POST /api/v1/auth/refresh` with a valid, unused, unexpired refresh token cookie
- **THEN** the system returns a new `accessToken` and sets a new refresh token cookie, and the old refresh token is marked as used

#### Scenario: Rotation preserves the family's cookie persistence
- **WHEN** client rotates a refresh token whose family was issued as session-scoped (`rememberMe: false`)
- **THEN** the re-issued `refreshToken` cookie is again a browser session cookie with no `Expires`/`Max-Age`
- **AND** when the family was issued as persistent, the re-issued cookie again carries `Max-Age=2592000`

#### Scenario: Concurrent rotation of the same token within grace window
- **WHEN** two requests simultaneously present the same refresh token to `POST /api/v1/auth/refresh` within 10 seconds of initial rotation
- **THEN** the first request rotates the token and receives the successor token; the second request receives a valid access token without triggering family revocation

#### Scenario: Reuse of a rotated refresh token outside grace window
- **WHEN** client sends `POST /api/v1/auth/refresh` with a refresh token that was consumed more than 10 seconds ago (`UsedAt != null`)
- **THEN** the system revokes all unrevoked tokens in that family (including any successor tokens) and returns `HTTP 401` with `{ "code": "AUTH_TOKEN_REUSE_DETECTED" }`

#### Scenario: Expired refresh token
- **WHEN** client sends `POST /api/v1/auth/refresh` with an expired refresh token
- **THEN** the system returns `HTTP 401` with `{ "code": "AUTH_REFRESH_TOKEN_EXPIRED" }`

#### Scenario: Revoked refresh token
- **WHEN** client sends `POST /api/v1/auth/refresh` with a refresh token that has `RevokedAt != null`
- **THEN** the system returns `HTTP 401`
