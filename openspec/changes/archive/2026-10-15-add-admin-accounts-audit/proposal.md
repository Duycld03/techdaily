# Proposal

## Why

DeepPace administrators can manage the catalog and inspect system health, but cannot suspend an account or end all of its sessions. Existing catalog actor fields retain only the latest action, leaving operators without a searchable history of who changed what and why.

## What Changes

- Add an Accounts workspace at `/admin/users` with paginated identity search, account details, Active/Suspended status, suspension/reactivation with a reason, and forced sign-out from every device.
- Enforce account status and session revocation across email/password sign-in, Google sign-in, refresh rotation, and authenticated APIs, including access tokens that have not expired.
- Prevent administrators from suspending themselves or removing the last active configured administrator through account actions.
- Add a read-only Administration log at `/admin/audit` with actor, action, target, UTC timestamp, reason, outcome, and filters. Persist account actions and existing catalog mutations with their corresponding administrative history.
- Extend server capability discovery and admin navigation for account management and audit reading. The existing operator-controlled administrator cohort receives these capabilities; designation remains outside the product UI.
- Keep email and credential management with the user. Account and audit responses exclude credentials, personal document content, and private learning records; this change does not add a document support-access workflow.
- **BREAKING:** Access tokens without the new server-verifiable session version will no longer authorize protected requests. An eligible existing refresh session can transparently obtain a replacement token; a browser without one must sign in again.

## Capabilities

### New Capabilities

- `admin-accounts`: Bounded account inspection, suspension/reactivation, all-device session revocation, and safeguards against administrative lockout.
- `admin-audit`: Durable, privacy-preserving administrative action history and authorized, read-only inspection of account and catalog events.

### Modified Capabilities

- `auth`: Server enforcement of account/session state, operator-controlled permissions for the expanded admin area, and capability discovery for each destination.
- `admin-workspace`: Accounts and Administration log navigation, route admission, and responsive layouts within the existing admin shell.

## Impact

- Domain and persistence: user account state, concurrency/session versions, an append-only audit entity, relational migrations, and indexed account/audit queries.
- Application and Infrastructure: directly injected account/audit use cases, shared mutation/audit transaction boundaries, catalog action recording, and concurrency-safe session issuance/revocation.
- Api: protected account and audit endpoints, additive admin capability discovery, JWT account/version validation, and localized error contracts. Existing learner ownership and catalog lifecycle rules remain authoritative.
- Frontend: account and audit stores/pages, permission-aware navigation, confirmation forms, EN/VI translations, and privileged-state cleanup integrated with existing authentication generations.
- Verification: backend authorization, revocation, concurrency, rollback, and privacy tests; frontend behavioral tests; authenticated Chromium inspection in both locales and themes at desktop and mobile sizes before UI completion.
- Excluded: administrator assignment UI, impersonation, account deletion, administrator email/password editing, billing, job control, audit export/retention controls, secret configuration, and personal document content access.
