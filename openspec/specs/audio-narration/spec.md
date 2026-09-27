# audio-narration Specification

## Purpose
Synthesizes a reader slice's AI-formatted text into speech entirely on the user's device (in-browser neural TTS), with the voice chosen automatically by slice language, and plays it back inside `/read/[bookId]` — caching the complete slice audio locally so playback is fully seekable, free, offline, and never re-synthesized on seek or replay.

## Requirements

### Requirement: On-Device Slice Narration Synthesis

The reader SHALL synthesize narration audio for a slice **entirely on the client**, using an on-device neural text-to-speech model run in a Web Worker, without any server request for synthesis, external API credential, or usage quota. All synthesis code SHALL be client-only and MUST NOT execute during server-side rendering.

Narration SHALL be available only for slices that are AI-formatted (`isAiFormatted == true`); for a non-formatted slice the reader SHALL NOT synthesize audio. Before synthesis, the reader SHALL derive a plain-text narration script from the slice's formatted markdown by removing markup and **excluding fenced code blocks** (so source code is not read aloud), preserving headings and prose.

The synthesis voice SHALL be determined automatically by the slice's content language (`Language`, `en` or `vi`); there is one on-device voice per supported language and no user-facing voice selection. When the slice does not itself carry a language, the reader SHALL fall back to the book/document language, and only then to the default `en`. The reader SHALL NOT narrate a slice whose known language is non-English using the English voice; a known `vi` language MUST select the vi voice.

The narration model for a language SHALL be downloaded lazily on first use and cached in the browser so it is fetched once and reused offline on subsequent uses. While a model is downloading or audio is being synthesized, the reader SHALL surface a localized progress/loading state.

#### Scenario: Synthesis runs on-device without a server request
- **WHEN** a user plays narration for an AI-formatted slice
- **THEN** the reader synthesizes the audio locally via the on-device model worker and issues no server request for text-to-speech.

#### Scenario: Narration gated on AI formatting
- **WHEN** the current slice is not AI-formatted (`isAiFormatted == false`)
- **THEN** the reader does not synthesize audio and the playback control is hidden or disabled.

#### Scenario: Code fences excluded from narration
- **WHEN** the slice's formatted content contains fenced code blocks
- **THEN** the derived narration script omits the code block contents while retaining the surrounding prose and headings.

#### Scenario: Voice selected automatically by content language
- **WHEN** a user plays a Vietnamese slice (`Language == "vi"`)
- **THEN** the reader synthesizes with the on-device vi voice without prompting for a voice choice.

#### Scenario: Known non-English language is not narrated with the English voice
- **WHEN** the slice or its book reports language `vi` and the user plays narration
- **THEN** the reader selects the vi voice and does not fall back to the English voice.

#### Scenario: Voice model downloaded once and cached for offline reuse
- **WHEN** a user plays narration for a language whose model is not yet cached, and later plays narration again in that language
- **THEN** the first play downloads and caches the model with a visible progress state, and the later play reuses the cached model without re-downloading, including offline.

### Requirement: Seekable Full-Slice Audio Cache

The reader SHALL synthesize a slice's narration sentence by sentence, surfacing synthesis progress (for example, sentences completed of the total). When on-device synthesis begins for a multi-sentence slice, the reader SHALL buffer an initial pre-roll threshold of sentences (at least 33% of the slice's total sentences, or 100% of sentences for slices with 3 or fewer sentences) before starting audible playback, preventing audio playback from exhausting its buffer and stalling when earlier sentences are shorter than the computation time of subsequent sentences.

Once this pre-roll threshold is satisfied, audio playback SHALL begin immediately with the assembled initial sentences, while the worker continues synthesizing the remaining sentences in the background. As subsequent sentences finish synthesis, they SHALL be queued seamlessly for uninterrupted continuous playback without audio underruns or restarting from the beginning.

Upon completing synthesis of all sentences in a slice, the reader SHALL assemble the sentences into a **single complete audio object** and SHALL transition to using that complete audio as the **sole** playback source, so that playback of the whole slice is continuous and the user can seek to any position within the slice **without re-synthesizing**. The reader SHALL NOT leave playback stalled on a partial fragment, and the reported total duration SHALL reflect the complete slice audio once assembled.

