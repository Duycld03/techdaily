# Audio Narration Spec Delta

Historical scenario titles under MODIFIED requirements remain stable identifiers for archive compatibility. Their updated normative outcomes replace the former defaults, transitions, and obsolete layout wording.

## MODIFIED Requirements

### Requirement: On-Device Slice Narration Synthesis

When the local engine is selected explicitly or reached as the final fallback, the reader SHALL synthesize narration audio for a slice entirely on the client, using an on-device neural text-to-speech model run in a Web Worker, without a server synthesis request, external API credential, or usage quota. All synthesis code SHALL be client-only and MUST NOT execute during server-side rendering.

Narration SHALL be available only for AI-formatted slices; the reader SHALL NOT synthesize non-formatted slices. The narration script SHALL remove markdown markup and fenced code blocks while preserving headings and prose.

The local voice SHALL follow the slice language (`en` or `vi`), falling back to the book language and then `en` only when language is unknown. A known Vietnamese slice MUST use the Vietnamese voice. Local narration SHALL have one voice per supported language without a voice picker.

The model SHALL download lazily only when local narration is actually requested, with localized download/synthesis progress. Successfully cached model resources SHALL be reused, including offline when all required resources are present. An unavailable uncached model SHALL produce an actionable error rather than an unconditional offline-success promise.

#### Scenario: Synthesis runs on-device without a server request
- **WHEN** a user plays an AI-formatted slice with the local engine selected
- **THEN** synthesis runs through the local worker with no server text-to-speech request.

#### Scenario: Narration gated on AI formatting
- **WHEN** the current slice is not AI-formatted
- **THEN** narration is hidden or disabled and no engine synthesizes it.

#### Scenario: Code fences excluded from narration
- **WHEN** formatted content contains fenced code blocks
- **THEN** the narration script omits code contents while retaining headings and prose.

#### Scenario: Voice selected automatically by content language
- **WHEN** local narration is requested for a Vietnamese slice
- **THEN** the Vietnamese model is selected without a voice prompt.

#### Scenario: Known non-English language is not narrated with the English voice
- **WHEN** the slice or its book reports Vietnamese
- **THEN** local narration uses the Vietnamese voice rather than the English fallback.

#### Scenario: Voice model downloaded once and cached for offline reuse
- **WHEN** a model has been downloaded successfully and its required resources remain cached
- **THEN** later local narration reuses those resources without downloading them again, including offline.

#### Scenario: Offline fallback has no usable model
- **WHEN** local fallback is reached offline and required model resources are absent
- **THEN** the player explains that local narration cannot be prepared and remains stopped without claiming playback succeeded.

### Requirement: Google Cloud Narration Synthesis with Database Caching

The system SHALL support server-side narration through Google Cloud Text-to-Speech as the second automatic engine after System or as an explicit user preference. All Google Cloud TTS calls SHALL be proxied through the ASP.NET Core backend using the server-side API key (`Google__TtsApiKey`); external API credentials SHALL NOT be exposed to the client.

Synthesis and quota requests SHALL use canonical relative backend routes in production and SHALL NOT fall back to hardcoded localhost addresses when configuration is empty.

The backend SHALL persist complete MP3 files in PostgreSQL (`DocumentChunkAudios`) keyed by `(DocumentChunkId, ContentHash, VoiceId)`. A matching cache hit SHALL return stored audio without a provider call. A miss SHALL partition scripts exceeding 4,500 UTF-8 bytes into sentence-bounded sub-chunks under 4,500 bytes, synthesize them in order, concatenate their MP3 output, and store the complete audio and total character count. The client SHALL cache returned audio in IndexedDB under the matching slice/voice/content identity so seeks, pauses, and replays need no network request.

A changed content hash SHALL invalidate the prior audio identity and cause new synthesis on the next uncached playback request.

#### Scenario: Database cache hit serves audio without calling Google Cloud API
- **WHEN** the requested slice/hash/voice audio is already cached in PostgreSQL
- **THEN** the server retains its existing under-50ms cache-hit contract and makes no Google Cloud TTS request.

#### Scenario: Long text exceeding 5,000 bytes is automatically partitioned and concatenated
- **WHEN** a requested narration script exceeds 4,500 UTF-8 bytes
- **THEN** the server partitions it into sentence-bounded batches under 4,500 bytes and returns one concatenated MP3 without a provider input-limit error.

