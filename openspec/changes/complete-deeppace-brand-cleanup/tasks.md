# Tasks

Domain entities, database migrations and API DTOs require no changes for this cleanup. Preserve the existing JWT/refresh and ownership contracts. Reconcile overlapping auth/ingestion work with the current checkout when applying; this checklist does not apply another change.

For every task touching frontend pages, components or layouts, keep its checkbox open until both integration gates in 9.1 and 9.2 pass and desktop/mobile screenshots are presented. Unit tests assert data contracts and state, never CSS class lists as layout proof.

## 1. Frontend — Storage Registry and Migration Coordinator

- [x] 1.1 Add the explicit canonical/legacy key registry, validators and injected storage/cookie adapters described in the design; verify unit cases accept supported booleans, enums, typography, positive slice orders and consistent identity pairs while rejecting malformed values and leaving unrelated entries untouched.
- [x] 1.2 Implement versioned persistent/tab-scoped phase checkpoints, canonical-first copy/readback/source-removal and incomplete cleanup resumption; verify adapter tests for destination failure, failed cleanup, interrupted progress, canonical conflicts and a second startup that performs no completed legacy reads.
- [x] 1.3 Add migration serialization and identity-generation/tombstone hooks with safe deferral when coordination is unavailable; verify two-updated-tab contention, logout/account replacement during pending transfer and denied-storage cases without claiming unsafe atomic Web Storage writes.
- [x] 1.4 Integrate a client startup coordinator and an SSR-safe migration-candidate/admission signal before canonical consumers initialize; verify startup ordering and that blocked audio work does not block usable canonical state or ordinary login.
- [x] 1.5 Document the adapter contract, phase lifetimes, source-retirement rules and sanitized diagnostic codes alongside the migration module/current developer guidance; verify the documented cases match coordinator tests and contain no stored private values or new behavioral source of truth outside OpenSpec.

## 2. Frontend — Session Consumers and Route Admission

- [x] 2.1 Replace independent legacy reads/copy logic in `useAuthStore` with canonical session resolution after the shared boundary; verify store regressions cover canonical-only, legacy-only, conflicting identities, missing/mismatched profiles and invalid/expired credentials without changing server authority.
- [x] 2.2 Make authenticated bearer-token lookup in `useApiClient` use the resolved canonical identity only; verify request/refresh tests never attach a legacy or mixed-account bearer token and preserve existing single-flight refresh/error handling and public authentication requests.
- [x] 2.3 Integrate neutral learner admission around protected shell/page rendering in `app.vue` and canonical route middleware, retaining admin permission verification and safe return paths; verify SSR/hydration tests expose no protected markup or requests for unresolved legacy-cookie identity and preserve guest/denial/transient-error behavior.
- [x] 2.4 Preserve known remember-me scopes and use conservative session-scoped access state for ambiguous cookie-only import; verify cookie/local/session assertions across migration and refresh, with no persistent promotion of session-only credentials, HTTP-only refresh-cookie access or refresh DTO changes.
- [x] 2.5 Connect sign-in, logout, terminal cleanup and account replacement to migration retirement/fencing and canonical cleanup; verify delayed work cannot resurrect a cleared identity or stale admin access, including failed storage cleanup and cross-tab invalidation.

## 3. Frontend — Reader, Library and Device Preferences

- [x] 3.1 Remove feature-level legacy fallback from shared reader typography and retain canonical hydration/persistence for Reader and Today; verify existing typography tests plus migrated/default/invalid-value cases without changing typography presets or layout behavior.
- [x] 3.2 Switch Roadmap, Knowledge Graph and GraphLegend consumers to canonical-only preference reads; verify saved `timeline`/`mindmap`, `2d`/`3d` and explicit collapsed `false` choices survive migration and later canonical updates.
- [x] 3.3 Use canonical-only bookmark/completed-slice reads in the reader and Library; verify migrated positive slice orders and completion collections retain book identity, canonical values win, and the transfer itself invokes no learning-progress, adoption or ownership mutations.
- [x] 3.4 Replace the active Settings `techdaily-auto-advance-cards` writer with `deeppace_auto_advance_cards` and the shared mapping; verify a saved `false` survives migration, toggles write only the canonical key, and unrelated Settings payloads remain unchanged.

## 4. Frontend — IndexedDB Narration Cache

