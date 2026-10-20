# Design

## Context

See `proposal.md` for motivation and the two delta specs for the behavior contract. This design is required because engine policy, asynchronous playback, browser persistence, and storage migration cross several frontend modules.

Observed implementation:
- `frontend/app/composables/useSliceAudio.ts` owns all three engines, media events, worker cancellation, quota, and IndexedDB access. `currentTime` and `systemSentenceIndex` are in-memory only. `setEngineMode()` and automatic System fallback both write the same engine key, which is later treated as a manual override.
- System speech already uses sentence-level utterances. Pause cancels the utterance and preserves its sentence index; the Web Speech API does not provide an application-owned audio file. Its elapsed-time ticker is an estimate, not an audio-alignment source.
- Cloud fetch awaits a complete Blob; Local stores complete WAVs and partial PCM buffers. Local streaming replaces the media source with sentence fragments, so fragment `currentTime` is not whole-slice elapsed time.
- `ReaderAudioPlayer.vue` passes the old timestamp during engine changes, prepares uncached audio while paused, and can autoplay after a voice change. Its source watcher currently observes only the slice ID.
- `clientStorage.ts`, `legacyStorageMigration.ts`, and `00.client-storage.ts` provide the existing canonical storage/startup boundary. `useAuthStore` exposes user identity and `sessionGeneration` for account replacement/terminal cleanup.
- Existing behavioral tests replace the media element. The underrun case emits `ended` without the preceding native `pause`; the exploration's isolated Chromium WAV probe observed both events in that order. This is evidence of a simulation gap, not proof of a live narration stall.
- The durable spec conflicts with itself about Cloud-default versus System-first selection, paused preparation versus no synthesis, and cross-engine position transfer. Older single-row presentation wording also conflicts with the current two-row toolbar requirement and implementation. The delta resolves the affected wording to the existing two-row layout; no new layout archetype is introduced.

## Goals / Non-Goals

**Goals:**
- Give engine selection one owner, distinguish user preference from session resolution, and make downstream fallback finite and cancellation-safe.
- Persist a truthful engine-local cursor without interpreting generated-sentence count or estimated System time as listening progress.
- Add small, versioned browser records without replacing current audio cache keys, changing authentication persistence, or introducing a backend progress API.
- Keep the existing reader, Today, media-session, sleep-timer, and natural-completion boundaries intact.

**Non-Goals:**
- Word-level Web Speech resume, cross-engine alignment, synchronized highlighting, cross-device/live cross-tab playback, or recording browser speech.
- Provider replacement, parallel Cloud sub-batches, streaming Cloud responses, backend deduplication, quota redesign, or backend cache-trust/security remediation.
- Re-encoding cached audio, changing worker model/precision/threading, adding dependencies, or redesigning unrelated reader chrome.
- Reworking general partial-cache eviction/write amplification; checkpoint retention is bounded independently of that existing cache behavior.

## Decisions

### 1. Persist preference, resolve an effective engine per playback session

Introduce `AudioEnginePreference = auto | system | cloud | device`, while retaining `AudioEngine` for an actual engine. The existing toolbar gains Auto and continues to expose explicit choices; it distinguishes the selected preference from the engine currently speaking. Existing voice controls follow the effective engine during an active session.

Put pure selection/failure classification and bounded voice discovery in `frontend/app/utils/audioEnginePolicy.ts`. Keep engine execution in `useSliceAudio.ts`; do not broaden this into a framework or move unrelated reader logic. Preference writes occur only at explicit selection or the one-time transition described below.

For Auto, a new playback session evaluates System, then Cloud, then Device. Explicit System or Cloud begins at that engine and can only fall downstream after a hard failure; explicit Device has no further fallback. Maintain an attempted-engine set for the current session. A later user playback session retries the configured policy rather than persisting a previous failure as preference. Cached progress never determines which engine wins Auto.

**Alternative rejected:** continuing to save the effective engine under the old key. It cannot distinguish a user choice from a transient provider failure and defeats System-first behavior after reopening.

### 2. Discover voices without speculative paid/local work

