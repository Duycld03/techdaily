# Refresh Tokens Specification

## Purpose
Provides short-lived access tokens with concurrency-safe refresh token rotation, family-based reuse detection, per-family revocation, and HttpOnly cookie storage to limit the blast radius of stolen credentials.

## Requirements

### Requirement: Refresh token issuance alongside access token
The authentication endpoints (`/api/v1/auth/login`, `/register`, `/google`, `/refresh`) SHALL issue a refresh token alongside the JWT access token. The refresh token SHALL be a cryptographically random 256-bit value, stored as a SHA-256 hash in the database, and delivered to the client as an HttpOnly SameSite=Lax cookie scoped to the auth endpoint path (`Path=/api/v1/auth`).

In HTTPS environments (detected via direct TLS or `X-Forwarded-Proto: https`), the cookie SHALL include the `Secure` attribute. In local or development environments running over plain HTTP, the `Secure` attribute SHALL be omitted so that browsers running in non-secure HTTP contexts correctly store and transmit the cookie. Each refresh token SHALL have a 30-day absolute expiry.

#### Scenario: Successful login returns access token and sets refresh cookie
- **WHEN** user authenticates via `POST /api/v1/auth/login` with valid credentials
- **THEN** the response body contains `accessToken` (JWT, configurable expiry) and the response includes a `Set-Cookie` header for the refresh token with `HttpOnly; SameSite=Lax; Path=/api/v1/auth; Max-Age=2592000`

#### Scenario: Google OAuth returns access token and sets refresh cookie
- **WHEN** user authenticates via `POST /api/v1/auth/google` with a valid Google ID token
- **THEN** the response body contains `accessToken` and the response includes a refresh token cookie with the same attributes

#### Scenario: Login over plain HTTP in local/LAN development
- **WHEN** client authenticates via `POST /api/v1/auth/login` over plain HTTP (e.g., `http://localhost:5000` or `http://192.168.x.x:5000`)
- **THEN** the `Set-Cookie` header for `refreshToken` does NOT include the `secure` flag, allowing the browser to accept and persist the cookie

#### Scenario: Login over HTTPS in production
- **WHEN** client authenticates via `POST /api/v1/auth/login` over HTTPS or through a reverse proxy supplying `X-Forwarded-Proto: https`
- **THEN** the `Set-Cookie` header for `refreshToken` includes `Secure; HttpOnly; SameSite=Lax; Path=/api/v1/auth`
### Requirement: Concurrency-safe refresh token rotation endpoint
The system SHALL expose `POST /api/v1/auth/refresh` that reads the refresh token from the HttpOnly cookie. Rotation SHALL be performed as an atomic database operation ensuring that a given refresh token can be successfully rotated into a new successor token.

The rotation operation SHALL use a conditional database update (`SET UsedAt = @now, ReplacedByTokenId = @newId WHERE Id = @id AND UsedAt IS NULL AND RevokedAt IS NULL AND ExpiresAt > @now`). The `ExpiresAt > @now` condition guarantees that an expired token can never be rotated.

If the update affects zero rows, the system SHALL inspect the token state:
- Token has `RevokedAt != null` → return `HTTP 401`
- Token has `ExpiresAt <= @now` → return `HTTP 401` with `AUTH_REFRESH_TOKEN_EXPIRED`
- Token has `UsedAt != null`:
  - **Within Grace Window (`@now - UsedAt <= 10 seconds`)**: The system SHALL treat this as a legitimate concurrent request (e.g. from racing browser tabs or network retries). The system SHALL NOT revoke the family; it SHALL issue and return a valid access token corresponding to the successor token (`ReplacedByTokenId`).
  - **Outside Grace Window (`@now - UsedAt > 10 seconds`)**: The system SHALL treat this as token reuse/theft. It SHALL revoke all tokens in the family (including any successor tokens) by setting `RevokedAt` on all unrevoked family members and return `HTTP 401` with `AUTH_TOKEN_REUSE_DETECTED`.

