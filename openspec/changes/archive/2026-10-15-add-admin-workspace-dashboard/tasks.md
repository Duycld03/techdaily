# Tasks

## 1. Integration Contracts

- [x] 1.1 Confirm the prerequisite catalog policy, capability response, entity states and admin catalog page are implemented; verify compatibility with the documented route and metric contracts without duplicating catalog work.
- [x] 1.2 After prerequisite auth specs are synced, add the full MODIFIED block for Administrator capability discovery and route protection to this change, replacing inline denial with `/admin/login` redirection; verify strict validation and no contradictory route-denial scenarios before this change is synced.

## 2. Application and Infrastructure

- [x] 2.1 Add dashboard DTOs and direct DI aggregate handlers using consistent non-deleted account, owned-document and catalog scopes; verify seeded tests for adoption copies, legacy unowned books, deleted records, zero counts and publication/processing breakdowns.
- [x] 2.2 Add bounded database status and sanitized AI-probe service interfaces/implementations using existing provider integrations, fail-loud configuration and cancellation; verify missing configuration, provider failure, timeout and secret-free errors with service tests and no AI calls on overview/status reads.
- [x] 2.3 Implement overview read consistency without fetching document bodies or private account data; verify relational integration tests and document metric definitions, observation timestamps and processing-count limitations.

## 3. API

- [x] 3.1 Map `/api/v1/admin/dashboard/overview`, `/status` and `/ai-probe` behind the existing administrator policy with no-store responses; verify 401/403 prevent database/probe execution and successful responses contain only declared fields.
- [x] 3.2 Add explicit AI probe rate limiting, 15-second timeout and sanitized per-service statuses; verify duplicate/rate-limit, failure and cancellation tests without weakening the existing system health contract.
- [x] 3.3 Document dashboard APIs, administrator cohort reuse and metric scopes in OpenAPI/operator documentation; verify Scalar contracts and document that no new credentials, roles or secret-editing UI are introduced.

## 4. Frontend Authentication and Shell

Before frontend edits, read the canonical Vue, Vue best practices, Nuxt, Pinia, VueUse, Vitest, Vue testing and Antfu Design skills, and inspect current DeepPace tokens, shared components and layout showcase. Preserve the Tailwind stack. All UI tasks remain unchecked until both full npm tests and inspected/presented desktop/mobile screenshots pass.

- [x] 4.1 Add explicit admin-auth/admin-shell route selection in app.vue and dedicated header/sidebar with Dashboard, Documents, locale/theme, logout and Switch to learning; verify component behavior tests, unchanged learner/reader shell selection and screenshots without duplicate chrome.
- [x] 4.2 Build `/admin/login` with StudioLayout and existing login/session primitives; verify invalid credentials, regular-account denial, account replacement, remember-me and safe admin return-path tests plus bilingual responsive screenshots.
- [x] 4.3 Implement server-confirmed admin route admission, SSR neutral fallback and the exact admin-login guest exception; verify anonymous/non-admin deep links, trailing slash handling, unknown admin paths, transient errors and loop-free redirects in middleware tests and real browser navigation.
- [x] 4.4 Make terminal 401 redirects area-aware, handle admin 403 by clearing privileged state, and discard canceled/old-account results; verify concurrent failures, refresh recovery, logout, account changes and existing learner auth tests.
- [x] 4.5 Mount the prerequisite `/admin/catalog` page inside the isolated admin shell while retaining MasterDetailLayout and its actions; verify catalog behavior tests and desktop/mobile screenshots, and verify Switch to learning opens `/today` without changing session identity.

## 5. Frontend Dashboard

- [x] 5.1 Add account-scoped dashboard state for overview/status/probe requests, independent errors, manual refresh, timestamps and stale results; verify loading, exact data mapping, unavailable versus zero, duplicate requests and no automatic AI probe calls in store tests.
- [x] 5.2 Build `/admin` with BentoDashboardLayout and existing DeepPace metric/surface components, read-only account total, distinct personal/catalog totals, processing breakdowns and service status; verify data contracts and en/vi light/dark desktop/mobile screenshots without invented telemetry or dead navigation links.
- [x] 5.3 Complete translations, accessible labels, focus handling, mobile drawer dismissal, typography and no-wrap actions across admin surfaces; verify keyboard interactions and visually inspect 1440x900 and 390x844 screenshots in both locales and themes, presenting images with the implementation report rather than asserting CSS classes.

## 6. Integration Verification

- [x] 6.1 Run full `dotnet test` and frontend `npm test`, then use authentic admin and ordinary accounts to verify sign-in, dashboard, catalog, learning switch, denial, refresh and logout end to end; record commands and results.
- [x] 6.2 Inspect SSR HTML/network behavior for denied and permission-pending admin requests and verify no privileged markup/data is exposed; verify direct APIs reject non-admin callers independently of UI routes.
- [x] 6.3 Run strict OpenSpec validation, reconcile prerequisite spec integration and confirm all changed UI has both behavioral and screenshot evidence before declaring implementation complete.