#### Scenario: Database cache miss synthesizes, stores in PostgreSQL, and streams audio
- **WHEN** requested narration is not cached in PostgreSQL
- **THEN** the server synthesizes it, persists the complete MP3, and returns its audio payload.

#### Scenario: Client stores server-synthesized audio in IndexedDB
- **WHEN** the client receives Cloud audio
- **THEN** it caches the audio in IndexedDB so subsequent local replays do not repeat the request.

#### Scenario: Slice content revision invalidates server cache
- **WHEN** reformatting or editing changes the narration content hash
- **THEN** audio under the old hash is not served as the revised narration.

#### Scenario: Production synthesis requests resolve to relative API endpoint without localhost fallback
- **WHEN** production API-base configuration is empty
- **THEN** synthesis uses the relative `/api/v1/library/chunks/.../audio` route without localhost fallback, mixed content, or connection-refused errors from a fabricated host.

### Requirement: System TTS Engine via Web Speech API

The reader SHALL support System TTS through the browser Web Speech API alongside Cloud and Local.

1. **Engine Selection**: A System option SHALL remain available, and an explicit selection SHALL persist as a preference distinct from the effective engine.
2. **Local Voice Discovery**: System selection SHALL enumerate and filter matching browser/OS voices, including supported Android Google Speech Services voices. Initial discovery SHALL be bounded and asynchronous when matching voices are not immediately exposed.
3. **Voice Customization & Persistence**: A preferred System voice SHALL remain selectable and persistent.
4. **Playback Rate & Pitch**: Existing System playback-speed support up to 2.0x and configured utterance pitch SHALL remain honored without adding an unused pitch control.
5. **Browser Speech Startup**: After a suitable voice is discovered, System SHALL speak through utterances without application model downloads, server synthesis requests, or Cloud quota consumption. Discovery and browser voice startup SHALL NOT be described as a guaranteed zero-latency operation. The application SHALL NOT claim to cache a returned System audio file.

#### Scenario: User selects System TTS engine on Android
- **WHEN** System is selected on Android with a matching Google Speech Services voice
- **THEN** matching System voices, including `vi-vn-x-vic-local` when installed, are available
- **AND** narration starts after discovery without an application model download or backend character usage.

#### Scenario: System TTS honors 2.0x playback speed
- **WHEN** the user sets 2.0x and requests System narration
- **THEN** the utterance rate is 2.0.

### Requirement: Dual-Engine Audio Toggle and Server Quota Guard

The reader SHALL offer Auto, System, Google Cloud, and Local engine preferences. Auto SHALL be the default and SHALL use the Cascading Audio Engine Resolution Hierarchy. Only an explicit preference change SHALL alter the persisted preference; an automatic fallback SHALL update the effective engine without changing that preference.

Engine transitions SHALL stop the previous engine, cancel its pending work, preserve valid local synthesis partials, and show progress only for the effective engine. Quota state SHALL retain a single shared frontend contract rather than duplicate declarations or unused library-store state.

The backend SHALL continue counting cumulative Cloud-synthesized characters in the current calendar month, with the existing frontend near-limit threshold of 900,000 and backend hard limit of 950,000. Quota responses SHALL retain `monthlyLimit`, `usedCharacters`, `remainingCharacters`, `isNearLimit`, and `isExhausted`. A synthesis request exceeding the hard allowance SHALL retain the existing RFC 7807 `AudioQuotaExhausted` problem contract and HTTP 429 response.

A quota guard SHALL make Cloud unavailable for selection/generation without displacing working System speech. When System is unavailable, the cascade SHALL proceed to Local. Quota exhaustion SHALL notify the user and start Local only while the current playback request still intends to play.

#### Scenario: Default engine is Google Cloud
- **WHEN** the reader has no explicit supported engine preference
- **THEN** the former Cloud-only default is replaced by Auto, and a playback request first evaluates suitable System speech.

#### Scenario: Switching to Cloud engine cancels on-device worker and hides device progress
- **WHEN** the effective engine changes from Local to Cloud
- **THEN** local synthesis is canceled, valid partials remain reusable, and local progress badges disappear.

#### Scenario: Partially synthesized chunks are preserved on engine switch
- **WHEN** local synthesis has generated 5 of 15 sentences before an engine change
- **THEN** those 5 cached sentences remain usable and a later local generation only synthesizes the missing sentences
- **AND** that generation progress does not imply a listening position at sentence 6.

