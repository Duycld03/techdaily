# Admin Accounts Specification

## Purpose

Enable authorized administrators to inspect account identity and control account access without taking over user credentials or exposing private learning content.

## Requirements

### Requirement: Paginated account directory
Authorized administrators SHALL search existing non-deleted accounts by name, email or identifier, filter by Active/Suspended status, and receive bounded pages with a total count and stable ordering. Unverified pending registrations SHALL NOT appear as accounts.

#### Scenario: Search and filter accounts
- **WHEN** an administrator searches an email fragment and selects Suspended
- **THEN** only matching suspended accounts appear with identity, status and creation time
- **AND** the response includes pagination metadata and a stable creation-time/identifier ordering.

#### Scenario: No matching accounts
- **WHEN** no account matches the current criteria
- **THEN** an empty result and zero count are returned without fabricated accounts or changing filters.

#### Scenario: Deleted and unverified identities
- **WHEN** an administrator searches for a soft-deleted account or an unverified pending registration
- **THEN** it is absent from the directory and cannot be selected for account actions.

### Requirement: Bounded account inspection
Account details SHALL expose only identifier, name, email, avatar, locale, creation time, status, sign-in method availability, and administrative access-state metadata. Email and credentials SHALL remain user-managed. These endpoints SHALL NOT expose credential material, personal document content, notes or learning answers, or grant support access.

#### Scenario: Inspect account identity
- **WHEN** an administrator selects an account
- **THEN** details show its identity, available password/Google sign-in methods, status and latest suspension metadata
- **AND** no password hash, OAuth subject, token, private document body or personal learning record is returned.

#### Scenario: Attempt credential or identity editing
- **WHEN** an administrator attempts to submit an email, password or sign-in linkage change through account administration
- **THEN** the operation is rejected without changing those fields
- **AND** the account interface offers only the authorized access-control actions.

#### Scenario: Personal content remains owner-restricted
- **WHEN** an administrator inspects an account or requests that user's personal document through a learner endpoint
- **THEN** account inspection grants no document-content permission
- **AND** existing owner isolation continues to apply.

### Requirement: Account suspension and reactivation
Administrators SHALL suspend an Active account or reactivate a Suspended account with a required reason. Suspension SHALL preserve the account and its learning data while preventing authenticated use. Reactivation SHALL permit fresh sign-in without restoring revoked sessions or changing credentials or administrator designation.

#### Scenario: Suspend an active account
- **WHEN** an administrator confirms suspension with a valid reason and current account version
- **THEN** the account becomes Suspended, its suspension actor/time are recorded, and existing sessions are revoked
- **AND** its documents and learning progress are preserved.

#### Scenario: Reactivate a suspended account
- **WHEN** an administrator confirms reactivation with a valid reason and current account version
- **THEN** the account becomes Active and can sign in again
- **AND** pre-suspension sessions remain invalid.

#### Scenario: Account no longer exists
- **WHEN** an administrator attempts an action on a missing or soft-deleted account
- **THEN** the server returns a not-found result without creating or restoring an account.

### Requirement: Administrator all-device sign-out
Administrators SHALL revoke every session of an Active account with a required reason. After the revocation commits, previous refresh sessions and unexpired access tokens SHALL fail subsequent authentication checks across learner and admin APIs. The account SHALL remain Active and permit fresh sign-in.

#### Scenario: Revoke sessions across devices
- **WHEN** an administrator forces sign-out for an account with two active devices
- **THEN** both devices' old access tokens and refresh cookies are rejected on subsequent requests
- **AND** neither token expiry nor a voluntary browser logout is required.

#### Scenario: Fresh sign-in after revocation
- **WHEN** the affected Active account signs in again after the forced sign-out commits
- **THEN** the new session is valid and the old sessions remain rejected.

#### Scenario: Revoke own sessions deliberately
- **WHEN** an administrator confirms all-device sign-out for their own Active account
- **THEN** all of their sessions end, including the current admin session
- **AND** the current browser clears privileged state and returns to admin login.

### Requirement: Administrative lockout protection
Suspension SHALL reject a target equal to the acting administrator and SHALL NOT leave zero active, non-deleted configured administrators. Safeguards SHALL be enforced by the server against the current account and designation state, including concurrent requests. Rejected operations SHALL NOT change account or session state.

#### Scenario: Administrator attempts self-suspension
- **WHEN** an administrator submits a suspension for their own account through the UI or directly through the API
- **THEN** the server rejects it with a machine-readable protection error and leaves the session usable.

#### Scenario: Last active administrator
- **WHEN** a suspension would remove the last active configured administrator
- **THEN** the server rejects it and retains administrative access.

#### Scenario: Concurrent administrator suspensions
- **WHEN** concurrent requests attempt to suspend the remaining configured administrators
- **THEN** the committed outcome retains at least one active configured administrator
- **AND** a request from an actor suspended by the competing operation cannot authorize a later mutation.

### Requirement: Validated reasons and stale-action rejection
Account mutations SHALL require a trimmed, non-empty reason of at most 500 characters and the account version last inspected by the administrator. Missing or invalid fields SHALL be rejected. A stale version or invalid status transition SHALL return a conflict without changing state or repeating session revocation.

#### Scenario: Missing reason
- **WHEN** an administrator submits suspension, reactivation or all-device sign-out with a blank reason
- **THEN** validation blocks the action in the interface and the server independently rejects a direct request.

#### Scenario: Competing account update
- **WHEN** another action changes the selected account before a pending confirmation is submitted
- **THEN** the server returns a conflict and the interface offers reload while preserving the entered reason.

#### Scenario: Retry after a lost success response
- **WHEN** the same account action is resubmitted with its old version after the first request committed
- **THEN** the retry cannot repeat the mutation or invalidate sessions issued after the original commit
- **AND** reloading reveals the current account state.

### Requirement: Account workspace interaction states
The account workspace SHALL provide localized loading, empty, failed, selected-account and submitting states. Access-control actions SHALL use accessible confirmation forms showing the account, consequence and required reason. Errors SHALL preserve input, and success SHALL refresh the affected account and directory.

#### Scenario: Cancel a confirmation
- **WHEN** an administrator cancels a suspension or sign-out confirmation
- **THEN** no mutation is submitted and focus returns to the initiating control.

#### Scenario: Submission fails
- **WHEN** a valid account action fails due to validation, conflict or temporary server failure
- **THEN** localized feedback explains the failure, the reason remains available, and the interface does not claim success.

#### Scenario: Submission is in flight
- **WHEN** an account action is awaiting its response
- **THEN** its confirmation cannot submit duplicate requests
- **AND** a result arriving after logout or account replacement cannot repopulate privileged account data.
