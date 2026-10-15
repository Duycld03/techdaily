# Design

## Context

See `proposal.md` for the motivation and capability scope. Current runtime writes are mostly canonical, but `useAuthStore`, `useApiClient`, route middleware and several feature consumers still read TechDaily values independently. This can combine a token from one source with a profile from another. Settings actively writes `techdaily-auto-advance-cards`, and `sliceAudioCache.ts` opens the `techdaily-audio` IndexedDB database.

`app.vue` renders learner chrome outside an authorization boundary. Returning early from middleware for a legacy cookie would therefore still expose the learner shell. Administrator routes already have permission verification and a neutral boundary; those controls must remain authoritative.

The server owns JWT validation and the HTTP-only `refreshToken` cookie. Refresh responses expose a token and user, without a persistence-mode flag. JavaScript cannot infer a cookie's original expiry from its value. Browser migration is consequently a same-origin compatibility operation, not authentication or cross-domain account transfer.

The main specs contain both canonical and stale naming. Existing requirement blocks are included in full in these deltas to preserve their scenarios. Naming normalization does not authorize restoring superseded login decorations, simulated operational metrics, compliance claims, authentication workflows or layouts.

The retained core-platform architecture block also contains pre-existing `docs/` retirement language, while supported operational documentation still exists in the checkout. This change normalizes current documentation references without executing a broad documentation deletion. The showcase scenario retains its original title for OpenSpec continuity and the actual legacy-named repository, while its live product URL becomes canonical.

## Goals / Non-Goals

**Goals:**

- Make canonical reads the normal application contract and isolate legacy access behind one tested migration boundary.
- Preserve valid user state while making interruption, denied storage, concurrent startup and logout safe.
- Keep startup admission predictable on SSR and hydration without waiting for an entire audio database transfer.
- Produce an explicit remaining-reference policy so tests and documentation do not reintroduce old runtime identifiers.

**Non-Goals:**

- Change token issuers accepted by the backend, refresh payloads, credentials, account ownership or administrator assignment.
- Redesign existing pages, implement another pending change, migrate browser state between domains, or rewrite deployment resource identities/history.
- Turn migration into a permanent feature-level fallback or generate replacement audio through AI or speech services.

## Decisions

### 1. Register known identifiers rather than replacing arbitrary prefixes

Add a small framework-independent storage registry and migration utility under the canonical frontend application tree, with injected cookie, storage and IndexedDB adapters. A Nuxt startup integration owns lifecycle/admission state. The modules' final filenames can follow nearby conventions; they are proposed additions, not existing services.

The registry contains explicit mappings, validators and storage scopes:

| Existing identifier | Canonical destination | Scope / validation |
| --- | --- | --- |
| `techdaily_token`, `techdaily_user` | `deeppace_token`, `deeppace_user` | Access-cookie/local/session identity pair; expiry, subject and profile consistency |
| Obsolete browser `techdaily_refresh_token` / `deeppace_refresh_token` copies | Remove known obsolete copies only | Never import or expose the HTTP-only `refreshToken` cookie |
| `techdaily_reader_typography` | `deeppace_reader_typography` | Existing typography object fields/enums |
| `techdaily_roadmap_view_mode` | `deeppace_roadmap_view_mode` | `timeline` or `mindmap` |
| `techdaily_graph_view_mode` | `deeppace_graph_view_mode` | `2d` or `3d` |
| `techdaily_graph_legend_collapsed` | `deeppace_graph_legend_collapsed` | Explicit serialized boolean, including `false` |
| `techdaily_bookmark_{bookId}` | `deeppace_bookmark_{bookId}` | Supported book identifier and positive integer slice order |
| `techdaily_completed_{bookId}` | `deeppace_completed_{bookId}` | Supported per-book JSON collection of completed slice orders |
| `techdaily-auto-advance-cards` | `deeppace_auto_advance_cards` | Settings boolean; update the active writer |
| `techdaily_reader_audio_auto_advance` | `deeppace_reader_audio_auto_advance` | Documented legacy boolean preference |
| `techdaily-audio` / `slice-audio` | `deeppace-audio` / `slice-audio` | Existing narration cache schema and recognized metadata |