If the speech API is absent, System is unavailable immediately. Otherwise check matching voices and listen for `voiceschanged` until a matching voice appears or a named `SYSTEM_VOICE_DISCOVERY_TIMEOUT_MS = 1500` deadline expires. Remove the listener and timer on resolution or cancellation; use event listeners rather than overwriting the shared `onvoiceschanged` property.

The deadline is a proposed implementation default, not a guaranteed speech-start latency. It prevents an initially empty list from immediately spending Cloud quota while remaining finite on browsers that never expose the requested language. No Cloud generation, local worker/model initialization, or inference runs concurrently with discovery. Quota checking must not delay successful System speech; reuse existing quota state and query it when Cloud becomes a candidate.

Classify failure once: System synthesis/voice unavailability, Cloud network/provider failure, and existing quota exhaustion are downstream-fallback reasons. `AbortError`, explicit pause/seek/cancel, stale sessions, buffering, and autoplay permission rejection are not. Keep access/validation errors visible rather than masking 401/403/invalid requests as provider outages. Preserve the existing near-limit and hard-limit values.

**Alternative rejected:** assuming browser voices are always local/offline or immediately available. A browser-provided voice can still fail at runtime; discovery and synthesis failure are distinct states.

### 3. Separate playback intent, generation state, and native media state

A session captures a monotonic generation, account ID/session generation, slice/content identity, preference, resolved voice/engine, attempted engines, and playback intent. `useSliceAudio.ts` remains the integration owner. Every asynchronous boundary checks that captured session before changing playback, reporting fallback, committing progress, or emitting natural completion. Within that session, identify each active utterance, media source, and pending playback continuation with an operation revision; all relevant callbacks must also match the current operation.

Keep intended continuation separate from the media element's `paused` flag and synthesis readiness. Natural fragment pause/end means buffering or next-fragment playback, not user pause or whole-slice completion. A user pause preserves the existing ability for Local synthesis to continue, but no late chunk or Cloud completion may turn the user's intent back on. Pause during pending preparation must not start fallback in the background.

On source/content, engine, voice, account, or route replacement, save the outgoing valid checkpoint where permitted, invalidate the session, abort its Cloud fetch, cancel its worker job/utterance, stop its carrier/timers, and detach obsolete media handlers. Seek, pause/resume replacement, a replacement utterance, and fragment/full-file handoff retire superseded operation revisions without resetting the session's attempted-engine set. Reject old start/end/error/time/metadata callbacks and play continuations even if their session still matches. Existing worker inference serialization stays intact; these checks complement it. Dispose must fence queued saves before releasing resources.

Use the auth store's existing identity/session-generation signal rather than changing token validation or inventing an audio identity. Token refresh for the same admitted session must not be treated as an account switch.

**Alternative rejected:** using only `(chunkId, voice, contentHash)` as a request token. Two superseding requests can have the same cache identity, and old errors can arrive before any Blob-key check.

### 4. Make start reason explicit and never transfer a timeline between engines

Replace ambiguous offset-driven calls with an explicit start context: ordinary user playback/reopening, manual engine change, fallback, voice/source change, or natural auto-advance. Only ordinary playback after resolving the same identity can restore a checkpoint. Initial voice/eligibility checks after reload do not themselves constitute a transition from playing audio: if System is unavailable and Cloud is initially resolved with matching cached audio, its valid Cloud checkpoint can resume. Once an engine attempt has begun, downstream failure recovery uses the explicit fallback restart reason.

An actual engine change starts at zero even if the target engine has an older checkpoint. Remove cross-engine time/percentage conversion and its obsolete tests; do not retain a compatibility offset path for it. Selecting the already effective engine with no other change is a no-op for position. Selecting Auto while a fallback engine is active re-evaluates the policy; if that changes the effective engine, the same restart rule applies.

