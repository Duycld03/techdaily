# Spec Delta: Refresh Tokens

## ADDED Requirements

### Requirement: Password reset revokes all refresh token families
A successful password reset SHALL revoke every refresh-token family belonging to the affected user, not only the family that initiated the reset, so that a credential change signs the account out of all devices.

#### Scenario: Password reset signs out every device
- **WHEN** a user completes a password reset while holding active refresh-token families on multiple devices
- **THEN** the system sets `RevokedAt` on all unrevoked tokens across every family for that user
- **AND** a subsequent `POST /api/v1/auth/refresh` from any of those devices returns `HTTP 401`

#### Scenario: New session after reset is unaffected
- **WHEN** the user signs in again after the reset (email/password or Google)
- **THEN** a fresh refresh-token family is issued and rotates normally
