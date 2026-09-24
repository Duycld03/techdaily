# Spec Delta: refresh-tokens

## MODIFIED Requirements

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

#### Scenario: Refresh token is invalid or expired during navigation
- **WHEN** user navigates to an authenticated route and token refresh fails with `401 Unauthorized`
- **THEN** the client purges session storage, clears cookies, displays a session-expired warning, and redirects to `/login` with the attempted route preserved in query parameter `redirect`

### Requirement: Refresh token issuance alongside access token
The authentication endpoints (`/api/v1/auth/login`, `/register`, `/google`, `/refresh`) SHALL issue a refresh token alongside the JWT access token. The refresh token SHALL be delivered to the client as an HttpOnly SameSite=Lax cookie scoped to the auth endpoint path (`Path=/api/v1/auth`).

In HTTPS environments (detected via direct TLS or `X-Forwarded-Proto: https`), the cookie SHALL include the `Secure` attribute. In local or network development environments communicating over plain HTTP, the `Secure` attribute SHALL be omitted so that browsers do not reject the cookie due to an insecure context.

#### Scenario: Login over plain HTTP in local/LAN development
- **WHEN** client authenticates via `POST /api/v1/auth/login` over plain HTTP (e.g., `http://localhost:5000` or `http://192.168.x.x:5000`)
- **THEN** the `Set-Cookie` header for `refreshToken` does NOT include the `secure` flag, allowing the browser to accept and persist the cookie

#### Scenario: Login over HTTPS in production
- **WHEN** client authenticates via `POST /api/v1/auth/login` over HTTPS or through a reverse proxy supplying `X-Forwarded-Proto: https`
- **THEN** the `Set-Cookie` header for `refreshToken` includes `Secure; HttpOnly; SameSite=Lax; Path=/api/v1/auth`