Already canonical narration speed/engine/voice/system-voice keys remain unchanged. Preserve their existing suffixes and semantics. Do not invent additional legacy audio keys from a string-prefix guess. Third-party locale/theme keys and standard service-worker/browser cache names are unchanged unless the inventory demonstrates an application-owned legacy identifier.

Alternative considered: a repository-wide string replacement. It cannot distinguish live deployment names, original excerpts, credential storage scopes or incompatible stored values, and is unsuitable for data migration.

### 2. Use versioned, independently resumable phases

Proposed progress identifiers are `deeppace_storage_migration_v1` in persistent storage and a session-scoped companion with the same namespace in session storage. The marker holds bounded version/phase/status data rather than copied user content. Track persistent identity/cookie cleanup, tab-local session identity, device preferences/book records, and IndexedDB transfer/cleanup separately. An origin-wide completion marker must not suppress a still-unmigrated tab's session storage.

For each eligible record: read and validate the known source; recheck canonical presence; write only if absent; read back/verify the destination; remove only the handled source; then checkpoint progress. Invalid recognized values are removed under the stated validation policy. Existing canonical data always wins, including a canonical value that its normal consumer later rejects. A partial or corrupt canonical session must use existing recovery, not a legacy replacement.

Source removal may fail after a successful copy. Retain incomplete cleanup state and resume cleanup without copying over the destination. Marker-write failure cannot be treated as completed work. When storage is denied, leave valid sources intact, stop the affected phase and keep canonical application/login behavior available. Completed phases check their marker, not old values, on later visits.

Auth/preferences transfer finishes or safely defers before their consumers initialize. Audio transfer is bounded asynchronous work and does not hold the entire application behind a loading screen. Incomplete phases resume at a later safe startup or canonical cache access without adding legacy reads to feature code.

Alternative considered: a single `migrated=true` flag after startup. It loses blocked or tab-specific work and makes recovery indistinguishable from completion.

### 3. Resolve a complete canonical session before authentication consumers

The startup boundary resolves session candidates before `authStore.init()`, bearer-token lookup, route refresh and administrator checks. The store and API client then share the same canonical resolution policy rather than choosing token and profile sources independently.

Canonical credentials in any supported scope exclude legacy credentials from identity selection. Retain the established canonical cookie/local/session priority, but require the selected profile to match its token; otherwise recover the profile from the same token or authenticated server response. Never pair a canonical token with a legacy profile. Decoding claims only checks structure/expiry/consistency; API authorization still requires the server's normal cryptographic validation and administrator permission checks.

A known local-storage legacy pair retains persistent behavior. A known session-storage pair uses session storage and session cookies only. If legacy sources disagree about identity or persistence without enough reliable evidence, defer that identity import and use refresh/login recovery. Cookie-only legacy state uses session-scoped access storage because JavaScript cannot prove its previous persistence mode. Subsequent refresh preserves this conservative client mode; the server continues to control its existing refresh-cookie lifetime. Do not assume a new persistence field exists or extend the refresh DTO for this cleanup.

For SSR, the shared boundary may inspect the presence of known legacy cookies only as a migration signal. It must not use their values as authorization. When browser resolution is needed, serialize a neutral admission state and render the same state during hydration. Add a learner admission boundary around chrome/page rendering in `app.vue`; returning from middleware alone is insufficient. Start protected page setup/fetching only after admission resolves. Keep the existing admin boundary and server-confirmed permissions in the sequence, retain safe local return paths, and invalidate stale permission state when identity changes.

Canonical SSR flows continue through their current checks. Guests with no valid session, refresh possibility or migration candidate follow existing redirects. A neutral migration or transient permission state must not render private content, invent an account or become a development bypass.

Alternatives considered: continue reading old cookies in each guard/API helper, or redirect every legacy browser immediately. The first retains identity races and unsafe SSR selection; the second discards valid same-origin compatibility data unnecessarily.

### 4. Fence identity writes and use real cross-tab coordination

