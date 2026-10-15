# Client Storage Specification

## Purpose

Keep DeepPace browser state under canonical application identifiers and safely retire known legacy data through a versioned migration without losing current preferences or weakening authentication.

## Requirements

### Requirement: Canonical application storage identity
Application-owned session and preference records SHALL use registered `deeppace_*` keys; the narration database SHALL use `deeppace-audio`. Ordinary feature, route and HTTP consumers SHALL read canonical identifiers only. Known TechDaily identifiers SHALL be confined to the migration boundary, its focused tests and explicit historical/operational documentation. Standard third-party keys and `refreshToken` SHALL retain their contracts.

#### Scenario: Current application creates browser state
- **WHEN** a user signs in, changes supported preferences, reads a book or caches narration
- **THEN** new application-owned state uses the registered DeepPace identifiers
- **AND** ordinary application behavior creates no TechDaily key or database.

#### Scenario: Existing standard storage keys
- **WHEN** the application uses refresh cookies, locale/theme libraries or unrelated site storage
- **THEN** branding cleanup does not rename their identifiers or clear their data.

### Requirement: Versioned one-time legacy transfer
A versioned migration SHALL import only recognized legacy application records before their canonical consumers initialize. Each record or phase SHALL keep completion state in its appropriate persistent or session scope. Completed migration work SHALL not repeatedly inspect legacy values on later visits. Supported incomplete phases SHALL remain safely retryable without treating partial work as complete.

#### Scenario: First visit with legacy preferences
- **WHEN** a browser has recognized legacy preferences and no corresponding canonical values
- **THEN** validated preferences transfer to their canonical identifiers once
- **AND** their completed migration state prevents repeated legacy reads.

#### Scenario: Already migrated browser
- **WHEN** a browser has completed the current migration version
- **THEN** normal startup and feature reads use canonical data directly without repeating legacy-key fallback.

#### Scenario: Migration interrupted halfway
- **WHEN** migration stops after transferring some entries
- **THEN** a later attempt resumes incomplete work without duplicating or overwriting completed canonical entries.

### Requirement: Canonical data precedence
Existing canonical records SHALL take precedence over legacy counterparts. Migration SHALL not overwrite them with older data or merge different identities. A canonical record that fails feature validation SHALL follow that feature's safe recovery behavior, without silently reviving a legacy session. Conflicting or superseded legacy records SHALL be retired according to the migration policy.

#### Scenario: Both preference names exist
- **WHEN** canonical and legacy view-mode values coexist
- **THEN** the canonical value is retained and its legacy counterpart is removed without replacing the user's current choice.

#### Scenario: Canonical and legacy accounts differ
- **WHEN** canonical credentials identify one account and legacy credentials identify another
- **THEN** canonical identity remains authoritative and no legacy profile or token is mixed into it.

#### Scenario: Canonical credentials are invalid
- **WHEN** canonical session validation fails while old credentials remain
- **THEN** the usual refresh or login flow handles the failure
- **AND** the old session cannot silently reappear through a feature-level fallback.

### Requirement: Validated transfer and safe source removal
Recognized legacy records SHALL be validated by their supported schema. Transfer SHALL confirm the canonical write before deleting the source entry. Invalid recognized values SHALL be discarded safely without executing content or exposing it in diagnostics. Cleanup SHALL target known application records rather than clearing all local/session storage or browser databases.

#### Scenario: Destination write fails
- **WHEN** browser quota or access restrictions prevent a canonical write
- **THEN** the valid legacy source is retained and that transfer remains incomplete.

#### Scenario: Malformed or unrelated data
- **WHEN** a known preference contains malformed JSON, an unsupported enum or an invalid numeric value
- **THEN** it does not become an active preference and the feature uses its documented default.
- **WHEN** an unrelated browser entry exists
- **THEN** migration leaves it untouched.

#### Scenario: Successful copy followed by restart
- **WHEN** an entry was copied but cleanup/completion recording was interrupted
- **THEN** resumption recognizes the canonical destination and safely removes only the superseded source.

### Requirement: Session identity and persistence preservation
Session migration SHALL keep token/profile identity consistent and preserve a known persistent or session-only mode. Session-only credentials SHALL not be promoted to persistent storage. Ambiguous cookie-only legacy state SHALL use conservative persistence until the server's existing refresh authority resolves it. Migration SHALL not weaken JWT validation, authorize a user or issue credentials itself.

#### Scenario: Legacy session-only sign-in
- **WHEN** valid legacy credentials belong to session storage and a session-only browser session
- **THEN** migration uses canonical session storage/session cookies and writes no persistent access credentials.

