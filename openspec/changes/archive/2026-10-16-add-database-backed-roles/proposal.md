# Proposal

## Why

Administrator authority currently comes from deployment user-ID allowlists duplicated across services, and every designated administrator receives all administrative capabilities together. Database-backed roles make account permissions explicit, independently enforceable, and changeable without redeploying configuration, while preserving existing authentication and ownership boundaries.

## What Changes

- Add one required `Users.RoleId` referencing `Roles`, with role grants in `RolePermissions`. Seed `Learner` and protected `Admin` roles; keep the learning-goal field `TargetRole` unrelated to authorization.
- Define supported permission keys in backend code, not executable user-defined permissions. Separate dashboard visibility, catalog management, account controls, audit reads, document operations, and role assignment. Resolve grants from current database state rather than trusting role names, JWT role claims, or browser metadata.
- Add audited user-role assignment in the existing account workspace. Assignment requires both account-management and role-management permission, an explicit target role, a reason, and the inspected account version. Ordinary account administrators cannot promote themselves or others without role-management permission.
- Revoke all target sessions when a role actually changes, preserve account status and learning data, and protect the last active protected administrator against concurrent demotion and suspension. Reject self-role changes and stale or repeated commands without mutation.
- Make administrator login, navigation, route admission, and dashboard APIs permission-specific so a restricted role does not need catalog authority to use its authorized destination or gain operational metrics through catalog authority.
- **BREAKING**: Remove all runtime `CatalogAdministration:UserIds` authorization paths after an explicit, one-time operator-controlled import of existing administrator identities. Persist completion, validate the import, and never auto-promote users from configuration at application startup.
- Remove the production startup QA account/credential writer as a cutover prerequisite; startup must neither create a default administrative identity nor reset an existing account's password. Keep any QA provisioning explicit and isolated to tests, without deleting existing accounts.
- Keep role definitions and grants read-only through product APIs in this change. No custom-role editor, role deletion, multiple simultaneous roles, per-user permission overrides, ASP.NET Identity rewrite, credential-storage redesign, or unrestricted impersonation is included.
- Make `add-admin-managed-categories` depend on this change. Category administration will introduce its own `ManageCategories` permission and grant it explicitly to the protected Admin role, not restore an allowlist or implicitly grant it to every other role.

## Capabilities

### New Capabilities

- `role-authorization`: Database role membership and grants, least-privilege defaults, protected role assignment, one-time initialization, and authority resolution.

### Modified Capabilities

- `auth`: Replace deployment designation with database authority, expose effective capabilities, and invalidate sessions on role changes without changing sign-in credentials or ownership rules.
- `admin-accounts`: Display authoritative roles, perform protected assignment, and extend lockout/concurrency safeguards across role and status commands.
- `admin-workspace`: Permission-specific login landing, navigation, admission, and privilege-state disposal for restricted roles.
- `admin-dashboard`: Require a dedicated dashboard permission instead of catalog permission for operational aggregates and probes.
- `admin-audit`: Include role assignment and safe before/after role identifiers, with atomic mutation history and isolated rejection persistence.

## Impact

- Backend: .NET 10 Domain user/role invariants, Application permission and account contracts, PostgreSQL/EF Core schema and initialization command, all duplicated allowlist consumers, session issuance/revocation, Minimal API policies, and OpenAPI contracts.
- Frontend: Existing account details and confirmation UI, admin capability store, login/route helpers and navigation, role metadata in authenticated user responses, and English/Vietnamese feedback. No separate role-definition workspace is added.
- Operations: Inventory the legacy designation, back up the database, coordinate schema/bootstrap/backend/frontend cutover, verify at least one active protected administrator, and remove the legacy deployment variable after import. Bootstrap completion is operational evidence, not a fabricated user-attributed action.
- Verification during implementation: Real PostgreSQL authorization and concurrency scenarios, session/refresh races, exact API boundaries, the complete frontend behavioral suite, and authenticated English/Vietnamese desktop/mobile browser verification. This change creates planning artifacts only; role implementation precedes category implementation.
