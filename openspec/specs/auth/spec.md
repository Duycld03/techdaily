# Auth Specification

## Purpose
Provides hybrid authentication, Google OAuth account linkage, and password initialization workflows enabling users on mobile devices or alternative environments to seamlessly authenticate with email and password.

## Requirements

### Requirement: Google OAuth Password Setup & State Visibility
The system SHALL expose whether an authenticated user account has an active password configured (`hasPassword`) and allow users with Google OAuth accounts to establish a password without providing an existing password.

#### Scenario: User with Google account checks password status
- **WHEN** an authenticated user calls `GET /api/v1/user/profile`
- **THEN** the system returns `hasPassword: false` and `isGoogleLinked: true` when no PBKDF2 password hash is present.

#### Scenario: User with Google account creates first password
- **WHEN** user sends `PUT /api/v1/user/change-password` with `newPassword` (length >= 6) and no `currentPassword`
- **THEN** the system hashes the new password with PBKDF2 (100,000 iterations, 16-byte random salt), updates `user.PasswordHash`, and returns `200 OK`.
- **THEN** subsequent profile requests return `hasPassword: true`.

---

### Requirement: In-App Mobile Handoff Guidance
The system SHALL provide contextual guidance to Google OAuth users prompting them to set a password for seamless mobile login.

#### Scenario: User visits Profile Security tab without a password
- **WHEN** user navigates to `/profile` and selects the "Security & Password" tab with `hasPassword: false`
- **THEN** the UI displays an informative alert highlighting that setting a password allows logging in with email and password on mobile or other devices.
- **THEN** the UI hides the "Current Password" input field and changes the submit action button to "Set Password" (Thiết Lập Mật Khẩu).

#### Scenario: Google user logs in on desktop for first time
- **WHEN** user signs in via Google OAuth on desktop
- **THEN** a non-intrusive banner appears suggesting the user create a password for easy access on mobile devices.

---

### Requirement: Email-Based Password Setup for Stranded Mobile Users
The system SHALL allow users on devices where Google OAuth cannot be used to request a secure password setup link or code via their registered Google email.

#### Scenario: Unauthenticated visitor requests password initialization on login page
- **WHEN** an unauthenticated visitor on `/login` submits their Google email via the password setup flow
- **THEN** the system verifies the email exists and generates a password setup instruction token.