#### Scenario: User toggles to On-Device engine
- **WHEN** a user explicitly chooses Local
- **THEN** the preference becomes Local and the worker is loaded only when playback is requested.

#### Scenario: Near-quota usage disables Google Cloud toggle
- **WHEN** monthly usage reaches the existing frontend near-limit threshold
- **THEN** Cloud is unavailable with a localized explanation
- **AND** working System speech remains eligible ahead of Local.

#### Scenario: Quota-exhausted API call rejected and client alerted
- **WHEN** a synthesis request would exceed the current calendar month's hard Cloud allowance
- **THEN** the backend retains HTTP 429 and the RFC 7807 `AudioQuotaExhausted` problem contract
- **AND** an active client playback request notifies the user and restarts the current slice using Local if it can initialize.

#### Scenario: Clean compilation without auto-import collisions
- **WHEN** the frontend is type-checked
- **THEN** quota types resolve without duplicate declarations or auto-import collisions.

#### Scenario: Audio quota utilization tracking and threshold enforcement
- **WHEN** quota utilization is queried
- **THEN** `monthlyLimit`, `usedCharacters`, `remainingCharacters`, `isNearLimit`, and `isExhausted` retain current-calendar-month cumulative accounting and the existing thresholds.

#### Scenario: Fallback does not become a manual preference
- **WHEN** Auto falls back to Cloud and the reader is later reopened with a usable System voice
- **THEN** Auto still applies and System is tried before Cloud.

### Requirement: Free-Tier Voice Selection and Polished Presentation

Cloud narration SHALL provide supported Neural2/WaveNet female and male choices for Vietnamese and English. Local narration SHALL have no voice picker. Voice triggers SHALL retain the compact 32px control height, rounded shape, and alignment with the Listen button.

The existing Multi-Engine Audio Narration Toolbar Layout SHALL govern presentation: its two-row arrangement SHALL remain contained on mobile and constrained desktop panes without engine-dependent jumps. Mobile voice names SHALL retain a readable width of at least 130px, never collapse, and share a contained full-width second row with timeline/status controls. Desktop controls SHALL retain the existing two-row toolbar rather than revive obsolete single-row geometry.

Frontend and backend SHALL enforce English/English and Vietnamese/Vietnamese voice compatibility. Preferences SHALL remain partitioned by language, and incompatible backend voice requests SHALL retain the `VOICE_LANGUAGE_MISMATCH` validation response.

Copy SHALL remain localized, descriptive, and free of hype labels such as "AI Audio" or "Google AI" and lightning-bolt iconography.

Engine and voice selections SHALL respect playback intent. While paused or idle, selection SHALL stop obsolete work and update preferences, voices, and available cached metadata without synthesis or autoplay. While playback is intended, an actual engine change SHALL start the selected narration at the beginning of the slice. Choosing a different voice SHALL not reuse an incompatible checkpoint. An explicit recovery action from an error state SHALL constitute a new playback request.

#### Scenario: Zero layout shift when toggling between Cloud and Device engines
- **WHEN** engine selection changes without a change of viewport or status content
- **THEN** the existing two-row card remains stable without control collisions or engine-dependent expansion.

#### Scenario: Voice selector matches Listen button height
- **WHEN** Cloud voice selection is visible
- **THEN** its trigger retains the compact 32px height of the Listen button.

#### Scenario: Switching to Cloud engine on English slice selects English voice
- **WHEN** a reader with a saved Vietnamese voice chooses Cloud for an English slice
- **THEN** an English voice is selected, never the saved Vietnamese voice.

#### Scenario: Voice preferences persist independently per language
- **WHEN** different English and Vietnamese voices are saved
- **THEN** returning to either language restores its own preference.

#### Scenario: Backend rejects voice ID conflicting with chunk language
- **WHEN** an English slice is requested with a Vietnamese Cloud voice
- **THEN** the backend retains its HTTP 400 `VOICE_LANGUAGE_MISMATCH` response.

#### Scenario: Free-tier voice selection for Vietnamese slice
- **WHEN** Cloud is selected for Vietnamese
- **THEN** the curated Vietnamese female `vi-VN-Neural2-A` and male `vi-VN-Neural2-D` choices remain available.

#### Scenario: Free-tier voice selection for English slice
- **WHEN** Cloud is selected for English
- **THEN** the curated English female `en-US-Neural2-F` and male `en-US-Neural2-D` choices remain available.

