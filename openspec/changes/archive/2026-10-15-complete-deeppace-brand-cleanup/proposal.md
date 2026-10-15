# Proposal

## Why

DeepPace still reads TechDaily session and preference keys throughout the application, writes one legacy Settings key, and stores narration in a legacy-named IndexedDB database. Active UI metadata, Web Push defaults, test harnesses and specifications also retain the old identity, allowing the remnants to persist or return during later changes.

## What Changes

- Establish canonical DeepPace browser-storage identifiers and one versioned migration boundary for known legacy cookies, local/session storage and narration cache. Features and HTTP/route authentication consumers read canonical names only.
- Transfer valid legacy data once, prefer existing canonical data, verify each destination before removing its legacy entry, and retain retryable migration progress when storage is blocked. Preserve session-versus-persistent choices, bookmarks, completed-slice markers and supported device preferences without importing unrelated browser data.
- Protect SSR/hydration, account replacement and logout: legacy data cannot overwrite a canonical identity, revive a cleared session or grant permission. Keep current JWT, refresh-cookie and backend authorization contracts.
- Rename the active Settings writer and narration database, consolidate duplicated fallback logic, and remove unused legacy translation identifiers and obsolete branding comments/placeholders.
- Use DeepPace in current product branding, structured metadata, canonical crawler references and notification defaults. Remove the TechDaily structured-data alias and align `robots.txt` with the existing DeepPace sitemap. Reuse configured Web Push contact information rather than inventing a replacement address.
- Update current specs, test fixtures, browser QA scripts and agent/developer guidance to use canonical identifiers. Keep legacy samples only where explicitly testing migration or documenting an actual retained operational resource.
- Preserve the GitHub repository slug, checkout/VPS paths, DNS/TLS hosts, CORS compatibility, database/volume identity and historical archive/migration evidence. This change performs no infrastructure rename, repository metadata mutation or production deployment.

## Capabilities

### New Capabilities

- `client-storage`: Canonical application storage identifiers and a bounded, resumable, privacy-preserving one-time migration of legacy browser data.

### Modified Capabilities

- `auth`: Canonical session consumers, centralized legacy import, preserved persistence mode and SSR/session-cleanup boundaries; update active authentication brand references.
- `reader`: Canonical bookmark, completion and typography persistence and current DeepPace reading terminology.
- `today-reader`: Shared typography persistence using the same canonical contract as the dedicated reader.
- `roadmap`: Canonical saved view-mode preference.
- `knowledge-graph`: Canonical graph-mode and legend preferences.
- `audio-narration`: Canonical narration preferences/cache identity and DeepPace media-session branding without changing playback or synthesis policy.
- `seo-and-crawlers`: DeepPace-only structured identity and consistent canonical sitemap references.
- `core-platform`: Current app/notification branding, QA credential identifiers and developer guidance without renaming operational resources.
- `library`: Align the documented upload-storage identifier with the current DeepPace implementation; preserve ingestion limits and ownership.

## Impact

- Frontend: authentication store/API client/middleware, startup admission boundary, reader/Library/Settings/Graph/Roadmap preferences, IndexedDB adapter, login copy, locale dictionaries, Nuxt metadata and static crawler assets.
- Backend: remove remaining TechDaily notification/contact fallbacks while retaining configured values, notification behavior, JWT validation and existing API contracts. No database migration or credential reset is required by this change.
- Verification/docs: canonical unit/E2E fixtures and agent instructions, dedicated mixed-state/migration regressions, browser storage/IndexedDB verification and authenticated desktop/mobile UI evidence. Preserve full existing requirement blocks/scenarios when normalizing their identifiers.
- Shared work: coordinate touched auth/session consumers with `add-admin-accounts-audit`, and preserve the ingestion contracts planned in `add-admin-document-operations`. These are integration overlaps, not authorization to apply or rewrite either change. Catalog UI parity work remains separate.
- Excluded: cross-origin browser-data transfer, infrastructure/repository rename, schema/table renaming, new authentication methods, learning-data rewrites, layout redesign, new AI generation and rewriting archived OpenSpec history or original source excerpts.