A paused/idle engine or voice selection updates preference and invalidates obsolete work; it can inspect existing cache metadata but cannot generate, download a model, speak, or autoplay. An actual paused engine change retains a pending `engine-change` restart reason until the next Listen; that reason overrides ordinary restoration and any older target-engine checkpoint, even after loaded UI state was cleared. Active selection and explicit error recovery are playback requests and can start the newly selected narration. Natural auto-advance starts the next slice from zero and is never inferred from restored state or a fragment/carrier event.

### 5. Use a cursor that matches the actual playback surface

Implement validation, immutable record updates, identity matching, and cursor conversion in `frontend/app/utils/audioPlaybackProgress.ts`.

| Cursor | Saved position | Restoration |
|---|---|---|
| System sentence | Zero-based sentence index | Speak from the beginning of that sentence; never use the estimated time ticker as a persisted byte/word offset. |
| Complete cached Cloud/Local file | Whole-file elapsed seconds | Wait for current-source metadata, reject invalid/out-of-range values, then seek before playing. Playback speed does not rescale the cursor. |
| Streaming Local | Sentence index, seconds within that sentence, and whole-slice elapsed seconds | Use actual PCM durations and the matching partial prefix to locate the audible sentence; if the matching complete file now exists, use the recorded whole-slice elapsed seconds. |

Track a Local segment's start in whole-slice seconds from actual sample counts. While streaming, `segmentStart + media.currentTime` produces a truthful global checkpoint; do not persist the raw fragment clock. Keep generation progress separate: five generated sentences can coexist with a listening checkpoint in sentence two.

A complete-file cursor requires the matching cached audio. A missing/invalid referenced complete or partial cache resets playback to zero rather than applying old timing to newly synthesized output. Generation can still reuse any independently valid partial prefix. At full-file handoff, preserve the global offset and update the cursor to the complete-file representation even while paused. No persistent sentence-alignment manifest or new audio-cache format is needed.

Voice/content mismatch, invalid cursor, or a completed record also produces a safe start at zero. A completed record is not a navigation command. The existing per-book slice bookmark continues to choose the visible slice; a listening checkpoint never overrides it or moves server learning progress.

**Alternative rejected:** one percentage or `currentTime` field for every engine. It cannot represent browser speech or Local fragments truthfully. Requiring word timestamps or Cloud sentence assets would expand the agreed scope.

### 6. Keep small browser records separate from audio assets

Register new canonical keys in `CLIENT_STORAGE`:
- `deeppace_reader_audio_preference`: a version-1 record containing the explicit mode.
- `deeppace_reader_audio_progress`: a version-1 collection of validated checkpoints, scoped by account/slice/engine/voice/content identity.

Each checkpoint contains that identity, its discriminated cursor, completion flag, and update time; it contains no token, profile, document text, or audio payload. Use localStorage through the existing guarded storage adapter. This provides same-browser persistence without changing session-only credential behavior. Without an admitted account, do not restore or persist an account checkpoint.

Proposed named defaults: retain at most 100 checkpoints per origin, reject stored collections larger than 64 KiB before parsing, and save during active playback at most once every 5 seconds. Immediately flush on pause, successful seek, visible-source replacement, hidden `visibilitychange`, `pagehide`, and natural completion. A System sentence transition can checkpoint its boundary without treating it as media time. Do not rely on `beforeunload` or serialize audio on these events.

Backward seeks overwrite progress with the latest intentional position, not the furthest position. Re-read and merge validated existing records before immutable updates; preserve other identities and unrelated storage. Retention removes the least recently updated checkpoint. No cross-tab controller or progress synchronization is promised; concurrent writes to the same checkpoint use the latest successful commit, not a furthest-position merge.

Capture account/session identity when scheduling saves and recheck it at commit. Cancel queued writes on replacement or logout. Already committed records can remain for the same account's later return but are not exposed to another account. Storage denial/corruption must keep current listening functional, disable unavailable persistence for that session, and show one localized notice rather than claiming a save succeeded.

### 7. Retire the ambiguous old preference through the existing startup boundary

