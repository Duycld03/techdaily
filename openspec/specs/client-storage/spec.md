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

### Requirement: Explicit Narration Preference Persistence

Narration preference SHALL use a registered, versioned DeepPace browser record supporting Auto, System, Cloud, and Local. Auto SHALL be the default. Automatic fallback SHALL not write an explicit preference. The old ambiguous engine value SHALL be retired through the centralized storage boundary without guessing whether it represented a manual choice or fallback.

#### Scenario: Old engine value has no intent provenance
- **WHEN** only the old engine-selection value exists
- **THEN** the new preference defaults to Auto rather than importing a possibly automatic fallback as manual intent
- **AND** existing voice, speed, bookmarks, audio caches, and unrelated preferences are preserved.

#### Scenario: Explicit canonical preference already exists
- **WHEN** the new valid preference and the old ambiguous value both exist
- **THEN** the explicit canonical preference wins and the obsolete value is retired without replacing it.

#### Scenario: Automatic engine transition
- **WHEN** Auto uses Cloud or Local after a failure
- **THEN** the stored preference remains Auto for later playback requests.

#### Scenario: Preference destination is unavailable
- **WHEN** storage restrictions prevent creating or verifying the canonical preference
- **THEN** the current session can still use Auto or a newly selected in-memory preference
- **AND** persistence is not falsely reported as successful or used as a reason to stop playback.

#### Scenario: Preference migration coordination is deferred
- **WHEN** startup coordination is unavailable or occupied before preference retirement completes
- **THEN** narration uses a valid new canonical preference if present, otherwise in-memory Auto, without reading the old engine value in feature consumers
- **AND** retirement and completion stay pending for a later coordinated startup attempt without changing identity-migration semantics.

### Requirement: Playback Checkpoint Identity and Validation

Listening checkpoints SHALL be separate from generated audio and credentials. A checkpoint SHALL match the current account, slice, engine, voice, and narration-content identity before restoration. It SHALL carry a supported version, compatible cursor, completion state, and update time. Malformed, non-finite, negative, incompatible, or out-of-range values SHALL not become playback positions.

#### Scenario: Account-scoped lookup
- **WHEN** account B opens the same slice previously heard by account A
- **THEN** account A's listening checkpoint is neither restored nor overwritten as account B's progress.

#### Scenario: Voice or narration changes
- **WHEN** content identity or voice differs from a saved checkpoint
- **THEN** the checkpoint is ineligible and playback begins at the start.

#### Scenario: Invalid stored cursor
- **WHEN** a record contains malformed data, an unsupported version, or a cursor invalid for the current narration
- **THEN** the record is ignored safely and listening remains available without importing legacy fallback data.

#### Scenario: Audio cache is not a listening checkpoint
- **WHEN** partial synthesis contains more generated sentences than the user has heard
- **THEN** the generated-sentence count does not replace the saved listening position.

#### Scenario: Completed listening record
- **WHEN** a valid checkpoint records natural completion
- **THEN** it is treated as a replay-from-start marker, not as a command to auto-advance on reload.

### Requirement: Bounded Best-Effort Listening Checkpoints

Checkpoint storage SHALL have a bounded retained record set and bounded record size. Active listening SHALL be checkpointed periodically and on pause, seek, source replacement, visibility loss, and natural completion when storage is available. Writes SHALL preserve unrelated data and record the latest intentional position, including backward seeks, rather than always the furthest position.

#### Scenario: Periodic checkpoint survives abrupt termination
- **WHEN** a page is terminated without a final lifecycle event
- **THEN** a later playback request can restore the last successfully persisted checkpoint
- **AND** the application does not promise recovery of progress after that checkpoint.

#### Scenario: User seeks backward
- **WHEN** the user seeks backward and the new position is saved
- **THEN** reopening restores that more recent intended position rather than an older furthest position.

#### Scenario: Retention bound is reached
- **WHEN** saving progress would exceed the checkpoint retention bound
- **THEN** the least recently updated checkpoint is evicted without clearing unrelated browser storage or audio caches.

#### Scenario: Storage denied or exhausted
- **WHEN** checkpoint storage cannot be read or written
- **THEN** in-session listening remains usable and a bounded localized notice explains that cross-reload position saving is unavailable
- **AND** no successful-persistence claim is shown.

#### Scenario: Browser data is cleared
- **WHEN** the user or browser clears this origin's playback progress
- **THEN** playback starts normally without claiming that the deleted position can be restored from the server or another device.

### Requirement: Playback Checkpoint Lifecycle Fencing

Checkpoint commits SHALL be bound to the current account and narration session. Logout, account replacement, source/engine replacement, and disposal SHALL invalidate delayed writes. Existing account-scoped checkpoints SHALL not authenticate users, grant document access, or alter reading bookmarks, completed-slice sets, or server learning progress.

#### Scenario: Delayed save after account replacement
- **WHEN** account A has a scheduled save and account B becomes active before it executes
- **THEN** the delayed save cannot write under account B or reactivate account A's playback session.

#### Scenario: Replaced narration finishes late
- **WHEN** a superseded narration session resolves after the current session has saved a checkpoint
- **THEN** it cannot overwrite that checkpoint or record false completion.

#### Scenario: Existing bookmark and admission boundaries remain authoritative
- **WHEN** a listening checkpoint references a different slice or an inaccessible document
- **THEN** it does not navigate to that slice, bypass access checks, or mark a learning milestone complete.

#### Scenario: Same account signs in again
- **WHEN** the same account later returns on this browser and its validated checkpoint remains stored
- **THEN** that checkpoint can become eligible after ordinary authenticated document access is established.