The reader SHALL persist the complete slice audio in the browser's on-device storage (IndexedDB), keyed by `(chunkId, voice, contentHash)`, where `voice` is the language's on-device voice and `contentHash` is derived from the normalized narration script. A subsequent request to narrate the same `(chunkId, voice, contentHash)` — including in a later session — SHALL load the cached audio and SHALL NOT re-synthesize. When a slice's formatted content changes so its `contentHash` differs, the stale cache entry SHALL NOT be used and the audio SHALL be re-synthesized once. The audio cache SHALL enforce a bounded size (an entry or total-size cap) and evict least-recently-used entries; an evicted slice re-synthesizes on next play.

#### Scenario: Pre-roll buffer threshold reached before playback begins
- **WHEN** a user plays on-device narration for a slice with 19 sentences
- **THEN** the reader buffers at least 7 sentences (33%) before initiating playback, ensuring sufficient playback runway while remaining sentences synthesize.

#### Scenario: Short slices buffer fully before playback
- **WHEN** a user plays on-device narration for a slice with 3 or fewer sentences
- **THEN** the reader buffers all sentences before playback begins.

#### Scenario: Continuous playback across chunk boundaries without stutter
- **WHEN** playback reaches the end of the initial buffered sentences while background synthesis continues
- **THEN** subsequent sentence audio chunks play sequentially without gaps, audio dropouts, or resetting back to the start of the audio.
#### Scenario: Whole slice plays continuously
- **WHEN** a slice's narration has finished synthesizing and the user starts playback
- **THEN** playback proceeds continuously through the entire slice and does not stop after the first sentence.

#### Scenario: Reported duration reflects the complete slice
- **WHEN** a slice's audio has finished assembling
- **THEN** the control's total duration equals the full assembled audio's duration, not the duration of an intermediate single-sentence fragment.

#### Scenario: Seeking within a slice does not re-synthesize
- **WHEN** a slice's audio has finished synthesizing and the user seeks backward or forward within it
- **THEN** playback jumps to the new position using the already-assembled complete audio without invoking the synthesis worker.

#### Scenario: Replaying a slice loads cached audio without re-synthesis
- **WHEN** a user re-opens a slice whose `(chunkId, voice, contentHash)` audio is present in IndexedDB
- **THEN** the reader loads the cached audio and begins playback without running the synthesis worker.

#### Scenario: Re-formatted slice invalidates cached audio
- **WHEN** a slice's formatted content changes so its `contentHash` differs from the cached entry, and the user plays narration
- **THEN** the stale entry is not used and the reader re-synthesizes and caches audio under the new `contentHash`.

#### Scenario: Cache eviction bounds device storage
- **WHEN** cached audio exceeds the configured cap
- **THEN** the reader evicts least-recently-used slice audio, and a subsequently re-opened evicted slice re-synthesizes once and is re-cached.

### Requirement: Reader Audio Playback Controls

The `/read/[bookId]` reader SHALL provide an audio playback control that lets the user listen to the current slice's narration, available only when the current slice is AI-formatted (hidden or disabled otherwise). Playback SHALL use an HTML5 `<audio>` element supporting play, pause, and position seeking over the assembled complete slice audio.

The reader SHALL offer a **playback speed** control spanning 0.5x to 2.0x applied client-side to the `<audio>` element's `playbackRate` **without re-synthesizing audio**; the selected speed SHALL persist across sessions and slices. The reader SHALL NOT present a voice picker, since the voice is chosen automatically by slice language.

While the language model is downloading, the reader SHALL display the download progress as an accurate whole-number percentage in the inclusive range **0 to 100**; it SHALL NOT display a value greater than 100% or otherwise mis-scaled. While synthesizing after download, the reader SHALL display a synthesis progress state.

All audio control labels, speed labels, and download/synthesis progress messages SHALL render localized text (en/vi) from the i18n catalog, and the controls SHALL follow the reader's responsive typography and `whitespace-nowrap shrink-0` layout invariants across both locales. The reader SHALL NOT use native browser dialogs for audio state; status is conveyed via in-page controls and `useToast()`.

