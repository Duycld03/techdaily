# Spec Delta

## Purpose

Provides short-lived access tokens with concurrency-safe refresh token rotation, family-based reuse detection, per-family revocation, and HttpOnly cookie storage to limit the blast radius of stolen credentials.

## ADDED Requirements

### Requirement: Refresh token issuance alongside access token
The authentication endpoints (`/api/v1/auth/login`, `/register`, `/google`) SHALL issue a refresh token alongside the JWT access token. The refresh token SHALL be a cryptographically random 256-bit value, stored as a SHA-256 hash in the database, and delivered to the client as an HttpOnly Secure SameSite=Lax cookie scoped to the auth endpoint path. Each refresh token SHALL have a 30-day absolute expiry.

#### Scenario: Successful login returns access token and sets refresh cookie
- **WHEN** user authenticates via `POST /api/v1/auth/login` with valid credentials
- **THEN** the response body contains `accessToken` (JWT, 60-minute expiry) and the response includes a `Set-Cookie` header for the refresh token with `HttpOnly; Secure; SameSite=Lax; Path=/api/v1/auth; Max-Age=2592000`

#### Scenario: Google OAuth returns access token and sets refresh cookie
- **WHEN** user authenticates via `POST /api/v1/auth/google` with a valid Google ID token
- **THEN** the response body contains `accessToken` and the response includes a refresh token cookie with the same attributes

### Requirement: Concurrency-safe refresh token rotation endpoint
The system SHALL expose `POST /api/v1/auth/refresh` that reads the refresh token from the HttpOnly cookie. Rotation SHALL be performed as an atomic database operation ensuring that a given refresh token can be successfully rotated exactly once. If two concurrent requests present the same refresh token, exactly one SHALL succeed and the other SHALL be treated as token reuse.

The rotation operation SHALL use a conditional database update (`SET UsedAt = @now, ReplacedByTokenId = @newId WHERE Id = @id AND UsedAt IS NULL AND RevokedAt IS NULL AND ExpiresAt > @now`). The `ExpiresAt > @now` condition guarantees that an expired token can never be rotated. If the update affects zero rows, the system SHALL inspect the token state:
- Token has `UsedAt != null` (already rotated) → reuse detected → revoke all tokens in the family including any successor tokens by setting `RevokedAt` on all unrevoked family members → return `HTTP 401` with `AUTH_TOKEN_REUSE_DETECTED`
- Token has `RevokedAt != null` → return `HTTP 401`
- Token has `ExpiresAt <= @now` → return `HTTP 401` with `AUTH_REFRESH_TOKEN_EXPIRED`

On successful rotation, the response SHALL contain a new `accessToken` in the body and set a new refresh token cookie.

#### Scenario: Valid refresh token rotation
- **WHEN** client sends `POST /api/v1/auth/refresh` with a valid, unused, unexpired refresh token cookie
- **THEN** the system returns a new `accessToken` and sets a new refresh token cookie, and the old refresh token is marked as used

#### Scenario: Reuse of a rotated refresh token
- **WHEN** client sends `POST /api/v1/auth/refresh` with a refresh token that has `UsedAt != null` (already consumed in rotation)
- **THEN** the system revokes all unrevoked tokens in that family (including any successor tokens) and returns `HTTP 401` with `{ "code": "AUTH_TOKEN_REUSE_DETECTED" }`

#### Scenario: Concurrent rotation of the same token
- **WHEN** two requests simultaneously present the same refresh token to `POST /api/v1/auth/refresh`
- **THEN** exactly one request succeeds with new tokens; the other triggers reuse detection which revokes the entire family including the just-issued successor token, forcing both clients to re-authenticate. There is no grace period.

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

### Requirement: Frontend transparent token refresh
The HTTP client composable SHALL detect access token expiry (either by a `401` response or by proactively checking the JWT `exp` claim before each request) and automatically call `POST /api/v1/auth/refresh` (the browser sends the HttpOnly cookie automatically). During the refresh call, concurrent requests SHALL be queued and replayed with the new access token. If the refresh fails, the client SHALL execute the existing session cleanup and redirect to `/login`.

#### Scenario: Access token expires during usage
- **WHEN** the access token expires while the user is actively using the application
- **THEN** the HTTP client transparently refreshes the token and retries the failed request without user-visible interruption

#### Scenario: Refresh token also expired
- **WHEN** both the access token and refresh token are expired
- **THEN** the client clears session state, shows the session-expired notification, and redirects to `/login`

#### Scenario: Concurrent requests during token refresh
- **WHEN** multiple API calls are in-flight and the access token expires
- **THEN** only one refresh request is sent; all queued requests are replayed with the new access token

### Requirement: Credential logging prohibition
Application logs SHALL NOT contain raw values of access tokens, refresh tokens, JWT strings, Authorization header values, OAuth client secrets, database connection strings containing passwords, or VAPID private keys. Logs MAY contain user IDs, token family IDs, request IDs, failure reason codes (without embedded credentials), and token expiry timestamps.

#### Scenario: Failed token rotation is logged
- **WHEN** a refresh token rotation fails due to reuse detection
- **THEN** the application logs the user ID, token family ID, and failure reason, but does NOT log the raw token value or its hash
