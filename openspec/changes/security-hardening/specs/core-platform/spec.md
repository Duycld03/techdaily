# Spec Delta

## MODIFIED Requirements

### Requirement: Standard Email & Password Authentication
The system SHALL allow users to register an account with email, password (min 8 characters), full name, and preferred locale (`POST /api/v1/auth/register`), securely hash passwords using PBKDF2 with SHA-256 (16-byte random salt, 600,000 iterations), and authenticate users via email and password (`POST /api/v1/auth/login`), issuing a 256-bit JWT bearer token upon successful verification. On login, if the stored password hash uses fewer than 600,000 iterations, the system SHALL transparently rehash the password with 600,000 iterations after successful verification.

#### Scenario: User registers with valid email and password
- **WHEN** visitor sends `POST /api/v1/auth/register` with valid email, name, and password >= 8 characters
- **THEN** system provisions user entity with PBKDF2 password hash (600,000 iterations), creates user learning stats, and returns `201 Created` with JWT token and refresh token.

#### Scenario: User authenticates with registered credentials
- **WHEN** user sends `POST /api/v1/auth/login` with registered email and correct password
- **THEN** system verifies hash and returns `200 OK` with JWT bearer token, refresh token, and user profile.

#### Scenario: Existing user with legacy iteration count logs in
- **WHEN** user with a password hashed at 100,000 iterations sends `POST /api/v1/auth/login` with correct password
- **THEN** system verifies the password against the stored hash, rehashes with 600,000 iterations, persists the updated hash, and returns the normal login response

#### Scenario: User attempts to register with password shorter than 8 characters
- **WHEN** visitor sends `POST /api/v1/auth/register` with a password of 7 characters or fewer
- **THEN** system returns `HTTP 400` with error code `AUTH_PASSWORD_TOO_SHORT`