#### Scenario: UI copy and icons avoid buzzwords
- **WHEN** the toolbar is displayed
- **THEN** labels remain descriptive and no hype prefix or lightning-bolt icon is introduced.

#### Scenario: Switching engine mode while paused does not autoplay
- **WHEN** a paused user chooses another engine or voice with no cached audio
- **THEN** the player remains paused and makes no synthesis request or model download.

#### Scenario: Switching engine mode while playing transitions seamlessly
- **WHEN** an actively listening user selects another effective engine
- **THEN** the previous engine stops and the selected engine begins the current slice from the start without slice advancement.

#### Scenario: Switching to Cloud while inactive loads Cloud audio state
- **WHEN** a paused user chooses Cloud
- **THEN** available cached metadata can be presented without playback
- **AND** a cache miss remains idle until the user requests playback.

#### Scenario: Voice selector remains readable and un-truncated on mobile when idle
- **WHEN** the mobile player is idle with Cloud selected
- **THEN** the selected voice is readable rather than truncated into an ambiguous stub.

#### Scenario: Dedicated full-width scrubber row on mobile during playback
- **WHEN** narration is loaded on mobile
- **THEN** voice, timeline, timestamps, and status remain contained in the second row
- **AND** playback, engine preference, and utility controls remain accessible above it.

#### Scenario: Voice selector does not vanish during mobile playback
- **WHEN** Cloud playback starts on mobile
- **THEN** its voice selector retains usable width and remains visible.

#### Scenario: Desktop single-row layout remains unchanged
- **WHEN** the player is rendered on desktop or inside a constrained split pane
- **THEN** the existing two-row layout remains usable without horizontal clipping.

### Requirement: Cascading Audio Engine Resolution Hierarchy

For Auto playback, the reader SHALL try suitable browser System speech first, Google Cloud second, and Local last. System availability SHALL include bounded asynchronous voice discovery for the slice language. A session SHALL try each eligible engine at most once; fallback SHALL not loop, run multiple engines, persist an effective engine as a preference, or carry position across engines.

System runtime synthesis failures SHALL proceed to Cloud when eligible. Cloud unavailability, network failure, server/provider unavailability, or the existing quota guard SHALL proceed to Local. Authentication/authorization rejection and invalid-content requests SHALL remain visible errors rather than be disguised as provider outages. Pause, seek, user cancellation, source replacement, buffering, and autoplay-policy rejection SHALL NOT trigger fallback.

Explicit engine choices SHALL remain supported. An explicit System or Cloud failure SHALL use only downstream engines in the same hierarchy; explicit Local failure SHALL stop with an actionable error. Local SHALL not load before it is selected or needed, and its failure SHALL not trigger a return to an already attempted engine.

#### Scenario: Host OS has matching Vietnamese voice installed
- **WHEN** Auto playback discovers an available Vietnamese System voice
- **THEN** System speaks with no Cloud synthesis request or local-model download.

#### Scenario: Host OS lacks Vietnamese voice pack
- **WHEN** bounded voice discovery finishes without a suitable Vietnamese voice
- **THEN** eligible Cloud narration is used before Local.

#### Scenario: Cloud TTS quota exhausted or device offline
- **WHEN** System is unavailable and Cloud is blocked by quota or connectivity
- **THEN** Local is attempted without a speculative Cloud generation request.

#### Scenario: User manually selects Cloud engine
- **WHEN** a user explicitly chooses Cloud and requests playback
- **THEN** Cloud is used without a System attempt, while the manual preference remains distinct from its possible Local fallback.

#### Scenario: System TTS runtime synthesis failure cascades automatically to Cloud TTS
- **WHEN** an active System utterance fails and Cloud is eligible
- **THEN** System stops and Cloud restarts the same slice from its beginning with a localized transition notice.

#### Scenario: Voices arrive asynchronously
- **WHEN** the initial browser voice list is empty but a matching voice arrives within the discovery window
- **THEN** System is used without consuming Cloud quota.

#### Scenario: Cloud network failure reaches Local
- **WHEN** an active Cloud attempt fails due to connectivity or provider unavailability
- **THEN** Local is attempted once from the beginning of the slice and the preference is unchanged.

#### Scenario: Authentication failure remains visible
- **WHEN** a Cloud request is rejected because authentication or document authorization is invalid
- **THEN** the application preserves its existing access-error handling and does not hide the rejection behind Local fallback.