#### Scenario: User plays the current slice narration
- **WHEN** a user viewing an AI-formatted slice activates the play control
- **THEN** the reader synthesizes (or loads cached) audio for the slice's language voice, loads it into the `<audio>` element, and begins playback with seek support.

#### Scenario: Download progress is shown as a value between 0 and 100 percent
- **WHEN** the narration model is downloading and the reader shows the download percentage
- **THEN** the displayed value is a whole number between 0 and 100 inclusive (for example `42%`), never a mis-scaled value such as `10000%`.

#### Scenario: User changes playback speed during playback
- **WHEN** a user sets the playback speed to 1.5x while audio is playing
- **THEN** the `<audio>` element's `playbackRate` updates to 1.5 immediately without re-synthesizing, and the speed preference is persisted.

#### Scenario: Speed preference persists across sessions
- **WHEN** a user who previously selected 1.25x returns to the reader in a later session
- **THEN** the reader restores 1.25x playback speed as the active preference.

### Requirement: GPU-Accelerated On-Device Synthesis with CPU Fallback

The reader SHALL run on-device narration synthesis on the GPU when the browser exposes GPU compute to the synthesis worker, and SHALL fall back to CPU execution when GPU compute is unavailable or fails to initialize, so synthesis completes on any supported browser. The choice of execution backend SHALL NOT change the produced audio or any user-facing control. A backend-initialization failure SHALL NOT surface as a narration error while a working fallback exists, and the backend selection SHALL NOT be re-probed or re-failed per sentence within a synthesis run.

When the browser exposes a choice of GPU adapters, the worker SHALL request a high-performance adapter so a discrete GPU is preferred over integrated graphics. The worker SHALL report the resolved execution device to the reader, and the reader SHALL surface a localized indication of the active compute device (GPU or CPU) within the audio control, so a user on a GPU-less browser understands that synthesis is running on the CPU rather than having to infer it from a raw browser console message. This indication SHALL be production-appropriate — applicable to all deployments, not a local-development banner or workaround — and SHALL follow the reader's responsive-typography and `whitespace-nowrap shrink-0` layout invariants in both the English and Vietnamese locales.

#### Scenario: GPU backend used when available
- **WHEN** GPU compute is available to the synthesis worker and a user plays narration
- **THEN** synthesis runs on the GPU backend and produces playable audio.

#### Scenario: Falls back to CPU when GPU is unavailable
- **WHEN** GPU compute is unavailable or its initialization fails
- **THEN** synthesis falls back to CPU, still completes successfully, and produces the same audio without surfacing a narration error.

#### Scenario: Backend chosen once per model
- **WHEN** the execution backend has been selected for a language's model
- **THEN** subsequent sentences in the run reuse that backend without re-probing or repeating a failed initialization.

#### Scenario: High-performance adapter preferred when a choice exists
- **WHEN** the browser exposes both an integrated and a discrete GPU adapter to the worker
- **THEN** the worker requests the high-performance adapter so the discrete GPU is used for synthesis.

#### Scenario: Active compute device shown in the reader
- **WHEN** synthesis resolves to CPU because the browser exposes no GPU adapter, and the user opens the audio control
- **THEN** the reader displays a localized indication that narration is running on the CPU, in place of relying on the browser console notice.

### Requirement: Multi-Threaded Synthesis Enabled on Reader Routes Only

On reader routes, the application SHALL be cross-origin isolated so on-device synthesis can use multi-threaded execution. This isolation SHALL be established across deployment tiers without duplicate or conflicting response headers. Response headers `Cross-Origin-Opener-Policy: same-origin` and `Cross-Origin-Embedder-Policy: credentialless` SHALL be emitted cleanly without repetition. This isolation SHALL be confined to reader routes and SHALL NOT be applied to the authentication route or other routes, so cross-origin sign-in — which depends on cross-window communication — continues to function. Cross-origin resources the reader legitimately needs (web fonts, document images) SHALL continue to load under the isolation policy.