- [x] 4.1 Switch `createIdbBackend()` to `deeppace-audio` while retaining `slice-audio`, complete/partial key formats and current playback contracts; verify cache/useSliceAudio regressions and that ordinary playback never opens the legacy database.
- [x] 4.2 Implement bounded recognized-record transfer with canonical conflict checks, verified writes and compatible LRU/partial validation; verify complete and partial cache reuse, malformed metadata recovery, interruption resumption and the existing byte/entry limits without synthesis calls.
- [x] 4.3 Handle absent-database probes, open connections, `blocked`/`versionchange`, transaction aborts, quota and unknown stores; verify adapter tests retain recoverable source data, avoid accidental legacy database creation/deletion and report incomplete cleanup accurately.
- [x] 4.4 Document cache phase recovery and compatible rollback constraints next to the cache/migration adapters; verify the guidance matches the implemented checkpoint/connection lifecycle and preserves canonical cache hits during deferred cleanup.

## 5. Application — Notification Payload Defaults

- [x] 5.1 Change the default `PushNotificationPayload` tag to `deeppace-daily`; verify payload contract tests preserve explicitly supplied tags, URLs and icons while an omitted tag uses the canonical default.

## 6. Infrastructure — Notification Configuration and Test Fixtures

- [x] 6.1 Remove the TechDaily serialized-tag/contact fallbacks in `WebPushService`, using the canonical tag and supported configured VAPID subject/default; verify configuration/serialization tests with a fake sender, unchanged explicit values and no live notification or key/subscription mutation.
- [x] 6.2 Normalize current backend fixture emails, in-memory test labels and branding comments where they do not identify retained resources; verify affected tests and the backend suite pass, and source review leaves real database/volume/host/restore identifiers and historical migrations intact.

## 7. Frontend — Product Copy and Crawler Metadata

- [x] 7.1 Replace the login's retired-brand example address, remove unconsumed legacy locale identifiers and normalize current component comments/copy; verify locale lookup/consumer searches and relevant form tests preserve behavior in English and Vietnamese, with final visual evidence required by 9.2.
- [x] 7.2 Remove the TechDaily JSON-LD alias and align `robots.txt` comments/sitemap with canonical DeepPace metadata; update `seo-metadata.spec.ts` and verify structured-data extraction, sitemap URL and crawler policy without changing compatibility hosts.
- [x] 7.3 Confirm existing media-session/service-worker product titles, icons and explicit notification tags remain canonical; verify relevant audio/config tests and reviewed runtime values without changing playback, dispatch timing or push subscriptions.

## 8. Frontend — QA Harnesses and Current Guidance

- [x] 8.1 Update ordinary frontend auth fixtures and browser QA scripts, including graph/roadmap, Library, admin workspace and dual-gate runners, to canonical session keys; verify targeted suites and authenticated harness startup work without relying on legacy fallback, while dedicated migration fixtures retain their old source samples.
- [x] 8.2 Update `AGENTS.md`, README/current developer guidance and applicable active documentation to DeepPace product/storage terminology and the migration/operational exception policy; verify QA examples use authentic canonical sessions and links retain the actual GitHub repository and checkout/VPS resource paths.
- [x] 8.3 Reconcile this change's naming deltas with the current auth/ingestion specs before eventual sync, and audit remaining matches with `rg -n -i techdaily`; verify each match belongs to explicit migration, actual operational identity or historical/source evidence, the documented upload name matches current ingestion, and unrelated/archived artifacts are not rewritten.

## 9. Cross-Component Integration Gates

- [x] 9.1 Run the full frontend `npm test` suite and backend `dotnet test` suite for the completed checkout, then `openspec validate complete-deeppace-brand-cleanup --strict`; verify all required behavioral/contract suites and strict spec validation pass before UI sign-off.
- [x] 9.2 Drive headless Chromium with an authentic authorized account using canonical session injection, capture and directly inspect desktop 1440x900 or 1920x1080 and mobile 390x844 screenshots for changed login/admission and learner/admin surfaces across English/Vietnamese and light/dark states; verify no protected-content flash, hydration error, clipped text, overlap or unexpected scrolling and present desktop/mobile screenshots to the user before closing UI tasks.
- [x] 9.3 Exercise real-browser legacy-only, canonical-only, conflicting session identities, session-only mode, multi-tab/logout races, interrupted/blocked IndexedDB transfer and a repeat visit; verify canonical storage/cache reuse, accurate phase outcomes, absence of new legacy writes, no migration-triggered remote generation and unchanged backend ownership/admin permission enforcement, retaining sanitized evidence outside the tracked repository.
