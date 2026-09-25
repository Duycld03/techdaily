# Spec Delta: Auth

## ADDED Requirements

### Requirement: OTP-Verified Email Registration
Registration SHALL prove control of the email address before any account is created. No `User` row SHALL exist for an unverified email.

The flow has two steps:
1. **Request**: `POST /api/v1/auth/register` accepts name, email, and password (length >= 8). If an account with that email already exists, the system SHALL reject with `AUTH_EMAIL_EXISTS`. Otherwise the system SHALL hold the pending registration (email, name, password hash, locale) in transient server-side storage, issue a registration OTP (see the OTP policy requirement), and email it. The response SHALL NOT include an authenticated session.
2. **Confirm**: `POST /api/v1/auth/register/verify` accepts the email and the OTP. On a valid, unexpired, unconsumed OTP with attempts remaining, the system SHALL create the `User` from the held pending registration, run first-time provisioning, issue access and refresh tokens, and return an authenticated session identical to a successful login.

Pending registrations SHALL expire with their OTP and SHALL NEVER surface as usable accounts.

#### Scenario: New email registration requests a verification code
- **WHEN** a visitor submits a unique email, name, and valid password to `POST /api/v1/auth/register`
- **THEN** the system stores the pending registration, emails a 6-digit code, and responds without an authenticated session

#### Scenario: Registration with an existing email is rejected clearly
- **WHEN** a visitor submits an email that already has an account to `POST /api/v1/auth/register`
- **THEN** the system responds with error code `AUTH_EMAIL_EXISTS` and sends no verification email

#### Scenario: Verifying the code creates the account and signs in
- **WHEN** the visitor submits the correct OTP to `POST /api/v1/auth/register/verify`
- **THEN** the system creates the user, provisions their starter content, and returns an access token plus a refresh-token cookie

#### Scenario: No account exists until the code is verified
- **WHEN** a visitor requests a registration OTP but never submits a valid code
- **THEN** no `User` row is created and the pending registration expires with the OTP

#### Scenario: Verification with a wrong code is rejected and counts an attempt
- **WHEN** the visitor submits an incorrect OTP to `POST /api/v1/auth/register/verify`
- **THEN** the system rejects it with an OTP error code, increments the attempt counter, and creates no account

### Requirement: One-Time Passcode Issuance and Validation Policy
Email one-time passcodes (OTPs) used for registration and password reset SHALL follow a single issuance and validation policy:
- The code SHALL be a 6-digit numeric value generated with a cryptographically secure random source.
- The code SHALL expire 10 minutes after issuance.
- The code SHALL be stored only as a cryptographic hash (never plaintext) and compared by hash.
- A code SHALL be single-use: once a verification succeeds it SHALL be marked consumed and SHALL NOT verify again.
- The system SHALL allow at most 5 incorrect verification attempts per issued code; on the 5th failure the code SHALL be invalidated and a new request required.
- Resending a code SHALL be rate-limited by a 60-second cooldown per email and purpose; a request inside the cooldown SHALL be rejected with `AUTH_OTP_RESEND_COOLDOWN`.
- OTP request and verification endpoints SHALL be rate-limited to bound abuse.

#### Scenario: Issued code expires after ten minutes
- **WHEN** a code is issued and 10 minutes elapse before it is submitted
- **THEN** verification fails with `AUTH_OTP_EXPIRED`

#### Scenario: Code is invalidated after five wrong attempts
- **WHEN** an incorrect code is submitted 5 times for the same issued OTP
- **THEN** the OTP is invalidated and further submissions fail with `AUTH_OTP_MAX_ATTEMPTS` until a new code is requested

#### Scenario: Resend within cooldown is rejected
- **WHEN** a client requests a new code within 60 seconds of the previous request for the same email and purpose
- **THEN** the system rejects the request with `AUTH_OTP_RESEND_COOLDOWN`

#### Scenario: Consumed code cannot be reused
- **WHEN** a code that already completed a successful verification is submitted again
- **THEN** verification fails with `AUTH_OTP_INVALID`

## MODIFIED Requirements

### Requirement: Email-Based Password Setup for Stranded Mobile Users
The system SHALL let a user reset or establish an email/password credential by proving control of their registered email through a one-time passcode (OTP), without requiring an existing password. This covers both standard accounts that forgot their password and Google-linked accounts that have never set one.

The flow has two steps:
1. **Request**: `POST /api/v1/auth/forgot-password` accepts an email. The system SHALL respond `200 OK` regardless of whether the email is registered (anti-enumeration). When the email maps to an existing account, the system SHALL issue a password-reset OTP (see the OTP policy requirement) and email it to that address.
2. **Confirm**: `POST /api/v1/auth/reset-password` accepts the email, the OTP, and a new password (length >= 8). On a valid, unexpired, unconsumed OTP with attempts remaining, the system SHALL set the account's password hash, mark the OTP consumed, and revoke all of the user's refresh-token families (see `refresh-tokens`).

