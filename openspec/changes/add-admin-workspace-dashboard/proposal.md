# Proposal

## Why

DeepPace needs a distinct administrative entry point and operational overview instead of placing management pages inside the learner shell. Administrators should land on a dashboard showing real document, account and service information, then manage the catalog or switch to learning without changing accounts.

## What Changes

- Add `/admin/login` with a dedicated administrator sign-in surface using the existing account/session system.
- Add an isolated admin header/sidebar and `/admin` dashboard; mount the catalog management page from `add-curated-document-discovery` inside this shell.
- Preserve DeepPace's existing design system and layout archetypes: StudioLayout for login, BentoDashboardLayout for overview, MasterDetailLayout for catalog; reuse shared tokens and controls with responsive English/Vietnamese and light/dark parity.
- Permit protected `/admin` routes only after server-confirmed administrator authorization. Redirect unauthenticated or non-admin visitors to `/admin/login`; never render protected admin data before authorization.
- Display non-deleted account count, personal-document count, curated-document counts by publication state, processing/failure counts and bounded service status with timestamps. Missing telemetry is unavailable, never fabricated zero or healthy.
- Provide an explicit Switch to learning action targeting `/today` without logout; administrator logout returns to `/admin/login` and ends the shared session.
- Make authentication redirects aware of the current UI area and prevent login loops, unsafe return URLs and cross-account cached admin data.
- Keep account listing/editing/suspension, full RBAC, job administration, audit-log browsing, billing, infrastructure control and secret editing outside this proposal. Account count is included; account management is a separate feature.

## Capabilities

### New Capabilities

- `admin-workspace`: Separate administrator entry, navigation shell and transitions to the learner experience.
- `admin-dashboard`: Authorized aggregate metrics and service-status overview with explicit refresh and honest unavailable states.

### Modified Capabilities

- `auth`: Area-aware expiration redirects, guest exception for admin login and server-verified admin route admission.
- `system-layout-archetypes`: Limit existing learner navigation chrome to learner routes while retaining standardized page archetypes in the new admin shell.

## Impact

- Depends on `add-curated-document-discovery` for catalog storage, catalog management and its administrator policy/configured user IDs. This change supplies the surrounding workspace and dashboard; it does not duplicate catalog CRUD or alter that change's files during planning.
- Frontend: app shell selection in `frontend/app/app.vue`, global auth middleware, auth store/API interceptor, new admin shell/login/dashboard pages and localized strings.
- Backend: administrator-only overview and explicit service-probe APIs, aggregate query handlers and sanitized health results. Reuse the catalog administrator allowlist for this first single-admin workspace without adding a role editor.
- Integrate after the catalog authorization/data contracts exist. Its planned inline access-denied admin-page behavior is refined here to redirect to `/admin/login`; reconcile that pending auth requirement during implementation/spec integration as documented in design.md.
- Verify existing learner login/refresh/reader behavior, no privileged SSR rendering, aggregate correctness, API authorization and English/Vietnamese desktop/mobile layouts.
