# Spec Delta

## MODIFIED Requirements

### Requirement: Concurrency-safe refresh token rotation endpoint
The system SHALL expose `POST /api/v1/auth/refresh` that reads the refresh token from the HttpOnly cookie. Rotation SHALL be performed as an atomic database operation ensuring that a given refresh token can be successfully rotated into a new successor token.

The multi-tab concurrent reuse grace window SHALL be **60 seconds** (`@now - UsedAt <= 60 seconds`):
- **Within Grace Window (`@now - UsedAt <= 60 seconds`)**: The system SHALL treat this as a legitimate concurrent request (e.g. from racing browser tabs, rapid HMR page reloads, or parallel API calls during dashboard load). The system SHALL NOT revoke the family; it SHALL issue and return a valid access token corresponding to the successor token (`ReplacedByTokenId`).
- **Outside Grace Window (`@now - UsedAt > 60 seconds`)**: The system SHALL treat this as token reuse/theft. It SHALL revoke all tokens in the family by setting `RevokedAt` on all unrevoked family members and return `HTTP 401` with `AUTH_TOKEN_REUSE_DETECTED`.

#### Scenario: Concurrent rotation of the same token within 60-second grace window
- **WHEN** multiple browser tabs or concurrent requests present the same already-used refresh token within 60 seconds of initial rotation
- **THEN** the subsequent requests receive a valid access token without triggering family revocation
- **AND** the user session remains intact across all tabs.

#### Scenario: Reuse outside 60-second grace window triggers family revocation
- **WHEN** client presents a refresh token that was consumed more than 60 seconds ago (`UsedAt != null`)
- **THEN** the system revokes all unrevoked tokens in that family and returns `HTTP 401` with `{ "code": "AUTH_TOKEN_REUSE_DETECTED" }`.

### Requirement: Frontend transparent token refresh with cross-tab coordination
The HTTP client composable and frontend route middleware SHALL detect access token expiry and coordinate `POST /api/v1/auth/refresh` transparently across browser tabs and client-side navigations.

1. **Proactive Refresh Lead Time**: The HTTP client SHALL proactively initiate background token refresh when the access token has less than **5 minutes (300 seconds)** remaining before expiration (`exp * 1000 - Date.now() <= 300_000 ms`), ensuring seamless rotation before requests encounter hard expiration.
2. **SSR Route Guard Resilience**: The Nuxt route middleware (`auth.global.ts`) SHALL NOT execute an immediate server-side hard redirect to `/login` if client-side session credentials or refresh token cookies may be present. SSR SHALL yield to client hydration to allow `tryRefreshToken()` to verify or restore the session before rejecting navigation.
3. **Transient Network Error Resilience**: Transient network failures, server restarts, or `502`/`503` gateway responses encountered by the HTTP client SHALL NOT clear the user's session credentials (`clearSession`) or trigger an unprompted logout; only an authentic terminal `401 Unauthorized` with failed refresh rotation SHALL invalidate the session.

#### Scenario: Client proactively refreshes token 5 minutes before expiration
- **WHEN** an authenticated user issues an API request and the access token has less than 5 minutes remaining before expiration
- **THEN** the client proactively initiates background token refresh and updates stored tokens without interrupting the user's workflow.

#### Scenario: SSR route middleware defers to client hydration when cookies are in transition
- **WHEN** an authenticated user refreshes a protected page or returns via browser navigation
- **THEN** SSR does not immediately abort and redirect to `/login`
- **AND** client hydration validates local storage and refreshes the token seamlessly.

#### Scenario: Transient backend restart during development does not log out user
- **WHEN** the backend API restarts (e.g. during code watch or compilation) and returns a connection error or 502/503
- **THEN** the frontend client preserves existing session tokens and does NOT purge credentials or redirect to `/login`.
