# Administrator workspace

The administrator workspace shares DeepPace accounts, authentication and the browser refresh-token session with learning. `/admin/login` admits only accounts confirmed by the existing `CatalogAdmin` policy. The operator assigns existing active user IDs through `CatalogAdministration__UserIds__0` and subsequent indexed environment entries. An empty designation grants no administrative access. Configuration changes take effect after reload or restart. No additional credentials, role editor or secret-editing UI is introduced.

## Inventory metrics

`GET /api/v1/admin/dashboard/overview` returns only aggregates from a PostgreSQL repeatable-read transaction:

- `accountCount`: non-deleted users.
- `personalDocuments`: non-deleted owned `DocumentBook` rows, including independent adopted copies. Legacy unowned books are excluded. Counts reflect book state, without fetching titles, source text or private learning content.
- `catalogDocuments`: non-deleted catalog titles, publication states and processing states. Adopting a title does not increase this catalog total.
- `observedAt`: UTC observation time, displayed in the selected interface locale.

Pending/Processing/Ready/Failed counts describe persisted document states. They are not worker queue depth. Unavailable reads return HTTP 503 rather than invented zero counts; the UI keeps the last successful observation visibly stale where available.

## Service observations

`GET /api/v1/admin/dashboard/status` performs a database connectivity check bounded to five seconds. A successful HTTP response measures API responsiveness for that request only; it does not assert universal system health. The initial dashboard load and manual refresh make no external AI request.

`POST /api/v1/admin/dashboard/ai-probe` explicitly checks text generation and embeddings. The shared application probe has a 15-second overall deadline and propagates request cancellation. Requests are limited to one per administrator per minute. Provider failures return HTTP 503 with sanitized per-service observations; duplicate/rate-limited requests return HTTP 429. Missing `Gemini__Model` or `Gemini__ApiKey` fails immediately without contacting providers. A measured result older than five minutes is stale. Probe results are account-scoped, remain in memory and reset on reload.

All dashboard routes independently require administrator authorization and send `Cache-Control: no-store`. Anonymous callers receive 401; authenticated non-administrators receive 403 before inventory/probe services run. The legacy `/api/v1/system/ai-health` response shape is preserved and uses the same probe implementation.

## Navigation and session lifetime

Dashboard uses `BentoDashboardLayout`, login `StudioLayout`, and catalog `MasterDetailLayout`, with shared DeepPace styling and English/Vietnamese, dark/light parity. Switch to learning opens `/today` without replacing the account or issuing tokens. Logout ends the shared browser refresh-token family and returns to `/admin/login`; other devices retain their independent sessions. Permission denial clears administrative state while preserving a valid learner session. Account replacement, logout and revoked permission abort privileged requests and discard late results.

The workspace includes inventory overview and existing catalog management. The account total is read-only. Account management, role assignment, infrastructure operations and secrets remain outside this release.

## Verification

The implementation was checked with the full backend suite (493 passing tests), the full frontend suite (774 passing tests across 88 files), and a production Nuxt build. Regression coverage includes independent aggregate scopes, authorization, bounded and sanitized probes, area-aware session expiry, account replacement, permission revocation, single-flight refresh, hydration boundaries and shell routing.

Commands from the repository root:

```sh
dotnet test backend/tests/DeepPace.Tests/DeepPace.Tests.csproj --no-restore --verbosity minimal
cd frontend
npm test
NUXT_PUBLIC_API_BASE_URL=http://127.0.0.1:5001 API_INTERNAL_URL=http://127.0.0.1:5001 npm run build
```

`frontend/e2e/test-admin-workspace.mjs` uses real API login responses and refresh cookies, never fabricated browser identities. Run it against an isolated, migrated QA database containing active administrator and ordinary accounts plus a published catalog title named `Distributed Systems Field Notes`. Configure `CatalogAdministration__UserIds__0` for that administrator. Supply `ADMIN_QA_EMAIL`, `ADMIN_QA_PASSWORD`, `LEARNER_QA_EMAIL` and `LEARNER_QA_PASSWORD` through the environment; the administrator pair can also come from the gitignored `.env` E2E entries. An optional `ADMIN_QA_ACCOUNTS_FILE` is a private QA fixture file with `ordinaryEmail` and `ordinaryPassword` fields. Never commit credentials or browser storage states.

```sh
TARGET_URL=http://127.0.0.1:3001 API_URL=http://127.0.0.1:5001 node e2e/test-admin-workspace.mjs
```

The browser suite passed 149 assertions and captured 39 screenshots. It covers real sign-in, regular-account denial, account replacement, remember-me cookies, SSR admission, refresh-only hydration, unknown administrator paths, direct API 401/403 rejection, catalog navigation, learning switch and shared logout. It simulates network/permission failures with Playwright response interception while retaining authentic sessions. The explicit health-probe button contacts the configured AI services once; loading the dashboard does not. A controlled browser clock verifies stale observations. Screenshots cover 1440×900 and 390×844 in English/Vietnamese and light/dark, including drawer keyboard focus, retries, denial and service states. Images and the assertion report are written to gitignored `frontend/e2e/screenshots/admin-workspace/`, inspected directly and presented with the implementation report.

Before archive, `openspec validate add-admin-workspace-dashboard --strict` passed. The four capability deltas were merged into the main specifications and all 27 main specs passed `openspec validate --specs`. The complete planning artifacts and 19 finished tasks are archived at `openspec/changes/archive/2026-10-15-add-admin-workspace-dashboard/`. The catalog-auth prerequisite is represented in the main specification, with its previous inline denial replaced by administrator-login redirection.
