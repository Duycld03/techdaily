# Spec Delta: Auth

## MODIFIED Requirements

### Requirement: Google OAuth Password Setup & State Visibility
The system SHALL expose whether an authenticated user account has an active password configured (`hasPassword`) and allow users with Google OAuth accounts (`isGoogleLinked: true`) to establish or update an email login password without providing an existing password (`currentPassword`).

For accounts linked to Google OAuth (`!string.IsNullOrEmpty(user.GoogleSubjectId)`), the password update endpoint (`PUT /api/v1/user/change-password`) SHALL NOT require `currentPassword`, regardless of whether `hasPassword` is `false` or `true`.

For standard accounts without Google OAuth linkage (`string.IsNullOrEmpty(user.GoogleSubjectId)`), the password update endpoint SHALL strictly require and verify `currentPassword` whenever `hasPassword` is `true`.

#### Scenario: User with Google account checks password status
- **WHEN** an authenticated user calls `GET /api/v1/user/profile`
- **THEN** the system returns `hasPassword: false` and `isGoogleLinked: true` when no PBKDF2 password hash is present.

#### Scenario: User with Google account creates first password
- **WHEN** user sends `PUT /api/v1/user/change-password` with `newPassword` (length >= 6) and no `currentPassword`
- **THEN** the system hashes the new password with PBKDF2 (100,000 iterations, 16-byte random salt), updates `user.PasswordHash`, and returns `200 OK`.
- **THEN** subsequent profile requests return `hasPassword: true`.

#### Scenario: User with Google account updates existing password without current password
- **WHEN** a user with `isGoogleLinked: true` and `hasPassword: true` sends `PUT /api/v1/user/change-password` with a valid `newPassword` (length >= 6) and null or omitted `currentPassword`
- **THEN** the system updates `user.PasswordHash` with the new hashed password and returns `200 OK`.

#### Scenario: Standard user without Google account must provide current password
- **WHEN** a user with `isGoogleLinked: false` and `hasPassword: true` sends `PUT /api/v1/user/change-password` with null, empty, or incorrect `currentPassword`
- **THEN** the system rejects the request with `HTTP 400 Bad Request` and error code `USER_CURRENT_PASSWORD_INCORRECT`.

---

### Requirement: In-App Mobile Handoff Guidance
The system SHALL provide contextual guidance to Google OAuth users prompting them to set or update their email login password on the Profile Security tab without blocking them with a current password requirement.

The "Current Password" input field SHALL ONLY be displayed and required for standard accounts (`!isGoogleLinked && hasPassword`). For Google-linked accounts (`isGoogleLinked: true`), the "Current Password" field SHALL be omitted regardless of `hasPassword` state.

#### Scenario: User visits Profile Security tab without a password
- **WHEN** user navigates to `/profile` and selects the "Security & Password" tab with `hasPassword: false`
- **THEN** the UI displays an informative alert highlighting that setting a password allows logging in with email and password on mobile or other devices.
- **THEN** the UI hides the "Current Password" input field and changes the submit action button to "Set Password" (Thiết Lập Mật Khẩu).

#### Scenario: Google user logs in on desktop for first time
- **WHEN** user signs in via Google OAuth on desktop
- **THEN** a non-intrusive banner appears suggesting the user create a password for easy access on mobile devices.

#### Scenario: Google user visits Profile Security tab with existing password
- **WHEN** a user with `isGoogleLinked: true` and `hasPassword: true` navigates to `/profile` and selects the "Security" tab
- **THEN** the UI hides the "Current Password" input field
- **AND** the Google banner informs the user that their account is linked to Google and their email password can be updated directly without entering their current password
- **AND** the submit action button displays "Update Password" (`profile.update_password_btn`).
