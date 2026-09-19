# Spec Delta: Auth

## MODIFIED Requirements

### Requirement: Google OAuth Password Setup & State Visibility
The system SHALL expose whether an authenticated user account has an active password configured (`hasPassword`) and allow users with Google OAuth accounts to establish an initial email login password without providing an existing password (`currentPassword`), while requiring and verifying `currentPassword` for any password update once credentials exist (`hasPassword: true`).

When `user.PasswordHash` is null or empty (`hasPassword: false`), `PUT /api/v1/user/change-password` SHALL allow setting a new password without requiring `currentPassword`.

When `user.PasswordHash` is configured (`hasPassword: true`), `PUT /api/v1/user/change-password` SHALL strictly require and verify `currentPassword` before updating the hash.

#### Scenario: User with Google account checks password status
- **WHEN** an authenticated user calls `GET /api/v1/user/profile`
- **THEN** the system returns `hasPassword: false` and `isGoogleLinked: true` when no PBKDF2 password hash is present.

#### Scenario: User with Google account creates first password
- **WHEN** user sends `PUT /api/v1/user/change-password` with `newPassword` (length >= 6) and no `currentPassword`
- **THEN** the system hashes the new password with PBKDF2 (100,000 iterations, 16-byte random salt), updates `user.PasswordHash`, and returns `200 OK`.
- **THEN** subsequent profile requests return `hasPassword: true`.

#### Scenario: User with established password must provide current password to update
- **WHEN** an authenticated user with `hasPassword: true` sends `PUT /api/v1/user/change-password` with null, empty, or incorrect `currentPassword`
- **THEN** the system rejects the request with `HTTP 400 Bad Request` and error code `USER_CURRENT_PASSWORD_INCORRECT`.

#### Scenario: User with established password successfully updates password with valid current password
- **WHEN** an authenticated user with `hasPassword: true` sends `PUT /api/v1/user/change-password` with valid `currentPassword` and new password (length >= 6)
- **THEN** the system verifies `currentPassword`, hashes the new password with PBKDF2, updates `user.PasswordHash`, and returns `200 OK`.

---

### Requirement: In-App Mobile Handoff Guidance
The system SHALL provide contextual guidance to Google OAuth users prompting them to set an email password on initial login, and transition to standard password management once a password is created.

The Google account setup tip banner SHALL ONLY be displayed when an account is linked to Google and does NOT have a password yet (`isGoogleLinked: true && hasPassword: false`).

The "Current Password" input field SHALL ONLY be displayed and required when the account already has an established password (`hasPassword: true`). When `hasPassword: false`, the field SHALL be omitted.

#### Scenario: User visits Profile Security tab without a password
- **WHEN** user navigates to `/profile` and selects the "Security & Password" tab with `hasPassword: false`
- **THEN** the UI displays an informative alert highlighting that setting a password allows logging in with email and password on mobile or other devices.
- **THEN** the UI hides the "Current Password" input field and changes the submit action button to "Set Password" (Thiết Lập Mật Khẩu).

#### Scenario: Google user logs in on desktop for first time
- **WHEN** user signs in via Google OAuth on desktop
- **THEN** a non-intrusive banner appears suggesting the user create a password for easy access on mobile devices.

#### Scenario: User visits Profile Security tab with established password
- **WHEN** a user navigates to `/profile` and selects the "Security" tab with `hasPassword: true`
- **THEN** the UI hides the Google setup tip banner
- **AND** the UI displays the "Current Password" input field as required
- **AND** the submit action button displays "Update Password" (`profile.update_password_btn`).

#### Scenario: Reactive UI transition upon creating initial password
- **WHEN** a user with `hasPassword: false` submits a new password and receives confirmation
- **THEN** the UI state immediately transitions `hasPassword` to `true`
- **AND** the Google setup tip banner is automatically hidden
- **AND** the "Current Password" input field appears for subsequent updates.