When entering a reader route from an unisolated browsing context (such as post-login or client-side navigation from library routes), the client application SHALL ensure the browsing context acquires cross-origin isolation (executing a full document navigation if `self.crossOriginIsolated` is not yet active), so that `self.crossOriginIsolated === true` reliably holds on reader views.

When cross-origin isolation is active (`self.crossOriginIsolated === true`), the synthesis worker SHALL configure the CPU (WASM) execution backend to run across multiple threads, scaling the thread count to the machine's available logical cores (`navigator.hardwareConcurrency`), so that a many-core CPU without a usable GPU is fully utilized for synthesis rather than running on a single thread. The configured thread count SHALL be bounded by the reported hardware concurrency to avoid oversubscription. When cross-origin isolation or multi-threaded execution is unavailable, the worker SHALL clamp thread allocation to 1 without emitting console errors; thread configuration SHALL NOT change the produced audio.

#### Scenario: Clean non-duplicate cross-origin headers delivered
- **WHEN** a client requests a `/read/**` document
- **THEN** the HTTP response contains single instances of `Cross-Origin-Opener-Policy: same-origin` and `Cross-Origin-Embedder-Policy: credentialless` without duplicate header values.

#### Scenario: Navigation into reader ensures active cross-origin isolation
- **WHEN** a user navigates to `/read/[bookId]` from an unisolated route
- **THEN** the reader browsing context establishes `window.crossOriginIsolated === true`.
#### Scenario: Sign-in route is not isolated and OAuth still works
- **WHEN** the sign-in route is loaded and the user completes cross-origin sign-in
- **THEN** the page is not cross-origin isolated and sign-in completes successfully.

#### Scenario: Reader cross-origin assets still load
- **WHEN** a reader slice references cross-origin fonts or images
- **THEN** those resources still load and render under the reader's isolation policy.

#### Scenario: Many-core CPU uses multiple synthesis threads
- **WHEN** a reader route is cross-origin isolated on a machine with no usable GPU and multiple logical cores, and a user plays narration
- **THEN** the CPU synthesis backend runs across multiple threads scaled to the available cores rather than on a single thread.

#### Scenario: Single-threaded fallback still completes
- **WHEN** cross-origin isolation or multi-threaded execution is unavailable
- **THEN** synthesis still completes correctly on a single thread and produces the same audio.

### Requirement: Narration Resource Lifecycle on Reader Exit

When the user leaves the reader view — navigating to another route or otherwise unmounting it — the reader SHALL stop narration playback, cancel any in-progress synthesis, and release the synthesis worker and loaded model, so no audio continues without visible controls and no worker or model accumulates across reader visits. A synthesis that resolves after the reader has been left SHALL NOT start playback.

#### Scenario: Leaving the reader stops playback and releases the worker
- **WHEN** narration is playing and the user navigates away from the reader
- **THEN** playback stops and the synthesis worker is terminated.

#### Scenario: In-progress synthesis is cancelled on exit
- **WHEN** synthesis is still in progress and the user navigates away from the reader
- **THEN** the in-progress synthesis is cancelled and its later completion does not start playback.

#### Scenario: No per-visit resource leak
- **WHEN** the user opens and leaves the reader repeatedly
- **THEN** synthesis workers and loaded models are not accumulated across visits.

### Requirement: Backend-Appropriate Weight Precision

The synthesis worker SHALL load the narration model at the weight precision that minimizes synthesis latency on the selected execution backend, rather than the framework's default. On the CPU (WASM) backend it SHALL use full precision (fp32): the framework's default WASM weights are quantized (int8), which run materially slower on this model because ONNX Runtime Web has no fast int8 kernels, so full precision is faster on the CPU path. On the GPU (WebGPU) backend it SHALL use half precision (fp16). If the preferred precision fails to build for a backend, the worker SHALL fall back to full precision without surfacing a narration error. The choice of weight precision SHALL NOT change any user-facing control, the audio cache key `(chunkId, voice, contentHash)`, or the guarantee that a language's model is downloaded once and reused offline; the produced narration SHALL remain intelligible speech in the slice's language.

