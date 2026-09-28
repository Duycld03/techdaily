# Auth Specification

## MODIFIED Requirements

### Requirement: OTP-Verified Email Registration
Registration SHALL prove control of the email address before any account is created. No `User` row SHALL exist for an unverified email.

The flow has two steps:
1. **Request**: `POST /api/v1/auth/register` accepts name, email, and password (length >= 8). If an account with that email already exists, the system SHALL reject with `AUTH_EMAIL_EXISTS`. Otherwise the system SHALL hold the pending registration (email, name, password hash, locale) in transient server-side storage, issue a registration OTP (see the OTP policy requirement), and email it. The response SHALL NOT include an authenticated session.
2. **Confirm**: `POST /api/v1/auth/register/verify` accepts the email and the OTP. On a valid, unexpired, unconsumed OTP with attempts remaining, the system SHALL create the `User` from the held pending registration, issue access and refresh tokens, and return an authenticated session identical to a successful login. The newly created account SHALL start with an EMPTY library and the system SHALL NOT provision any starter content or run starter-handbook first-time provisioning.

Pending registrations SHALL expire with their OTP and SHALL NEVER surface as usable accounts.

#### Scenario: New email registration requests a verification code
- **WHEN** a visitor submits a unique email, name, and valid password to `POST /api/v1/auth/register`
- **THEN** the system stores the pending registration, emails a 6-digit code, and responds without an authenticated session

#### Scenario: Registration with an existing email is rejected clearly
- **WHEN** a visitor submits an email that already has an account to `POST /api/v1/auth/register`
- **THEN** the system responds with error code `AUTH_EMAIL_EXISTS` and sends no verification email

#### Scenario: Verifying the code creates the account and signs in
- **WHEN** the visitor submits the correct OTP to `POST /api/v1/auth/register/verify`
- **THEN** the system creates the user and returns an access token plus a refresh-token cookie
- **AND** the new account's library is empty, with no starter content provisioned

#### Scenario: No account exists until the code is verified
- **WHEN** a visitor requests a registration OTP but never submits a valid code
- **THEN** no `User` row is created and the pending registration expires with the OTP

#### Scenario: Verification with a wrong code is rejected and counts an attempt
- **WHEN** the visitor submits an incorrect OTP to `POST /api/v1/auth/register/verify`
- **THEN** the system rejects it with an OTP error code, increments the attempt counter, and creates no account
