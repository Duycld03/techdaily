# Proposal

## Why

Review of the archived database-backed roles change found fifteen integration gaps: valid renewed administrators can be rejected, some catalog writes can outlive revoked authority, startup can advertise a healthy API without its ingestion consumer, and account/audit/UI contracts are inconsistent. Close these gaps against the existing role model rather than introducing another authorization mechanism; passing scoped tests did not exercise these scenarios.

## What Changes

- Carry the validated principal's actual session version through publication, document retry and catalog source replacement, and require it at affected internal command boundaries instead of defaulting to version 1. Add final current-actor admission to catalog hide and explicit review without making a separate review record a publication prerequisite.
- Persist eligible authorized role-command rejections and publication rejections through the existing clean audit boundary after any failed transaction is rolled back and disposed. Keep successful mutation/audit commits atomic and retain existing safe error codes.
- Return the target's persisted role in account-control responses without a fabricated Learner descriptor. Apply the protected-admin minimum only to transitions that actually remove an Active protected administrator, allowing a Suspended administrator's role to change while suspension remains intact.
- Distinguish valid schema awaiting initialization from missing/unreadable receipt schema and invalid required grant sets. Evaluate authenticated administrative availability before permission denial; preserve anonymous/invalid-session 401 and post-initialization insufficient-permission 403.
- **BREAKING (operational)**: Fail web/worker host startup when required authorization readiness cannot be established, rather than running a host whose PDF worker has returned permanently. Valid schema without an initialization receipt still permits real Learner registration/sign-in. Recovery from failed startup requires a supervisor/operator restart; no automatic processing retry loop or administrator recovery path is introduced.
- Enforce destination-specific admission after refresh and retry, and authorize return URLs by their validated pathname while preserving safe query/hash context. Reuse the existing destination mapping, including Categories, rather than adding another route policy.
- Align role-option consumers with the existing `permissionKeys` response, preserve the conflict gate until a matching account reload succeeds, and decouple role-option loading ownership from account-directory refreshes so loading always settles or can be retried safely.
- Add behavior-focused regressions for all fifteen findings, real PostgreSQL interleavings and authenticated runtime/browser evidence during implementation; update the existing administrator/deployment guidance after those checks. This proposal itself performs no implementation or runtime state changes.

## Capabilities

### New Capabilities

None. The existing capabilities already own the affected behavior.

### Modified Capabilities

- `auth`: Valid post-revocation sessions with versions greater than 1 remain usable at every affected privileged command, while stale sessions remain rejected; administrative grant-denial contracts explicitly follow initialization/readiness precedence.
- `role-authorization`: Exact required grant integrity, receipt/schema availability classification and authenticated initialization-unavailable precedence.
- `curated-catalog`: Current-authority admission for hide and explicit review, preserving publication eligibility and visibility behavior.
- `document-ingestion`: Startup readiness and ingestion-consumer availability cannot diverge; queued recovery remains unchanged after a successful restart.
- `admin-accounts`: Authoritative role descriptors in action results, non-destructive Suspended-role assignment, canonical permission explanations and recoverable role-form states.
- `admin-audit`: Durable authorized early rejections and publication rejections independent of rolled-back mutation transactions.
- `admin-workspace`: Destination-specific renewal/retry admission and safe return-path query/hash preservation.

## Impact

- Backend: existing Minimal API command construction, Application catalog/account handlers and DTO projection, `AdministrativeUnitOfWork`, `AuthorizationReadinessService`, and `Program`/worker startup ordering. Internal required-argument changes migrate all callers and fixtures; public request bodies, JWT claims and credential storage remain unchanged.
- Frontend: existing admin access/users stores, route utilities/middleware, authorization boundary, catalog outlet guard and role-assignment modal, with matching English/Vietnamese feedback and behavioral tests.
- Verification: current HTTP/session tests must use non-default session versions and real producer DTO shapes; new concurrency cases require an isolated PostgreSQL fixture and observed runtime outcomes rather than mock forwarding or source/CSS assertions.
- Operations/docs: update `docs/admin-workspace.md` and relevant existing deployment guidance for failed-startup restart requirements and accurate role/bootstrap instructions. No role/grant migration, privilege import, account provisioning, credential reset, new dependency or production deployment is planned.
- Adjacent work: `harden-frontend-quality-gates` owns lint/type tooling and CI enforcement. This change neither edits that change's artifacts nor changes its quality baseline, toolchain or CI scope; application fixes must remain compatible with whichever gates are present when applied.
