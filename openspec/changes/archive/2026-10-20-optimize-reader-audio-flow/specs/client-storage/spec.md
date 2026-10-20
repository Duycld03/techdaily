# Client Storage Spec Delta

## ADDED Requirements

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
