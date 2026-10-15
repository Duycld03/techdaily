# Spec Delta

## MODIFIED Requirements

### Requirement: Paginated account directory
Authorized administrators SHALL search non-deleted accounts by name, email or identifier, filter by Active/Suspended status, and receive bounded pages with total count and stable ordering. Entries SHALL include their authoritative role descriptor alongside identity, status and creation time. Unverified pending registrations SHALL NOT appear as accounts.

#### Scenario: Search and filter accounts
- **WHEN** an administrator searches an email fragment and selects Suspended
- **THEN** only matching suspended accounts appear with identity, status, role and creation time
- **AND** pagination metadata and stable creation-time/identifier ordering are returned.

#### Scenario: No matching accounts
- **WHEN** no account matches the criteria
- **THEN** an empty result and zero count are returned without fabricated accounts or changed filters.

#### Scenario: Deleted and unverified identities
- **WHEN** an administrator searches a soft-deleted account or unverified pending registration
- **THEN** it is absent from the directory and cannot be selected for actions.

### Requirement: Bounded account inspection
Account details SHALL expose only identifier, name, email, avatar, locale, creation time, status, role descriptor, sign-in method availability and administrative access-state metadata. Credentials and email SHALL remain user-managed. Inspection SHALL NOT expose credential material, private documents, notes or learning answers, or grant support access. Roles SHALL remain distinct from learning-goal metadata.

#### Scenario: Inspect account identity
- **WHEN** an administrator selects an account
- **THEN** details show identity, role, password/Google sign-in availability, status and latest suspension metadata
- **AND** no password hash, OAuth subject, token, private document body or learning record is returned.

#### Scenario: Attempt credential or identity editing
- **WHEN** an administrator submits an email, password or sign-in linkage change through account administration
- **THEN** those changes are rejected without mutation
- **AND** only separately authorized account-control and role-assignment actions are offered.

#### Scenario: Personal content remains owner-restricted
- **WHEN** an administrator inspects an account or requests its personal document through a learner endpoint
- **THEN** inspection grants no document-content permission
- **AND** owner isolation continues to apply.

### Requirement: Administrative lockout protection
Suspension and role reassignment SHALL NOT leave zero Active, non-deleted accounts in the protected Admin role. Self-suspension and self-role changes SHALL be rejected. Safeguards SHALL use current persisted membership and account state across concurrent role/status commands, not configured user-ID lists. Rejection SHALL preserve account, role and session state.

#### Scenario: Administrator attempts self-suspension
- **WHEN** an administrator submits suspension for their own account through UI or API
- **THEN** a machine-readable protection error is returned and the session remains usable.

#### Scenario: Last active administrator
- **WHEN** suspension or role reassignment would remove the last Active, non-deleted protected Admin
- **THEN** the operation is rejected and administrative access is retained.

#### Scenario: Concurrent administrator suspensions
- **WHEN** concurrent commands attempt to suspend the remaining protected administrators
- **THEN** at least one active protected administrator remains
- **AND** an actor suspended by a competing operation cannot authorize a later mutation.

#### Scenario: Concurrent demotion and suspension
- **WHEN** role reassignment races with suspension or another demotion of the remaining protected administrators
- **THEN** the committed outcome retains at least one active protected Admin
- **AND** every committed command used current actor grants and target version.

### Requirement: Validated reasons and stale-action rejection
Account mutations, including role reassignment, SHALL require a trimmed, non-empty reason of at most 500 characters and the inspected account version. Missing or invalid fields SHALL be rejected. A stale version, unchanged-role assignment or invalid status transition SHALL return conflict without changing state or repeating session revocation.

#### Scenario: Missing reason
- **WHEN** an administrator submits suspension, reactivation, all-device sign-out or role assignment with a blank reason
- **THEN** the interface blocks submission and the server independently rejects a direct request.

#### Scenario: Competing account update
- **WHEN** another command changes the selected account before confirmation
- **THEN** conflict feedback offers reload while preserving entered reason and pending role selection
- **AND** submission remains gated until current state is successfully reloaded and the administrator deliberately reconfirms.

#### Scenario: Retry after a lost success response
- **WHEN** an account action is resubmitted with its old version after its first commit
- **THEN** it cannot repeat the mutation or invalidate sessions issued after that commit
- **AND** reloading reveals current state.

## ADDED Requirements

### Requirement: Role assignment workspace
The existing account workspace SHALL show localized current-role metadata to account inspectors. A role-change action SHALL appear only with effective role-assignment permission and SHALL show target identity, current/selected role, mandatory reason and session consequences in an accessible confirmation form. No role-definition editor SHALL be offered.

#### Scenario: Account inspector cannot assign roles
- **WHEN** the actor has ManageAccounts but lacks ManageRoles
- **THEN** account role labels remain visible, but role options are not fetched and no role-change action is available
- **AND** existing authorized status/session actions remain independent.

#### Scenario: Assignment succeeds
- **WHEN** a permitted administrator confirms another account's valid role change
- **THEN** the account detail and directory refresh to the committed role/version
- **AND** feedback explains that previous target sessions ended, without signing out the acting administrator.

#### Scenario: Role form validation or service failure
- **WHEN** submission fails because of validation, conflict or temporary service failure
- **THEN** selected role and reason remain available with localized feedback
- **AND** success is not claimed; a failed reload cannot silently replace the inspected version or enable resubmission.

#### Scenario: Keyboard confirmation and cancellation
- **WHEN** the form is used with keyboard or assistive technology
- **THEN** the actual custom combobox and reason field expose labels and associated errors, dialog focus is contained, and background controls are unavailable
- **AND** the first Escape closes an open role list without also closing the dialog; cancellation restores focus to the surviving initiating control.

#### Scenario: Late result follows account replacement
- **WHEN** a role-option or assignment response arrives after logout, definitive denial or account replacement
- **THEN** its result is discarded and cannot repopulate privileged state.

#### Scenario: Bilingual responsive role confirmation
- **WHEN** current/selected role labels, protection errors and the 500-character reason are viewed in English and Vietnamese at desktop and mobile widths
- **THEN** the identity, selected value, warning, errors and actions remain readable and operable without clipping or collision
- **AND** the full role name is obtainable without hover-only interaction, while action labels and compact badges retain the project's no-wrap invariant.