After a successful reset every session is invalidated: an email/password user SHALL re-authenticate manually with the new password, while a Google-linked user MAY re-authenticate via Google OAuth without a manual password login.

#### Scenario: Unauthenticated visitor requests password initialization on login page
- **WHEN** an unauthenticated visitor on `/login` submits their email via the forgot-password flow
- **THEN** the system responds `200 OK` and, if the email is registered, emails a 6-digit password-reset OTP

#### Scenario: Forgot-password request for an unknown email does not reveal existence
- **WHEN** a visitor submits an email that is not registered to `POST /api/v1/auth/forgot-password`
- **THEN** the system responds `200 OK` with no indication of whether the account exists and sends no email

#### Scenario: User completes a password reset with a valid code
- **WHEN** a user submits their email, the correct OTP, and a new password (length >= 8) to `POST /api/v1/auth/reset-password`
- **THEN** the system updates the password hash, marks the OTP consumed, and revokes all of the user's refresh-token families
- **AND** the response requires the user to sign in again with the new password

#### Scenario: Google-linked account without a password sets one via reset
- **WHEN** a Google-linked user with `hasPassword: false` completes the forgot-password OTP flow with a new password
- **THEN** the system establishes the password hash and subsequent profile requests return `hasPassword: true`
- **AND** the user MAY continue to sign in with Google without a manual password login

#### Scenario: Reset attempt with an invalid or expired code is rejected
- **WHEN** a user submits an incorrect, expired, or already-consumed OTP to `POST /api/v1/auth/reset-password`
- **THEN** the system rejects the request with the corresponding OTP error code and does not change the password

### Requirement: Machine-Readable Authentication Error Codes
The authentication and user account management endpoints (`/api/v1/auth/*` and `/api/v1/user/*`) SHALL return standardized error codes for all credential and validation failures:
- `AUTH_EMAIL_PASSWORD_REQUIRED`: Email or password omitted.
- `AUTH_PASSWORD_TOO_SHORT`: Password length less than 8 characters.
- `AUTH_EMAIL_EXISTS`: Registered account with email already present.
- `AUTH_INVALID_CREDENTIALS`: Email not found or password verification failed.
- `AUTH_GOOGLE_TOKEN_INVALID`: Google ID token invalid, expired, or signature/audience verification failed. This error code SHALL strictly represent external token validation failures and SHALL NOT be returned for internal database connection or user provisioning exceptions.
- `AUTH_GOOGLE_NOT_CONFIGURED`: Google Client ID unconfigured on server.
- `USER_CURRENT_PASSWORD_INCORRECT`: Supplied current password failed verification.
- `USER_NEW_PASSWORD_TOO_SHORT`: New password length less than 8 characters.
- `AUTH_TOKEN_REUSE_DETECTED`: A previously-rotated refresh token was reused (potential theft).
- `AUTH_REFRESH_TOKEN_EXPIRED`: The refresh token has expired.
- `AUTH_OTP_INVALID`: Submitted one-time passcode does not match an active issued code.
- `AUTH_OTP_EXPIRED`: The one-time passcode passed its expiry window.
- `AUTH_OTP_MAX_ATTEMPTS`: Too many incorrect passcode attempts; the code is invalidated and a new one must be requested.
- `AUTH_OTP_RESEND_COOLDOWN`: A new passcode was requested before the resend cooldown elapsed.

Every returned error code SHALL have a corresponding localized message in the system i18n catalog (`en` and `vi`).

#### Scenario: User attempts login with wrong password
- **WHEN** user submits invalid credentials to `POST /api/v1/auth/login`
- **THEN** server returns HTTP 400 with `{ "code": "AUTH_INVALID_CREDENTIALS", "error": "Invalid email or password." }`

#### Scenario: User registers with an existing email
- **WHEN** user submits duplicate email to `POST /api/v1/auth/register`
- **THEN** server returns HTTP 409 with `{ "code": "AUTH_EMAIL_EXISTS", "error": "An account with this email already exists." }`

#### Scenario: Google token validation fails
- **WHEN** client sends an invalid Google ID token to `POST /api/v1/auth/google`
- **THEN** server returns HTTP 400 with `{ "code": "AUTH_GOOGLE_TOKEN_INVALID", "error": "Invalid Google token." }` without leaking internal exception details

#### Scenario: Database connection failure during Google login does not return AUTH_GOOGLE_TOKEN_INVALID
- **WHEN** client sends a valid Google ID token to `POST /api/v1/auth/google` but the database is unreachable or connection fails
- **THEN** the system does NOT return `AUTH_GOOGLE_TOKEN_INVALID`
- **AND** the failure is reported through standard exception and problem details handling.

#### Scenario: OTP verification fails with an expired code
- **WHEN** a user submits an expired one-time passcode to an OTP verification endpoint
- **THEN** the server returns HTTP 400 with `{ "code": "AUTH_OTP_EXPIRED" }` and a localized message