#### Scenario: CPU path uses full precision for lowest latency
- **WHEN** a user plays narration and synthesis runs on the CPU (WASM) backend
- **THEN** the worker loads full-precision (fp32) weights rather than the framework's default quantized weights, and produces playable narration.

#### Scenario: GPU path uses half precision
- **WHEN** a user plays narration and synthesis runs on the WebGPU backend
- **THEN** the worker loads half-precision (fp16) weights and produces playable narration.

#### Scenario: Full-precision fallback on preferred-build failure
- **WHEN** the preferred precision for the selected backend fails to build
- **THEN** the worker falls back to full-precision weights, completes synthesis, and surfaces no narration error.

#### Scenario: Precision does not alter cache or controls
- **WHEN** narration is synthesized and later replayed
- **THEN** the cached audio is reused under the same `(chunkId, voice, contentHash)` key and no additional or changed playback control appears.

### Requirement: Google Cloud Narration Synthesis with Database Caching

The system SHALL support server-side audio narration synthesis via Google Cloud Text-to-Speech (Cloud TTS) as the primary high-speed narration engine. All calls to Google Cloud TTS SHALL be proxied through the ASP.NET Core backend using a server-side API key (`Google__TtsApiKey`); client applications SHALL NOT be exposed to external API credentials.

Client applications requesting audio synthesis or quota status SHALL resolve backend API routes using canonical relative URLs in production environments (matching the reverse proxy configuration) and MUST NOT fall back to hardcoded localhost addresses when environment configuration strings evaluate to empty values.
The backend SHALL persist complete synthesized audio files in PostgreSQL (`DocumentChunkAudios`) keyed by `(DocumentChunkId, ContentHash, VoiceId)`. When a request for a slice's narration arrives:
1. If an audio record matching `(DocumentChunkId, ContentHash, VoiceId)` exists in the database, the backend SHALL return the cached audio immediately without calling the Google Cloud TTS API.
2. If no record exists, the backend SHALL synthesize the audio via Google Cloud TTS:
   - If the narration text exceeds 4,500 bytes UTF-8 (to ensure safe compliance with Google Cloud TTS's single-request limit of 5,000 bytes), the backend SHALL automatically partition the text into sentence-bounded sub-chunks under 4,500 bytes.
   - The backend SHALL synthesize each sub-chunk in order and concatenate the resulting MP3 audio byte streams into a single seamless MP3 audio stream.
3. The backend SHALL store the complete synthesized MP3 audio and total character count in PostgreSQL (`DocumentChunkAudios`) and return the audio stream to the client.
4. Upon receiving the audio from the server, the reader client SHALL store it in browser IndexedDB under the same `(chunkId, voice, contentHash)` cache key so subsequent seeks, pauses, and replays on that browser are served locally with zero network latency.

When a slice's content changes such that its `ContentHash` changes, the existing database audio record SHALL be invalidated and new audio synthesized on next play.

#### Scenario: Database cache hit serves audio without calling Google Cloud API
- **WHEN** a reader requests narration for a slice whose `(DocumentChunkId, ContentHash, VoiceId)` audio is already cached in PostgreSQL
- **THEN** the server returns the cached audio in less than 50ms and makes zero requests to the Google Cloud TTS API.

#### Scenario: Long text exceeding 5,000 bytes is automatically partitioned and concatenated
- **WHEN** a reader requests narration for a slice whose plain-text script exceeds 4,500 UTF-8 bytes (such as a lengthy Vietnamese or English chapter)
- **THEN** the server partitions the text along sentence boundaries into sub-chunks strictly under 4,500 bytes, synthesizes each sub-chunk without encountering Google Cloud 400 Bad Request errors, concatenates the resulting MP3 streams, and returns a single unified audio payload.

#### Scenario: Database cache miss synthesizes, stores in PostgreSQL, and streams audio
- **WHEN** a reader requests narration for a slice not yet cached in PostgreSQL
- **THEN** the server synthesizes audio via Google Cloud TTS, writes the MP3 audio bytes to PostgreSQL, and returns the audio to the client.

#### Scenario: Client stores server-synthesized audio in IndexedDB
- **WHEN** client receives audio synthesized by Google Cloud TTS from the server
- **THEN** client stores the audio in browser IndexedDB so repeat listens in the session do not issue repeated HTTP requests.

#### Scenario: Slice content revision invalidates server cache
- **WHEN** a slice's markdown is re-formatted or edited, altering its `ContentHash`
- **THEN** the server does not serve stale audio from the prior hash and re-synthesizes audio under the new `ContentHash`.

#### Scenario: Production synthesis requests resolve to relative API endpoint without localhost fallback
- **WHEN** a user initiates Google Cloud narration on a production deployment where `NUXT_PUBLIC_API_BASE_URL` is configured as an empty string
- **THEN** the audio composable resolves the endpoint against relative origin (`/api/v1/library/chunks/.../audio`)
- **AND** zero connection refused errors or mixed-content protocol violations occur.

### Requirement: Dual-Engine Audio Toggle and Server Quota Guard

The `/read/[bookId]` reader audio player SHALL provide an in-player toggle switch allowing the user to select between **Google Cloud** (cloud-accelerated) and **On-Device** (local Web Worker) synthesis engines. The default engine SHALL be Google Cloud, and the user's preference SHALL persist in `localStorage`.

The frontend client applications SHALL consume audio narration quota models (`AudioQuotaInfo`) from a centralized TypeScript type definition without duplicating exported interface declarations across composable functions or Pinia store modules, preventing auto-import collision and symbol shadowing during compilation.

The backend SHALL track cumulative characters synthesized via Google Cloud TTS during the current calendar month. To protect the free-tier monthly allowance (hard-capped at 950,000 characters to ensure a safe buffer below Google's 1,000,000 allowance):
1. The backend SHALL expose current monthly quota utilization.
2. When monthly quota consumption reaches or exceeds the threshold (900,000 characters), the reader frontend SHALL disable the Google Cloud toggle switch with an informative tooltip and automatically select the On-Device engine.
3. If an API request to synthesize via Google Cloud is received when the monthly quota is exhausted (reaches 950,000 characters, such as via direct API call or client DOM tampering), the backend SHALL reject the request with an RFC 7807 problem details response (HTTP 429 Too Many Requests or 403 Forbidden with code `AudioQuotaExhausted`).
4. Upon receiving a quota exhaustion response, the client SHALL display a localized error toast notifying the user that the monthly Google Cloud quota has been reached, and automatically switch the player to the On-Device engine.

#### Scenario: Default engine is Google Cloud
- **WHEN** a reader opens an AI-formatted slice for the first time without a saved preference
- **THEN** the audio player selects Google Cloud as the active engine.

#### Scenario: User toggles to On-Device engine
- **WHEN** a user switches the audio toggle from Google Cloud to On-Device
- **THEN** the player switches to the local Web Worker engine, synthesizes on CPU/GPU, and caches in browser IndexedDB.

#### Scenario: Near-quota usage disables Google Cloud toggle
- **WHEN** the server reports monthly character quota at or above 900,000 characters
- **THEN** the Google Cloud toggle in the reader is disabled with an explanatory tooltip and narration defaults to On-Device mode.

#### Scenario: Quota-exhausted API call rejected and client alerted
- **WHEN** a client submits a synthesis request to the backend after the monthly quota is exhausted
- **THEN** the backend responds with an RFC 7807 `AudioQuotaExhausted` error, and the client displays a toast notification and reverts to the On-Device engine.

#### Scenario: Clean compilation without auto-import collisions
- **WHEN** the frontend application is built or type-checked via `npm test` or `npx nuxi typecheck`
- **THEN** the compiler completes without emitting duplicate auto-import collision warnings for `AudioQuotaInfo`.

#### Scenario: Audio quota utilization tracking and threshold enforcement
- **WHEN** a reader queries the monthly audio quota utilization
- **THEN** the server returns the current monthly character usage, remaining characters, and limit flags (`isNearLimit`, `isExhausted`) according to the canonical contract.

### Requirement: Free-Tier Voice Selection and Polished Presentation

When the Google Cloud engine is active, the reader audio player SHALL provide a voice selector restricted to Google Cloud Free-Tier voices (Neural2 and WaveNet tiers) for the slice's content language, supporting at least one female and one male voice for Vietnamese (`vi-VN`) and English (`en-US`). Voice selection SHALL NOT be shown when the On-Device engine is active.

The voice selection dropdown trigger SHALL match the compact height, vertical padding, and border radius of the adjacent "Listen" action button (`py-1.5`, ~32px rendered height), ensuring that switching between On-Device and Google Cloud narration engines does not vertically expand or distort the height of the reader audio player bar.

The system (both frontend client and backend synthesis handler) SHALL strictly enforce language compatibility between the document slice's content language and the selected voice model:
1. Slices in English (`en`) SHALL only be synthesized with English voice models (`en-US-*`).
2. Slices in Vietnamese (`vi`) SHALL only be synthesized with Vietnamese voice models (`vi-VN-*`).
3. The system SHALL NOT synthesize English documentation with a Vietnamese voice model or Vietnamese documentation with an English voice model, even if a user previously selected a voice in another language.
4. Voice preference persistence in client storage SHALL be partitioned by language (e.g. separate storage keys for `en` and `vi`), ensuring that selecting a preferred voice in one language does not overwrite or corrupt the voice selection when reading documents in another language.
5. If the client submits a voice ID that does not match the slice's content language, the backend SHALL reject the request with an RFC 7807 validation error (`VOICE_LANGUAGE_MISMATCH`).

All audio controls, buttons, tooltips, and status indicators SHALL use clean, professional, descriptive copy and SHALL NOT use hype or buzzword labels such as "AI", "AI Audio", or "Google AI". The UI SHALL use standard, subtle iconography (e.g. cloud icon or plain toggle switch) and SHALL NOT use mismatched or aggressive icons such as lightning bolts (`Zap`).

#### Scenario: Voice selector matches Listen button height
- **WHEN** the user switches between On-Device and Google Cloud engine modes in the reader audio player
- **THEN** the voice selector trigger button height matches the height of the "Listen" button (`~32px`), preventing the player bar container from expanding or changing height vertically.

#### Scenario: Switching to Cloud engine on English slice selects English voice
- **WHEN** a user who previously selected a Vietnamese voice (`vi-VN-Neural2-A`) on a Vietnamese book switches to Cloud mode on an English book slice
- **THEN** the player resolves and uses an English voice (`en-US-Neural2-F`), never the persisted Vietnamese voice.

#### Scenario: Voice preferences persist independently per language
- **WHEN** a user selects a male voice (`en-US-Neural2-D`) for English documents and later selects a female voice (`vi-VN-Neural2-A`) for Vietnamese documents
- **THEN** returning to an English document restores the male English voice (`en-US-Neural2-D`), while opening a Vietnamese document uses the female Vietnamese voice (`vi-VN-Neural2-A`).

#### Scenario: Backend rejects voice ID conflicting with chunk language
- **WHEN** an API request arrives to synthesize an English document chunk with a `vi-VN-*` voice ID
- **THEN** the backend responds with HTTP 400 Bad Request and error code `VOICE_LANGUAGE_MISMATCH`.
#### Scenario: Free-tier voice selection for Vietnamese slice
- **WHEN** a user views a Vietnamese slice with Google Cloud engine active
- **THEN** the player provides a choice between curated Vietnamese Neural2 female (`vi-VN-Neural2-A`) and male (`vi-VN-Neural2-D`) voices.

#### Scenario: Free-tier voice selection for English slice
- **WHEN** a user views an English slice with Google Cloud engine active
- **THEN** the player provides a choice between curated English Neural2 female (`en-US-Neural2-F`) and male (`en-US-Neural2-D`) voices.

#### Scenario: UI copy and icons avoid buzzwords
- **WHEN** viewing the audio narration player in any state
- **THEN** the controls render clean labels ("Google Cloud", "Thiết bị" / "Device") without "AI" prefixes, and no lightning bolt icons are rendered in the audio control.