The old `deeppace_reader_audio_engine` string has no provenance: it could be a manual choice or an automatic fallback. Use a new independently completed narration-preference migration phase in the existing centralized storage boundary. Attempt it before narration consumers initialize, even when earlier identity/preferences phases already completed. If existing coordination returns `deferred` because Web Locks are unavailable or occupied, initialize consumers from a valid new canonical preference, otherwise in-memory Auto; they must never consult the old key. Leave retirement/completion pending for a later coordinated startup attempt and preserve existing identity-admission semantics.

If a valid new preference exists, preserve it. Otherwise write and verify version-1 Auto before retiring the old string. If storage is unavailable, use in-memory Auto without deleting an unconfirmed source or claiming completion. Do not reset the existing identity/audio migration phases, search old-origin data, rename the audio database, invalidate models, or clear browser storage. Keep speed, language-scoped voices, typography, and per-book bookmarks intact.

The one-time reset of an old manual choice is an explicit compatibility trade-off: the user can choose that engine again, but an automatic fallback must not silently become permanent. New explicit choices persist thereafter.

### 8. Preserve integration and make verification consumer-visible

Wire account identity and start reasons at `ReaderAudioPlayer.vue`, keeping auth composables initialized synchronously. Observe narration identity changes, not only slice ID, so reformatted content cannot resume stale audio. Retain the current two-row layout and add localized Auto/effective-engine, fallback, restart, and persistence-unavailable copy in both existing locale files. Do not add a new settings page or pitch control.

Reader and Today consumption continue through the shared player. Verify `/read/[bookId]` auto-advance only after genuine whole-slice completion and `/today` never advances because of audio completion or checkpoint restoration. Preserve sleep-timer and media-session actions, including explicit remote Play as a playback request.

Update existing behavioral suites with native pause-before-ended ordering and delayed callbacks. New focused tests cover identity/cursor validation, migration destination failure, interrupted transition, backward seek, bounded persistence, and restart/restore precedence. Do not add CSS-class assertions, source-text tests, or mocks that only echo method forwarding.

During implementation, run the full frontend tests and typecheck, then drive the real application in Chromium with an authenticated test account. Exercise a real supported narration engine, reload/reopen resume, natural completion, and cached Local playback; use controlled failure injection only for unavailable/error branches and distinguish it from real provider/model execution. Capture and inspect English/Vietnamese desktop (1440x900 or 1920x1080) and mobile (390x844) screenshots on the actual reader/Today surfaces. Browser/OS voices and mobile native speech behavior must not be claimed from mocked headless voices.

## Risks / Trade-offs

- System may repeat the beginning of the current sentence after reopening -> this is the promised granularity, not word-perfect resume.
- A browser voice can depend on network services -> retain runtime fallback rather than labeling System inherently offline.
- Device audio can be evicted or synthesized differently after cache loss -> reset its cursor instead of claiming exact restoration on a new asset.
- Abrupt termination can lose progress since the last successful checkpoint -> bound periodic writes and flush supported lifecycle events, without promising an unload callback.
- Web Storage can be denied or edited -> validate bounded records and continue current listening with an explicit persistence notice.
- The old preference cannot prove user intent -> reset only that selection to Auto once and preserve other data.
- Existing Cloud request script/hash trust and quota accounting risks remain -> do not increase Cloud concurrency or claim this frontend change fixes backend security; this change keeps existing backend contracts.
- The existing general partial-cache accounting/write-volume risks remain -> do not confuse the bounded checkpoint collection with a repair of the audio cache's eviction policy.

## Migration Plan

1. Implement the registry, pure policy/progress helpers, and independently completed preference transition without changing existing identity migration behavior.
2. Integrate session fencing, explicit start reasons, and per-engine cursor capture/restore; migrate every affected player caller and remove obsolete cross-engine offset behavior.
3. Integrate controls/localization and update the existing README audio descriptions. Leave backend schemas, endpoints, secrets, worker models, and audio cache formats untouched.
4. Pass behavioral/type and real-browser verification gates before marking implementation tasks complete.
5. Rollback can deploy the previous frontend: its old engine key is absent so it uses its existing default; the new preference/progress records remain unused. Keep existing audio caches intact. Reapplying the new frontend recognizes its versioned records without rerunning completed identity migrations.
