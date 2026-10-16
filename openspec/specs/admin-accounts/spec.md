# Admin Accounts Specification

## Purpose

Enable authorized administrators to inspect account identity and control account access without taking over user credentials or exposing private learning content.

## Requirements

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
Account details SHALL expose only identifier, name, email, avatar, locale, creation time, status, role descriptor, sign-in method availability and administrative access-state metadata. Credentials and email SHALL remain user-managed. Inspection SHALL NOT expose credential material, private documents, notes or learning answers, or grant support access. Roles SHALL remain distinct from learning-goal metadata. Successful account-control responses SHALL describe the target's actual persisted role, never an assumed Learner role, and SHALL agree with subsequent authorized inspection.

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

#### Scenario: Account control returns the unchanged authoritative role
- **GIVEN** an eligible target holds protected Admin or another valid non-Learner role
- **WHEN** an authorized suspension, reactivation or all-device sign-out succeeds
- **THEN** its account response retains that target's persisted role ID, key and bilingual names with the updated status/version
- **AND** refreshed inspection and directory results agree without falsely showing that the action reassigned the target to Learner.

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
Suspension and role reassignment SHALL NOT leave zero Active, non-deleted accounts in the protected Admin role. Self-suspension and self-role changes SHALL be rejected. Safeguards SHALL use current persisted membership and account state across concurrent role/status commands, not configured user-ID lists. Rejection SHALL preserve account, role and session state. The active-administrator minimum SHALL apply only when the proposed transition removes an Active protected administrator; changing a Suspended target's role SHALL NOT be blocked by that minimum when the active recovery anchor remains unchanged.

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

#### Scenario: Suspended administrator can lose its protected role
- **GIVEN** one Active protected Admin remains and another protected Admin is Suspended
- **WHEN** a permitted actor assigns an eligible different role to the Suspended account with a valid reason and current version
- **THEN** assignment succeeds without changing the remaining Active protected Admin
- **AND** the target stays Suspended with its suspension metadata unchanged, while the actual role change advances its versions and ends old sessions.

#### Scenario: Unchanged assignment is not an administrator-removal transition
- **WHEN** a permitted actor submits the target's current role with its current version, including for the sole Active protected Admin when actor and target differ
- **THEN** the response is the unchanged-role conflict rather than last-admin protection
- **AND** membership, versions and sessions remain unchanged.

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

### Requirement: Role assignment workspace
The existing account workspace SHALL show localized current-role metadata to account inspectors. A role-change action SHALL appear only with effective role-assignment permission and SHALL show target identity, current/selected role, mandatory reason and session consequences in an accessible confirmation form. No role-definition editor SHALL be offered. Selected-role scope SHALL use the response's canonical permissionKeys. Failed or discarded account reloads SHALL retain the conflict gate and draft; unrelated directory refreshes SHALL NOT leave role discovery permanently loading.

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

#### Scenario: Selected role explains its actual supported grants
- **WHEN** role discovery returns a role with recognized permissionKeys and the administrator selects it
- **THEN** the confirmation displays localized explanations for those returned grants
- **AND** an empty permissionKeys list is distinguishable from loading or failed role discovery, without inventing permissions or requiring an alternate response-field name.

#### Scenario: Conflict reload fails
- **GIVEN** assignment returned a concurrency conflict and the role/reason draft is retained
- **WHEN** the administrator requests reload but current account details cannot be obtained
- **THEN** localized retry feedback and the conflict gate remain, the inspected version and draft are not silently replaced, and submission stays disabled
- **AND** only a successful matching reload followed by deliberate confirmation can permit resubmission.

#### Scenario: A reload belongs to an earlier confirmation
- **WHEN** a reload completes after its dialog was closed/reopened, its target changed or the acting session was replaced
- **THEN** it cannot unlock the current confirmation, overwrite its target/version or claim successful conflict resolution.

#### Scenario: Directory changes while role discovery is pending
- **WHEN** the administrator closes a confirmation, refreshes/searches/filters/pages the account directory and reopens confirmation while role discovery is still pending
- **THEN** the latest role request can complete its own loading state independently of that directory change
- **AND** completion shows available options or a localized failure with usable retry, not a permanently loading selector.