#### Scenario: User cancellation is not engine failure
- **WHEN** a user pauses, seeks, leaves, or cancels a narration attempt
- **THEN** no downstream engine starts and no slice-completion event is emitted.

### Requirement: Graceful Speech Engine Fallback & Notification

A hard System failure SHALL select the next eligible engine in the hierarchy, update the effective-engine indicator and language-appropriate voice controls, and notify the user with the actual reason for transition. If Cloud is unavailable, the UI SHALL report Local rather than claim Cloud playback. Each engine transition SHALL restart the current slice without advancing it.

System and Local narration SHALL NOT upload generated audio, partials, or character-tracking records to the backend. Only actual Cloud synthesis requests SHALL use backend audio persistence and quota tracking.

#### Scenario: Web Speech API has no compatible voice for slice language
- **WHEN** discovery finds no suitable System voice and Cloud is available
- **THEN** the effective engine and voice controls change to Cloud and a localized notice explains the fallback.

#### Scenario: No backend database persistence for client-side audio
- **WHEN** narration completes using only System or Local without a Cloud attempt
- **THEN** no audio-synthesis POST is sent and no backend audio row or character usage is created by that narration.

#### Scenario: Web Speech API synthesis failure transitions to Cloud engine
- **WHEN** System fails during playback and Cloud is available
- **THEN** the UI reflects Cloud and narration restarts at the beginning, not at a transferred System timestamp.

#### Scenario: Cloud is unavailable after System failure
- **WHEN** System fails and the next usable engine is Local
- **THEN** the UI reports Local, explains the transition, and never claims that speech will be guaranteed offline without cached resources.

## REMOVED Requirements

### Requirement: Cross-Engine Playback Progress Retention

**Reason**: Different engines and voices do not share an audio timeline; elapsed seconds or duration ratios do not establish the same spoken-content position.

**Migration**: Stop transferring seconds or percentages between engines. Use Restart Narration on Engine Change for transitions and Same-Engine Listening Position Restoration for later visits using the same identity.

## ADDED Requirements

### Requirement: Restart Narration on Engine Change

An actual engine change SHALL restart the current slice at zero and ignore saved target-engine progress. If paused, that restart reason SHALL remain pending until the next playback request and override restoration. Initial availability checks after reload SHALL not count as an engine change. Redundant selection SHALL not reset position or emit completion.

#### Scenario: Cloud switches to Local after forty-five seconds
- **WHEN** Cloud has played 45 seconds and the effective engine changes to Local
- **THEN** Local starts at the beginning of the same slice, regardless of either track's duration or saved Local progress.

#### Scenario: System fails after several sentences
- **WHEN** System fails after sentence 8 and fallback chooses Cloud
- **THEN** Cloud starts the slice from the beginning and the reader does not advance.

#### Scenario: Selecting the current engine is not a restart
- **WHEN** a user selects the already effective engine without changing the voice or source
- **THEN** the existing playback position is not reset by a redundant selection.

#### Scenario: Paused engine change overrides an older target checkpoint
- **WHEN** paused Cloud playback is changed to Local, Local has an older checkpoint, and the user later presses Listen
- **THEN** the deferred engine-change reason starts Local at zero rather than restoring the older Local checkpoint.

### Requirement: Same-Engine Listening Position Restoration

After reload or a later visit on the same browser, a playback request SHALL restore a valid checkpoint only for the same account, slice, effective engine, voice, and narration content. Resolution SHALL still honor System-first Auto rather than choose an engine merely because it has a checkpoint. No timestamp or percentage SHALL be translated to another engine.

#### Scenario: System resumes a saved sentence
- **WHEN** a matching System checkpoint records sentence 3 of 8 and the user requests playback after reload
- **THEN** System reads from the beginning of sentence 3 and continues subsequent sentences.

#### Scenario: Complete cached audio resumes at its saved second
- **WHEN** matching complete Cloud or Local audio remains cached and its checkpoint records 45 seconds
- **THEN** playback resumes at 45 seconds after its media metadata is available.

#### Scenario: Auto resolves to a different engine on a later visit
- **WHEN** a Cloud checkpoint exists but Auto now finds a suitable System voice
- **THEN** System is used from the beginning rather than restoring or translating the Cloud position.

#### Scenario: Voice or content identity changed
- **WHEN** the selected voice or narration content differs from a saved checkpoint
- **THEN** that checkpoint is not restored and the current narration begins at the start.