#### Scenario: Valid refresh token rotation
- **WHEN** client sends `POST /api/v1/auth/refresh` with a valid, unused, unexpired refresh token cookie
- **THEN** the system returns a new `accessToken` and sets a new refresh token cookie, and the old refresh token is marked as used

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

### Requirement: Per-family token revocation endpoint
The system SHALL expose `POST /api/v1/auth/revoke` that reads the refresh token from the HttpOnly cookie. The endpoint SHALL revoke all tokens in the presented token's family by setting `RevokedAt` on all unrevoked family members. The endpoint SHALL clear the refresh token cookie via `Set-Cookie` with `Max-Age=0`. The endpoint SHALL return `HTTP 200` regardless of whether the token was found or recognized (to prevent token enumeration). Revoking a token family does NOT affect other families belonging to the same user (per-device session model: Device A logout does not invalidate Device B's session).

#### Scenario: User explicitly logs out (revokes current session)
- **WHEN** client sends `POST /api/v1/auth/revoke` with a valid refresh token cookie
- **THEN** the system revokes all tokens in that family, clears the refresh token cookie, and returns `HTTP 200`
- **AND** other active sessions (different families) for the same user remain valid

#### Scenario: Revocation with unknown or missing token
- **WHEN** client sends `POST /api/v1/auth/revoke` without a refresh token cookie or with an unrecognized token
- **THEN** the system returns `HTTP 200` without error (silent no-op)

### Requirement: Frontend transparent token refresh with cross-tab coordination
The HTTP client composable and frontend route middleware SHALL detect access token expiry and coordinate `POST /api/v1/auth/refresh` transparently across browser tabs and client-side navigations.

When a protected page is accessed and the in-memory access token is expired or missing, the route middleware SHALL NOT synchronously purge user credentials. Instead, it SHALL asynchronously invoke token refresh via the auth store. Only if the refresh attempt yields a terminal failure (such as `401 Unauthorized`, expired refresh token, or network rejection) SHALL the client purge local session state, show an expiration notification, and redirect to `/login`.

The Web Locks API (`navigator.locks`) SHALL guard token refresh attempts so that concurrent API calls or simultaneous route navigations share a single refresh network transaction.

#### Scenario: Access token expires during page navigation
- **WHEN** user navigates to an authenticated route (e.g., `/review`, `/today`, `/quiz`) while the access token is expired but a valid refresh token cookie exists
- **THEN** the route middleware awaits a transparent token refresh, receives a new access token, updates the auth store, and completes navigation without redirecting to `/login`

#### Scenario: User returns to active tab after token expiration
- **WHEN** user returns to an existing tab or reloads a protected page after the access token has expired
- **THEN** the store initialization preserves existing user metadata, triggers asynchronous token refresh, and retains authentication status

#### Scenario: Access token expires during usage
- **WHEN** the access token expires while the user is actively using the application
- **THEN** the HTTP client transparently refreshes the token and retries the failed request without user-visible interruption

#### Scenario: Multi-tab token refresh
- **WHEN** multiple browser tabs make requests simultaneously with an expired access token
- **THEN** the Web Locks API ensures only one tab sends `POST /api/v1/auth/refresh` and other tabs reuse the newly refreshed access token without race conditions

#### Scenario: Refresh token also expired or invalid during navigation
- **WHEN** both the access token and refresh token are expired or refresh fails with `401 Unauthorized`
- **THEN** the client clears session state, shows the session-expired notification, and redirects to `/login` with the attempted route preserved in query parameter `redirect`
### Requirement: Credential logging prohibition
Application logs SHALL NOT contain raw values of access tokens, refresh tokens, JWT strings, Authorization header values, OAuth client secrets, database connection strings containing passwords, or VAPID private keys. Logs MAY contain user IDs, token family IDs, request IDs, failure reason codes (without embedded credentials), and token expiry timestamps.

#### Scenario: Failed token rotation is logged
- **WHEN** a refresh token rotation fails due to reuse detection
- **THEN** the application logs the user ID, token family ID, and failure reason, but does NOT log the raw token value or its hash