#### Scenario: Legacy persistent sign-in
- **WHEN** a known persistent legacy session is migrated
- **THEN** its access credentials retain persistent storage behavior and the existing refresh-cookie policy is preserved.

#### Scenario: Invalid token or mismatched profile
- **WHEN** legacy token data is expired/malformed or its profile belongs to a different identity
- **THEN** no inconsistent authenticated session is constructed
- **AND** existing refresh/login and server permission checks remain authoritative.

### Requirement: Private admission during SSR migration
When canonical identity cannot be determined until browser storage is available, SSR SHALL render a neutral admission state without protected learner/admin markup or requests. Client startup SHALL resolve canonical precedence and migration before ordinary session consumers run. Safe return destinations and existing permission verification SHALL remain intact.

#### Scenario: Legacy cookie conflicts with canonical browser storage
- **WHEN** SSR receives only a legacy access cookie but the browser also holds another canonical identity in local/session storage
- **THEN** SSR does not expose protected content for the legacy identity
- **AND** client admission resolves the canonical identity before protected requests or hydration content appear.

#### Scenario: Guest without migration candidates
- **WHEN** no canonical credentials, eligible refresh cookie or migration candidate exists
- **THEN** existing guest redirect behavior applies without showing a protected shell.

### Requirement: Safe bookmark and preference migration
Migration SHALL preserve recognized per-book bookmark/completed-slice identifiers and supported typography, roadmap, graph and Settings choices under canonical keys. It SHALL validate key/value shapes without changing document ownership, server progress or document language. Local migration SHALL not grant access to any book or copy learning records between accounts.

#### Scenario: Reader state transfers
- **WHEN** valid legacy bookmark and completed-slice entries have no canonical counterparts
- **THEN** their book identity and supported values transfer without creating documents or advancing server learning progress.

#### Scenario: False and default-valued preferences
- **WHEN** a saved toggle is false or a saved value equals a supported default
- **THEN** migration preserves that explicit choice rather than replacing it through truthiness-based fallback.

#### Scenario: Book access after migration
- **WHEN** a migrated local entry references a book the current user cannot access
- **THEN** the existing backend ownership check still rejects access and no private content is obtained from that entry.

### Requirement: Bounded narration cache migration
Recognized legacy narration cache stores SHALL transfer to the canonical database with compatible cache keys and validated metadata in bounded batches. Existing canonical cache entries SHALL win conflicts, and current eviction limits SHALL still apply. Cache migration SHALL perform no synthesis or remote generation and SHALL not block usable canonical application state while storage is unavailable.

#### Scenario: Legacy cache hit after transfer
- **WHEN** a compatible complete narration entry transfers successfully
- **THEN** canonical playback can reuse the cached audio without synthesizing it again.

#### Scenario: Blocked or interrupted database migration
- **WHEN** another open connection, quota or interruption prevents completion
- **THEN** the legacy database is not falsely reported as removed and incomplete work remains recoverable
- **AND** the app remains usable with canonical data and safe cache-miss behavior.

#### Scenario: Corrupt partial cache or obsolete metadata
- **WHEN** a cache entry cannot satisfy the current schema/limits
- **THEN** it is discarded under the existing cache recovery/eviction policy without presenting it as valid playback data.

### Requirement: Logout and concurrent migration safety
Logout, terminal session cleanup and account replacement SHALL fence pending identity migration. Cleared or superseded credentials SHALL not be restored by delayed work or another migration pass. Concurrent migrations SHALL be serialized where supported or safely deferred; no completion claim SHALL rely on an unsafe concurrent overwrite.

#### Scenario: Logout during migration
- **WHEN** the user logs out while session migration is pending
- **THEN** delayed migration cannot rewrite credentials or repopulate the previous user's session
- **AND** identity migration completion/cleanup state prevents legacy resurrection.

#### Scenario: Multiple tabs start migration
- **WHEN** two updated tabs attempt the same incomplete transfer
- **THEN** they produce one consistent canonical result without duplicate cleanup or clobbering a current canonical value.

### Requirement: Origin and diagnostic boundaries
Migration SHALL operate only on data accessible within the current origin and supported cookie scope. It SHALL not fetch credentials or browser data from the retained legacy domain. Diagnostics SHALL expose bounded phase/error information without token, profile, bookmark, document or audio contents. Archived history and actual infrastructure identifiers SHALL not be bulk-rewritten.

#### Scenario: New domain cannot access old-origin storage
- **WHEN** a browser has storage on another origin only
- **THEN** the app does not claim that data was migrated or attempt cross-origin credential transfer
- **AND** normal authenticated access remains available.

#### Scenario: Migration error reported
- **WHEN** storage migration fails
- **THEN** diagnostics identify the safe failure/phase without logging or displaying stored private values.