Use an application-owned Web Lock for migration where available and coordinate identity commits with canonical login/logout/account replacement. Recheck canonical destinations and the identity generation immediately before commits; the current in-memory session/admin generations remain part of this fencing. Persist a bounded identity-retired/tombstone state when logout or terminal cleanup occurs, and notify updated tabs through existing storage events or an equivalent browser channel. No private values belong in that signal.

Logout invalidates pending work immediately, clears known credentials through the shared adapter and prevents old import on subsequent visits. Normal sign-in writes canonical state and also retires legacy identity import. Preference/cache phases remain independent from the identity tombstone.

Web Storage has no compare-and-swap transaction. Do not present a local-storage lock flag as equivalent to a Web Lock. If safe serialization cannot be established, defer contested legacy transfer and keep canonical/refresh/login paths usable. Updated tabs honor the same coordination contract. A pre-upgrade tab that keeps writing old values is unsupported after completion; reload it rather than making old feature fallbacks permanent.

Alternative considered: check destination absence once and copy later. An intervening sign-in or logout could then be overwritten by delayed work.

### 5. Keep feature migration free of learning side effects

Remove old reads from `useReaderTypography`, `useRoadmapViewMode`, `useKnowledgeGraphStore`, `GraphLegend`, the reader and Library bookmark enumeration once startup integration is ready. All ordinary writes/reads use registered canonical identifiers. The Settings writer changes in the same release as its migration mapping.

Validate values using existing consumer schemas. Preserve explicit `false`, supported defaults, valid book identifiers and positive integer slice orders rather than using truthiness as a migration test. Bookmarks are slice orders, not chunk GUIDs; the reader still bounds an order against the fetched book's chunk count. Transferring a local bookmark/completion set must not invoke reader navigation handlers, chunk completion APIs, pacer synchronization, ownership changes or document adoption. Backend ownership remains mandatory when a book is later opened.

Alternative considered: trigger existing page callbacks to recreate state. Those callbacks can advance learning progress or send server mutations and are unsuitable for storage transfer.

### 6. Transfer narration cache through a bounded adapter

The only observed production store is `slice-audio`, opened through `createIdbBackend()` from `useSliceAudio`. Keep cache keys `${chunkId}::${voice}::v2::${contentHash}`, complete `Blob` values, `partial::` records and valid `__slice_audio_lru__` semantics compatible. Migration validates metadata, supported partial structures, sample rates and sizes; it does not change synthesis, content identity or playback settings.

Enumerate only recognized legacy records with a cursor and process bounded batches, yielding between batches. Verify each canonical record transaction/readback before deleting its source. Recheck canonical presence to avoid replacing newly cached playback. Rebuild/merge valid LRU metadata and apply the current 300 MiB / 60-entry policy; partial transfers also consume a bounded transfer budget. Discard invalid, obsolete or evictable cache data under the cache policy, without automatically synthesizing replacement content.

Do not accidentally create an empty legacy database just to discover it: use available database enumeration, or abort creation during an absence probe. Close owned connections. Delete the legacy database only after recognized work is handled and its schema contains no unhandled stores. Treat `blocked`/`versionchange`, transaction aborts, quota and interruption as observable incomplete phases. An old open connection or unknown store means cleanup remains pending rather than claiming deletion or clearing unrelated data.

Alternative considered: rename the database and drop the old cache. That forces avoidable replay work and does not meet the agreed data-preservation contract. Reading the whole database into memory would also defeat the transfer bounds.

### 7. Normalize current product/configuration surfaces with scoped exceptions

Remove the retired JSON-LD `alternateName` and point `robots.txt` at the already canonical DeepPace sitemap. Replace the login placeholder with a neutral example address, remove unused `sign_in_to_techdaily` translations after checking consumers, and normalize current comments/copy without changing layouts.

Change `PushNotificationPayload` and serialized fallback tags to `deeppace-daily`. Resolve VAPID subject from supported configuration, retaining the existing canonical configured default rather than introducing a guessed support address. Preserve explicit tags, keys, subscriptions, localization, dispatch timing, deep links and the already canonical service-worker defaults. Verification uses fakes/serialization, not live pushes.

