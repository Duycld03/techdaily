# Spec Delta

## MODIFIED Requirements

### Requirement: Extended Access Token Lifespan Matching Curriculum
The authentication endpoints (`/api/v1/auth/login`, `/register`, `/google`) SHALL issue JWT access tokens with a 60-minute lifetime. Long-lived session continuity SHALL be provided by refresh tokens (see `refresh-tokens` capability) rather than long-lived access tokens.

#### Scenario: User authenticates successfully
- **WHEN** user signs in via email/password or Google OAuth
- **THEN** the returned JWT access token has an expiration set to 60 minutes from creation time

### Requirement: Machine-Readable Authentication Error Codes
The authentication and user account management endpoints (`/api/v1/auth/*` and `/api/v1/user/*`) SHALL return standardized error codes for all credential and validation failures:
- `AUTH_EMAIL_PASSWORD_REQUIRED`: Email or password omitted.
- `AUTH_PASSWORD_TOO_SHORT`: Password length less than 8 characters.
- `AUTH_EMAIL_EXISTS`: Registered account with email already present.
- `AUTH_INVALID_CREDENTIALS`: Email not found or password verification failed.
- `AUTH_GOOGLE_TOKEN_INVALID`: Google ID token invalid or signature verification failed. The error response SHALL NOT include internal exception messages or stack traces.
- `AUTH_GOOGLE_NOT_CONFIGURED`: Google Client ID unconfigured on server.
- `USER_CURRENT_PASSWORD_INCORRECT`: Supplied current password failed verification.
- `USER_NEW_PASSWORD_TOO_SHORT`: New password length less than 8 characters.
- `AUTH_TOKEN_REUSE_DETECTED`: A previously-rotated refresh token was reused (potential theft).
- `AUTH_REFRESH_TOKEN_EXPIRED`: The refresh token has expired.

#### Scenario: User attempts login with wrong password
- **WHEN** user submits invalid credentials to `POST /api/v1/auth/login`
- **THEN** server returns HTTP 400 with `{ "code": "AUTH_INVALID_CREDENTIALS", "error": "Invalid email or password." }`

#### Scenario: User registers with an existing email
- **WHEN** user submits duplicate email to `POST /api/v1/auth/register`
- **THEN** server returns HTTP 400 with `{ "code": "AUTH_EMAIL_EXISTS", "error": "An account with this email already exists." }`

#### Scenario: Google token validation fails
- **WHEN** client sends an invalid Google ID token to `POST /api/v1/auth/google`
- **THEN** server returns HTTP 400 with `{ "code": "AUTH_GOOGLE_TOKEN_INVALID", "error": "Invalid Google token." }` without leaking internal exception details

### Requirement: Google OAuth Password Setup & State Visibility
The system SHALL expose whether an authenticated user account has an active password configured (`hasPassword`) and allow users with Google OAuth accounts (`isGoogleLinked: true`) to establish or update an email login password without providing an existing password (`currentPassword`).

For accounts linked to Google OAuth (`!string.IsNullOrEmpty(user.GoogleSubjectId)`), the password update endpoint (`PUT /api/v1/user/change-password`) SHALL NOT require `currentPassword`, regardless of whether `hasPassword` is `false` or `true`.

For standard accounts without Google OAuth linkage (`string.IsNullOrEmpty(user.GoogleSubjectId)`), the password update endpoint SHALL strictly require and verify `currentPassword` whenever `hasPassword` is `true`.

#### Scenario: User with Google account checks password status
- **WHEN** an authenticated user calls `GET /api/v1/user/profile`
- **THEN** the system returns `hasPassword: false` and `isGoogleLinked: true` when no PBKDF2 password hash is present.

#### Scenario: User with Google account creates first password
- **WHEN** user sends `PUT /api/v1/user/change-password` with `newPassword` (length >= 8) and no `currentPassword`
- **THEN** the system hashes the new password with PBKDF2-HMAC-SHA256 (600,000 iterations, 16-byte random salt), updates `user.PasswordHash`, and returns `200 OK`.
- **THEN** subsequent profile requests return `hasPassword: true`.

#### Scenario: User with Google account updates existing password without current password
- **WHEN** a user with `isGoogleLinked: true` and `hasPassword: true` sends `PUT /api/v1/user/change-password` with a valid `newPassword` (length >= 8) and null or omitted `currentPassword`
- **THEN** the system updates `user.PasswordHash` with the new hashed password and returns `200 OK`.

#### Scenario: Standard user without Google account must provide current password
- **WHEN** a user with `isGoogleLinked: false` and `hasPassword: true` sends `PUT /api/v1/user/change-password` with null, empty, or incorrect `currentPassword`
- **THEN** the system rejects the request with `HTTP 400 Bad Request` and error code `USER_CURRENT_PASSWORD_INCORRECT`.

## ADDED Requirements

### Requirement: Mandatory secret configuration at startup
The application SHALL fail fast during startup if the `Jwt:Secret` configuration value is missing, empty, or has fewer than 256 bits of cryptographically random entropy. The `Jwt:Secret` value MUST be generated using a CSPRNG; human-readable, example, or default secrets SHALL NOT be used. The application SHALL NOT fall back to any hardcoded default JWT signing key. The same fail-fast behavior SHALL apply to VAPID key configuration (`WebPush:PrivateKey`, `WebPush:PublicKey`).

#### Scenario: Application starts without JWT secret
- **WHEN** the application starts with `Jwt:Secret` unset or empty
- **THEN** the application throws an exception during startup and does not begin accepting HTTP requests

#### Scenario: Application starts with JWT secret configured
- **WHEN** the application starts with `Jwt:Secret` set to a CSPRNG-generated value with at least 256 bits of entropy
- **THEN** the application starts normally and uses the configured secret for JWT signing

### Requirement: Production CORS origin isolation
The CORS policy SHALL load allowed origins exclusively from configuration (`Cors:AllowedOrigins`). The policy SHALL NOT include localhost or loopback origins unless explicitly listed in the configuration for that environment.

#### Scenario: Production deployment with configured origins
- **WHEN** the application runs with `Cors:AllowedOrigins` set to `["https://techdaily.duckdns.org"]`
- **THEN** CORS preflight and actual requests from `http://localhost:3000` are rejected

#### Scenario: Development with localhost origins
- **WHEN** the application runs with `Cors:AllowedOrigins` including localhost entries
- **THEN** requests from those localhost origins are permitted
