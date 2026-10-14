# Design

## Context

See proposal.md for motivation. `frontend/app/app.vue` currently selects only authentication or learner/reader shells. `auth.global.ts` exempts existing learner auth routes and sends authenticated visitors away from login. `useAuthStore.logout(redirectPath)` already supports a destination; `useApiClient` performs refresh and deduplicates terminal 401 navigation. `SystemEndpoints` currently exposes an AI health GET that actually invokes text generation and embeddings, so it cannot serve as an automatic dashboard polling source. Users and documents inherit soft deletion and CreatedAt; there is no observed active-account or resource-usage telemetry suitable for inventing activity/CPU/cost cards.

The catalog change is being implemented separately (its task count is no longer zero). It owns catalog persistence, preparation, CRUD, policy and `CatalogAdministration:UserIds`. Do not race or duplicate that work. This workspace is an integration dependent on those contracts.

## Goals / Non-Goals

**Goals:** Isolate navigation and entry points while sharing authentication and design primitives; provide accurate aggregates with bounded read-only system observations; prevent SSR and stale client data from leaking administrative information.

**Non-Goals:** Another application/deployment, another password database, independent sessions, account management, full role administration, live infrastructure monitoring, arbitrary SQL, secret editing or a catalog rewrite.

## Decisions

### 1. One application, three explicit shell modes

Classify normalized route paths using segment-aware checks: `/admin`, `/admin/*`, and the exact `/admin/login` exception. `/administrator` is not an admin path. The app selects admin-auth, authorized admin-shell, or existing learner/auth/reader behavior. Admin-auth renders StudioLayout without either navigation shell. Admin shell has its own header and mobile/desktop sidebar with Dashboard and Documents links, locale/theme controls, logout and Switch to learning. It does not mount learner AppHeader, AppSidebar or AppCommandPalette. Unknown admin routes still pass the authorization gate before showing a safe not-found page.

Use BentoDashboardLayout for `/admin`; mount the catalog change's MasterDetailLayout page inside this shell. Reuse actual DeepPace Tailwind semantic tokens (`brand`, `canvas`, shared glass surfaces), Lucide icons, typography, AppModal, AppSelect, buttons, pagination and toast components. Do not add UnoCSS, a third-party dashboard theme or isolated CSS token palette. Consult the current showcase and layout components before implementation; extend shared components only compatibly. Body text is text-sm on mobile and text-base or larger from tablet, actions/badges/footer controls use no-wrap, shrink prevention and responsive gaps. Both dark/light and en/vi are supported.

Alternative: separate frontend hosting adds deployment and session complexity without a requirement for independent releases. Distinct UI within the current application satisfies the agreed scope.

### 2. Shared authentication with server-confirmed admission

Reuse existing email/password login and remember-me behavior. After successful authentication, obtain the existing catalog capability from the server before admitting the account. This release treats the configured catalog administrators as the workspace administrator cohort; it grants only dashboard read and existing catalog rights, not future account-management privileges. No new default account, copied credentials, public grant endpoint or trusted client role flag is introduced. The current .env user-ID entry is compatible.

Admin login remains reachable with an existing regular-user session and displays a localized permission explanation. Valid ordinary credentials can establish an ordinary shared session but never admit the account into admin UI; the login page permits replacement credentials and provides the learner destination. An already authorized admin visiting login goes to the validated return path or dashboard. Validate return URLs as same-origin normalized paths matching a protected admin route; reject `//`, alternate origins, backslash/encoding tricks, login destinations and learner routes, falling back to `/admin`.

Before protected admin SSR, verify permission server-side by forwarding only request-scoped credentials to the trusted backend. If renewal cannot safely complete during SSR, render only a neutral authorization-pending boundary and perform the existing single-flight refresh/client permission check; never serialize metrics or render admin chrome pending approval. Definitive 401/403 goes to `/admin/login`; network/5xx shows retry without wiping a valid session. Login failures stay in the form. Backend overview/probe endpoints require the same policy independently.

### 3. Navigation, logout and data lifetime

Switch to learning navigates to `/today` with the same account. A normal learner login by an admin retains learner navigation; admin access is available through the admin URL, not an automatic forced redirect of all sessions. Explicit admin logout uses existing revoke/session-clear behavior with `/admin/login` destination and clears all admin stores. Shared logout affects both interfaces, not unrelated browser devices.

Select terminal session-expiry destination from the active route area and retain single-flight redirect behavior. On permission denial, clear admin state and redirect without necessarily revoking a still-valid learner session. Abort in-flight privileged requests and increment a session-generation token on logout/account change; discard results whose account/generation no longer matches. Check permission on protected admin navigation and let every protected API recheck permission. Do not cache capability across accounts or serialize it into a global SSR singleton.

### 4. Dashboard data and measurement definitions

Add dedicated DI query handlers and protected APIs under `/api/v1/admin/dashboard`:

| Endpoint | Response and behavior |
|---|---|
| GET `/overview` | Accounts total, personal-document total and Pending/Processing/Failed counts, catalog total plus Draft/Published/Hidden and processing breakdowns, observedAt |
| GET `/status` | Bounded database connectivity observation, service response timestamp, no external AI request |
| POST `/ai-probe` | Explicit text and embedding probe; sanitized per-service outcome, latencyMs, checkedAt; rate-limited |

All routes return 401/403 before querying or probing for unauthorized callers, and use no-store responses. Count non-deleted Users; count non-deleted owned DocumentBooks, including adopted copies, as personal documents. Exclude legacy unowned books from this labeled metric. Catalog counts use separate non-deleted catalog rows. Processing totals use persisted document state and are not queue depth. Use database aggregate queries without loading chunk text or per-account content. Use a consistent read snapshot for overview counts so breakdowns match totals; do not share an EF context across parallel operations.

Load overview and inexpensive status independently on initial admission and manual Refresh. No automatic recurring polling is necessary in this release. Show timestamp, loading, success, stale and unavailable states; preserve successful data with a stale label during errors. API responsiveness refers only to the actual successful status request, not a claim that every subsystem is healthy. If the API cannot be reached, show unavailable; database failures should produce a sanitized status result where possible without turning missing counts into zero.

AI probe is an explicit action, bounded by a 15-second overall timeout and cancellation, deduplicated while running and rate-limited to one per administrator per minute. Extract/reuse probe internals from SystemEndpoints behind an application interface; never call its anonymous GET automatically from the dashboard. Preserve the existing endpoint's contract in this scope. Configuration absence fails immediately. Map provider/network details to safe error codes and messages, never return API keys, raw response bodies or connection strings. Keep the latest probe result only in account-scoped dashboard state for this release; on reload show Not checked until a new explicit probe. A result older than five minutes is stale. No persisted telemetry store is required.

### 5. Relationship to the catalog change and specs

Apply/integrate after catalog entities, policy, capabilities and admin page exist. Catalog owns document workflows; this change replaces surrounding chrome and supplies dashboard navigation. Existing main auth expiration/default-deny requirements are modified with full preserved learner scenarios in this delta. The catalog change also proposes `Administrator capability discovery and route protection`, whose non-admin behavior is an inline access-denied state. The user's newer decision replaces that UI behavior with redirect to `/admin/login`, including revocation. Backend 403 behavior remains unchanged.

During integration, once the prerequisite auth requirement exists in main specs, add a full MODIFIED block for that requirement to this change with the new redirect behavior before syncing/archiving this change. If the prerequisite remains unarchived, coordinate its spec integration first. Do not silently archive both competing descriptions. No prerequisite files are changed during this planning request.

### 6. Verification strategy

Extend existing `frontend/tests/middleware/auth.spec.ts`, `frontend/tests/stores/auth.spec.ts` and API-client tests for admin login, refresh, SSR neutral state, denial redirects, safe return paths, concurrent errors and stale responses. Add meaningful shell visibility/navigation and dashboard state tests rather than CSS assertions. Backend tests cover exact aggregate definitions, deleted records, independent scopes, empty versus unavailable, no AI calls on GET, sanitized probes, rate limiting and 401/403.

Automated Chromium verification uses authentic admin and ordinary accounts, current session cookies and legacy migration only where applicable. Inspect and present 1440x900 and 390x844 screenshots of admin login, dashboard, integrated catalog and switching to learning in English/Vietnamese. Check both themes, mobile menu/focus, loading/errors and that no learner chrome leaks into admin. All frontend test suites must pass as well; UI tasks remain incomplete until both gates pass.

## Risks / Trade-offs

- Existing global guest redirect can loop -> admin login is a specific exception evaluated before learner guest redirect logic.
- SSR permission/refresh complexity -> deny protected rendering until verified; request-scoped state and neutral fallback avoid hydration leaks.
- Existing catalog work changes concurrently -> integrate its contracts first; preserve edits and reconcile the named spec conflict explicitly.
- Aggregates can be mistaken for live telemetry -> labels, scope definitions, timestamps and unavailable/stale states avoid invented measurements.
- Shared session surprises -> document that switching UI preserves identity and logout ends the shared browser session.

## Migration Plan

1. Complete prerequisite catalog authorization/data integration; retain existing .env admin designation and add no secrets.
2. Deploy protected dashboard read/probe APIs and verify authorization before exposing UI routes. No new database tables are required for dashboard metrics.
3. Deploy admin shell, login, routing and dashboard together, then mount catalog inside the shell; verify learner routes/refresh remain unchanged.
4. Reconcile the pending catalog route-protection requirement before spec sync/archive. If rolling back, remove the workspace UI and overview APIs while preserving catalog functionality, accounts and source data.