#### Scenario: Initial Auto resolution restores the same Cloud engine
- **WHEN** Auto is evaluated after reload, System discovery finds no suitable voice before any engine starts, and matching cached Cloud audio has a valid checkpoint
- **THEN** Cloud is the initially resolved engine and resumes its checkpoint
- **AND** the availability check is not treated as an engine transition that resets progress.

### Requirement: Partial Local Listening Position Restoration

Local generation progress and listening progress SHALL remain separate. A matching partial cache SHALL avoid re-synthesizing available sentences, while a listening checkpoint SHALL identify the actual sentence/global position heard. Restoring a valid checkpoint SHALL not play an earlier cached prefix merely because more sentences were synthesized than listened to.

#### Scenario: Five generated sentences do not mean five heard sentences
- **WHEN** 5 of 15 sentences are cached but the user stopped during sentence 2 and then reloads
- **THEN** generation reuses the 5 available sentences while playback restores sentence 2's saved position, not sentence 6.

#### Scenario: Local fragment time is not whole-slice time
- **WHEN** a checkpoint is taken 2 seconds into a later local sentence fragment
- **THEN** restoration uses that sentence and its offset in the whole narration rather than treating 2 seconds as the start of the slice.

#### Scenario: Partial generation completes before reopening
- **WHEN** a local checkpoint was taken during streaming and the matching complete cached file is available after reload
- **THEN** playback restores the checkpoint's whole-slice elapsed position in the complete file.

#### Scenario: Referenced cached audio was lost
- **WHEN** the required complete audio or referenced local partial is absent or incompatible
- **THEN** an obsolete byte-timeline checkpoint is not applied to regenerated audio and narration safely starts from the beginning.

### Requirement: Checkpoint Restoration Without Autoplay

Mounting a reader, loading a checkpoint, or changing an idle preference SHALL NOT start speech, Cloud synthesis, local inference, or a model download. The next playback request SHALL restore eligible progress unless a pending explicit restart takes precedence. Authorized auto-advance after natural completion SHALL remain a distinct continuation, not a reload side effect.

#### Scenario: Reader reload stays quiet
- **WHEN** a reader reloads with a valid checkpoint
- **THEN** no narration or synthesis begins until the user activates Listen.

#### Scenario: Completed narration can replay
- **WHEN** a saved checkpoint represents natural completion and the user later presses Listen
- **THEN** narration starts from the beginning rather than remaining stuck at the end or auto-advancing on mount.

#### Scenario: Today progress does not complete a drill
- **WHEN** audio progress is restored or narration finishes on `/today`
- **THEN** the current slice remains subject to its existing Senior Challenge workflow and audio restoration does not advance or complete it.

### Requirement: Playback Session Isolation

Only the current session and current speech/media operation SHALL control playback, fallback, completion, or checkpoints. Replaced narration, accounts, and routes SHALL invalidate the session; seek, replacement utterances, and media handoffs SHALL invalidate superseded operations within it. Delayed callbacks SHALL not resurrect either. Buffering SHALL preserve playback intent without becoming user pause or completion.

#### Scenario: Late Cloud response after engine change
- **WHEN** an old Cloud request finishes after the user selected another engine
- **THEN** it cannot replace audio, start playback, trigger fallback, or overwrite the current checkpoint.

#### Scenario: Same cache key belongs to different sessions
- **WHEN** an earlier request for the same slice and voice finishes after a newer request superseded it
- **THEN** matching content identity alone does not authorize its callbacks to control the newer session.

#### Scenario: Native pause precedes fragment ended
- **WHEN** a streaming fragment naturally emits pause before ended while more narration is pending
- **THEN** the session retains its intended continuation, waits or advances to available audio, and does not mark the slice complete.

#### Scenario: Leaving or changing accounts fences progress
- **WHEN** the reader is left or the active account changes with asynchronous work pending
- **THEN** obsolete work cannot start playback or commit a checkpoint under the replacement account.

#### Scenario: Superseded utterance finishes after seeking
- **WHEN** System seeking replaces an utterance and an old utterance later emits start, end, or error
- **THEN** the old operation cannot change the cursor, speak another sentence, trigger fallback, or complete the slice
- **AND** the current session retains its existing attempted-engine history.

#### Scenario: Old fragment callback arrives after full-file handoff
- **WHEN** Local replaces a sentence fragment with the complete audio and an obsolete fragment callback arrives
- **THEN** it cannot apply stale metadata, change the new position, or emit false slice completion.