Update normal backend fixture labels, frontend auth fixtures, QA runners, current developer/agent guidance and relevant current docs. Migration fixtures intentionally keep source names. README CI links must continue to identify the actual `Duycld03/techdaily` repository; its product/live-site label may use DeepPace. Keep actual checkout/VPS paths, DNS/TLS/CORS compatibility hosts, restore script resource identifiers, archived changes and original source excerpts. Audit remaining matches by their purpose rather than requiring a zero-match repository.

The library delta only corrects the documented temporary directory to match the current `deeppace-uploads` implementation. It does not choose a new source-retention mechanism.

Alternative considered: rename repository, hosts and volumes in this change. Those operational migrations need separate compatibility and rollout work and are outside the user's selected scope.

### 8. Reconcile overlapping plans and verify behavior before sign-off

At apply time, rebase touched requirement blocks and session consumers against the current checkout. `add-admin-accounts-audit` overlaps auth admission/session generations; `add-admin-document-operations` overlaps the ingestion requirement. These changes are not prerequisites, and their task completion is not assumed. If ingestion changes land first, retain their source-retention/retry behavior and apply only the relevant naming correction. Naming-only delta blocks must not revert newer behavior when eventually synced.

Test the migration utility through injected storage adapters, and authentication/admission through existing store/API/middleware test boundaries. Assertions cover values, payloads, precedence, lifecycle and private-content absence; CSS class lists are not layout tests. Update the existing SEO/config assertion that currently requires the old alias.

Behavioral cases include legacy-only, canonical-only, mixed identities, invalid/expired credentials, persistent/session-only/cookie-only modes, false toggles, denied storage, copy/cleanup interruptions, multi-tab contention, logout/account replacement during migration and corrupted/blocked narration stores. A second startup must demonstrate completed phases no longer read legacy sources.

For changed frontend surfaces, the repository requires full `npm test` plus automated headless Chromium with authentic authorized sessions and direct screenshot inspection at desktop 1440x900 (or 1920x1080) and mobile 390x844. Cover English/Vietnamese and light/dark states across the login/admission and affected learner/admin surfaces. Present desktop/mobile evidence to the user before marking the UI work complete; never inject a fake default account to bypass authorization. Run `dotnet test` for touched backend contracts and validate this OpenSpec change strictly.

## Risks / Trade-offs

- Browser storage restrictions or quota → Retain valid sources, checkpoint per phase, defer unavailable work and keep normal canonical login usable.
- Cookie-only persistence cannot be reconstructed → Use session-scoped access state, preserve server refresh authority and avoid silently creating persistent access credentials.
- Old-origin state is inaccessible → Do not claim cross-origin migration; users can authenticate normally on the canonical origin.
- Open legacy tabs can hold IndexedDB connections or write obsolete keys → Report cleanup as pending, honor canonical precedence and require those tabs to reload rather than support indefinite legacy feature reads.
- Rollback to an old Settings/cache writer can recreate remnants → Prefer a compatible repair release; any rollback build must retain the canonical storage contract and completed/tombstone semantics.
- Broad stale spec blocks can obscure a naming-only change → Preserve scenarios, document the scope here, and rebase deltas before sync so newer functional changes are not overwritten.

## Migration Plan

1. Implement the registry, adapters, phase markers and tests before removing scattered readers. Integrate admission and identity fencing in the same release as canonical consumers.
2. Switch Settings and IndexedDB writers with their migration mappings; update product/configuration defaults, normal fixtures and current documentation. Audit every remaining legacy match against the scoped exception policy.
3. Run targeted regressions and the repository's full behavioral and visual gates. Exercise interrupted/repeated migrations and blocked database cleanup with an authenticated test user; record sanitized phase outcomes only.
4. On the first visit after deployment, resolve canonical identity/preferences or safe deferral before admission; migrate narration incrementally. Later visits use canonical consumers and only resume incomplete phases.
5. For failures, ship a repair using the same canonical identifiers and an explicitly versioned phase repair where needed. Do not reverse-copy current credentials/preferences into legacy names, reset completion markers indiscriminately or roll back accepted JWT/refresh authority. Infrastructure identifiers remain untouched throughout.
