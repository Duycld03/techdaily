# Spec Delta

## MODIFIED Requirements

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
