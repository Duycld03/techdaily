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

The reader SHALL synthesize a slice's narration sentence by sentence, surfacing synthesis progress (for example, sentences completed of the total). When on-device synthesis begins, the reader SHALL enforce a low-latency pre-roll threshold: for slices with 2 or fewer sentences, the reader SHALL buffer all sentences (`sentences.length`), and for slices with more than 2 sentences, the reader SHALL cap the pre-roll buffer target at a maximum of 2 sentences (`Math.min(2, sentences.length)`), initiating audible playback within seconds while continuing background streaming synthesis for subsequent sentences.

Once this pre-roll threshold is satisfied, audio playback SHALL begin immediately with the assembled initial sentences, while the worker continues synthesizing the remaining sentences in the background. As subsequent sentences finish synthesis, they SHALL be queued seamlessly for uninterrupted continuous playback without audio underruns or restarting from the beginning.

While synthesizing on-device sentences, the reader SHALL persist intermediate generated sentence chunks in the browser's on-device storage (IndexedDB) keyed by `(chunkId, voice, contentHash)`. If on-device synthesis is interrupted (such as by pausing, navigating away, or switching to the Cloud engine) and later re-initiated:
1. The reader SHALL inspect IndexedDB for existing partial chunks matching `(chunkId, voice, contentHash)`.
2. If matching partial chunks are found and the slice's `contentHash` matches, the reader SHALL restore the already-synthesized chunks ($0 \dots K-1$), update the synthesis progress indicator to reflect $K/N$, immediately start pre-roll playback if $K \ge \text{targetBufferCount}$, and dispatch synthesis to the worker for only the remaining ungenerated sentences ($K \dots N-1$), eliminating redundant computation.
3. If the slice's content has changed such that its `contentHash` differs, the stale partial cache entries SHALL NOT be used, any stale partial entries SHALL be deleted, and synthesis SHALL restart from sentence 0.

Upon completing synthesis of all sentences in a slice, the reader SHALL assemble the sentences into a **single complete audio object** and SHALL transition to using that complete audio as the **sole** playback source, so that playback of the whole slice is continuous and the user can seek to any position within the slice **without re-synthesizing**. The reader SHALL preserve the current playback offset using the media element's `loadedmetadata` event. If the user was paused when background synthesis completed, the player SHALL set the playback offset on the complete audio file and remain paused without resetting or discarding the paused position. When the user pauses on-device narration and subsequently resumes playback, audio playback SHALL continue seamlessly from the exact paused second without stalling or remaining frozen at an earlier timestamp. If playback is initiated on an audio source that has reached its end (`ended` is true or `currentTime >= duration`), the reader SHALL reset the playback position to 0 and begin playback from the start.

When on-device synthesis is initiated on a slice with existing partial progress ($K$ of $N$ sentences, where $K \ge 1$ and $K < N$):
1. The engine SHALL concatenate all $K$ available sentence chunks into the initial playable audio segment, enabling immediate playback of all $K$ sentences without re-synthesizing.
2. The synthesis progress indicator SHALL immediately display $K/N$.
3. The Web Worker SHALL be dispatched to synthesize only sentences $K \dots N-1$.
4. As subsequent sentences $K, K+1, \dots$ finish synthesis, they SHALL be chained seamlessly for uninterrupted playback.

While background synthesis is active (`isStreaming === true`), if audio playback reaches the end of the currently buffered chunks before the next chunk has finished synthesis, the player SHALL NOT terminate playback, SHALL NOT set `playing = false`, and SHALL NOT freeze the time display. The player SHALL transition to a buffering state (`status = 'loading'`) and automatically transition to playing the next sentence chunk as soon as it arrives from the Web Worker.

When a user clicks "Listen" on a slice whose on-device synthesis has not completed (`synthIndex < synthTotal` or when only partial pre-roll audio is loaded), the reader SHALL resume worker synthesis for all remaining ungenerated sentences ($K \dots N-1$) rather than merely looping the partial pre-roll buffer. Pausing playback SHALL NOT discard background synthesis progress (`synthIndex`, `synthTotal`), enabling the worker to continue or cleanly resume generation.
The reader SHALL persist the complete slice audio in the browser's on-device storage (IndexedDB), keyed by `(chunkId, voice, contentHash)`, where `voice` is the language's on-device voice and `contentHash` is derived from the normalized narration script. A subsequent request to narrate the same `(chunkId, voice, contentHash)` — including in a later session — SHALL load the cached audio and SHALL NOT re-synthesize. When a slice's formatted content changes so its `contentHash` differs, the stale cache entry SHALL NOT be used and the audio SHALL be re-synthesized once. The audio cache SHALL enforce a bounded size (an entry or total-size cap) and evict least-recently-used entries; an evicted slice re-synthesizes on next play.
#### Scenario: Resuming interrupted on-device synthesis from partial cache
- **WHEN** on-device synthesis was previously interrupted after synthesizing 5 of 15 sentences, and the user re-initiates on-device narration for the identical slice
- **THEN** the reader loads the 5 cached sentence chunks from IndexedDB, sets synthesis progress to 5/15, immediately starts playback if pre-roll threshold is satisfied, and requests the worker to synthesize only sentences 6 through 15.

#### Scenario: Modified slice invalidates stale partial chunks
- **WHEN** a slice with partial cached chunks (e.g. 5 of 15 sentences) has its content revised such that its `contentHash` changes, and the user plays on-device narration
- **THEN** the reader detects the hash mismatch, discards the stale partial chunks, and synthesizes all sentences starting from index 0 under the new `contentHash`.
#### Scenario: Long slice begins playback after 2 sentences
- **WHEN** a user initiates on-device narration on a 36-sentence reading slice
- **THEN** the reader audio engine sets the pre-roll buffer target to exactly 2 sentences and starts audio playback as soon as sentence 2 completes, rather than delaying playback until 12 or more sentences are synthesized.

#### Scenario: Short slice buffers all sentences
- **WHEN** a user initiates on-device narration on a 2-sentence reading slice
- **THEN** the reader audio engine sets the pre-roll buffer target to 2 sentences and starts playback once all 2 sentences are synthesized.
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

#### Scenario: Resuming local TTS playback after pause
- **WHEN** a user pauses on-device TTS narration mid-sentence (e.g. at 0:15) while synthesis finishes in the background, and later clicks "Listen" to resume
- **THEN** audio playback resumes playing from 0:15 and continues through the remainder of the slice without freezing.

#### Scenario: Playing completed audio restarts from beginning
- **WHEN** a user clicks "Listen" on a slice whose playback has already reached the end
- **THEN** the audio restarts playing from 0:00 rather than remaining frozen at the end duration.

#### Scenario: Resuming on-device synthesis from 5/15 partial cache on page refresh
- **WHEN** a user refreshes the page on a 15-sentence slice where 5 sentences were previously synthesized and cached in IndexedDB, and initiates on-device narration
- **THEN** all 5 sentences are assembled into the initial playable audio, synthesis progress indicates 5/15, playback of the 5 sentences is immediately available, and the Web Worker synthesizes only sentences 5 through 14.

#### Scenario: Audio catches up to worker without freezing
- **WHEN** audio playback reaches the end of the initial buffered sentences while the next sentence is still being synthesized by the Web Worker
- **THEN** playback enters a buffering state instead of stopping, and as soon as the next sentence chunk arrives, playback automatically continues.

#### Scenario: Replaying an incomplete slice resumes synthesis
- **WHEN** on-device playback is sitting paused at the end of an incomplete pre-roll and the user clicks "Listen"
- **THEN** the reader initiates synthesis of the remaining sentences while playing, updating the progress indicator until all sentences are complete.

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

The system SHALL enforce strict isolation between the two engines:
1. When switching from On-Device to Google Cloud, or when Google Cloud narration playback begins, the reader SHALL immediately cancel any active on-device Web Worker synthesis tasks, ceasing worker inference and preventing further chunk emissions.
2. Any on-device sentence chunks completed prior to cancellation SHALL be preserved in the partial cache in IndexedDB under the matching `contentHash`, allowing later resumption if the user switches back.
3. The reader audio player SHALL display on-device synthesis progress indicators (`current/total`) strictly when the On-Device engine is active. The player SHALL NOT render on-device synthesis badges or ghost loading labels when Google Cloud audio is active or playing.

The frontend client applications SHALL consume audio narration quota models (`AudioQuotaInfo`) from a centralized TypeScript type definition without duplicating exported interface declarations across composable functions or Pinia store modules, preventing auto-import collision and symbol shadowing during compilation.

The backend SHALL track cumulative characters synthesized via Google Cloud TTS during the current calendar month. To protect the free-tier monthly allowance (hard-capped at 950,000 characters to ensure a safe buffer below Google's 1,000,000 allowance):
1. The backend SHALL expose current monthly quota utilization.
2. When monthly quota consumption reaches or exceeds the threshold (900,000 characters), the reader frontend SHALL disable the Google Cloud toggle switch with an informative tooltip and automatically select the On-Device engine.
3. If an API request to synthesize via Google Cloud is received when the monthly quota is exhausted (reaches 950,000 characters, such as via direct API call or client DOM tampering), the backend SHALL reject the request with an RFC 7807 problem details response (HTTP 429 Too Many Requests or 403 Forbidden with code `AudioQuotaExhausted`).
4. Upon receiving a quota exhaustion response, the client SHALL display a localized error toast notifying the user that the monthly Google Cloud quota has been reached, and automatically switch the player to the On-Device engine.

#### Scenario: Default engine is Google Cloud
- **WHEN** a reader opens an AI-formatted slice for the first time without a saved preference
- **THEN** the audio player selects Google Cloud as the active engine.

#### Scenario: Switching to Cloud engine cancels on-device worker and hides device progress
- **WHEN** on-device synthesis is in progress and the user switches the toggle to Google Cloud or initiates Cloud playback
- **THEN** the active on-device Web Worker synthesis task is cancelled immediately, the on-device progress badge (e.g. `2/45`) is hidden, and Cloud playback proceeds without ghost progress labels.

#### Scenario: Partially synthesized chunks are preserved on engine switch
- **WHEN** on-device synthesis is cancelled at 5 of 15 sentences due to switching to Google Cloud
- **THEN** the 5 completed chunks remain stored in IndexedDB under the current `contentHash`.
- **AND** when the user subsequently switches back to On-Device mode for that slice, synthesis resumes from sentence 6.

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

The voice selection dropdown trigger SHALL match the exact compact rendered height, vertical alignment, and border radius of the adjacent "Listen" action button (`h-8`, exactly 32px rendered height), and the reader audio player container SHALL maintain a stable, non-shifting minimum height (`min-h-[50px]`), ensuring that switching between On-Device and Google Cloud narration engines produces zero layout shift, vertical expansion, or jitter in the reader audio player bar or the content below it.
The system (both frontend client and backend synthesis handler) SHALL strictly enforce language compatibility between the document slice's content language and the selected voice model:
1. Slices in English (`en`) SHALL only be synthesized with English voice models (`en-US-*`).
2. Slices in Vietnamese (`vi`) SHALL only be synthesized with Vietnamese voice models (`vi-VN-*`).
3. The system SHALL NOT synthesize English documentation with a Vietnamese voice model or Vietnamese documentation with an English voice model, even if a user previously selected a voice in another language.
4. Voice preference persistence in client storage SHALL be partitioned by language (e.g. separate storage keys for `en` and `vi`), ensuring that selecting a preferred voice in one language does not overwrite or corrupt the voice selection when reading documents in another language.
5. If the client submits a voice ID that does not match the slice's content language, the backend SHALL reject the request with an RFC 7807 validation error (`VOICE_LANGUAGE_MISMATCH`).

All audio controls, buttons, tooltips, and status indicators SHALL use clean, professional, descriptive copy and SHALL NOT use hype or buzzword labels such as "AI", "AI Audio", or "Google AI". The UI SHALL use standard, subtle iconography (e.g. cloud icon or plain toggle switch) and SHALL NOT use mismatched or aggressive icons such as lightning bolts (`Zap`).

The reader audio engine selection controls SHALL respect the user's current playback state during engine transitions:
1. **No Autoplay When Paused**: If audio playback is currently paused or inactive (`playing === false`), switching between Google Cloud and On-Device engine modes SHALL update the selected engine mode and reset loaded slice state to idle, but SHALL NOT start audio synthesis or playback. The player SHALL remain paused until the user explicitly clicks the "Listen" / "Play" button.
2. **Continuous Playback When Active**: If audio playback is currently active (`playing === true`), switching between engine modes SHALL pause the previous engine and immediately begin synthesis and playback with the newly selected engine.
3. **Engine Toggle Audio Preparation**: Switching between Google Cloud and On-Device engine modes while playback is inactive SHALL update the selected engine mode and prepare the newly active engine's audio without starting audible autoplay, leaving the player ready to play when "Listen" is clicked.
4. **Explicit Fallback Recovery Exception**: Activating the 1-tap `[☁ Switch to Google Cloud]` fallback button from an error state SHALL always switch to Cloud mode and initiate playback immediately.
#### Scenario: Zero layout shift when toggling between Cloud and Device engines
- **WHEN** the user switches between On-Device and Google Cloud engine modes in the reader audio player
- **THEN** the total outer container height of the player bar remains constant (50px) without any vertical jump, expansion, or cumulative layout shift.

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

#### Scenario: Switching engine mode while paused does not autoplay
- **WHEN** audio playback is currently paused on an on-device narration slice and the user clicks the "Cloud" engine button
- **THEN** the active engine mode switches to Cloud, but audio playback does NOT start automatically and the player remains in a paused state showing "Listen" ("Nghe").

#### Scenario: Switching engine mode while playing transitions seamlessly
- **WHEN** audio is actively playing on an on-device narration slice and the user clicks the "Cloud" engine button
- **THEN** on-device playback stops, the engine mode switches to Cloud, and Cloud narration begins playing automatically.


#### Scenario: Switching to Cloud while inactive loads Cloud audio state
- **WHEN** playback is inactive on an on-device slice and the user clicks the "Cloud" engine button
- **THEN** the active engine mode switches to Cloud and loads the slice's Cloud audio information without starting audible autoplay, leaving the player ready to play when "Listen" is clicked.
### Requirement: Mobile Device Resource Safeguards & Single-Threaded WASM

On mobile browser environments (including iOS Safari, WebKit webviews, and Android mobile browsers), the reader's on-device text-to-speech engine SHALL adapt its execution parameters to respect mobile memory ceilings and process threading restrictions:

1. **Single-Threaded WASM Execution**: The Web Worker SHALL restrict WebAssembly execution to single-threaded mode (`numThreads = 1`) on mobile devices, regardless of whether the document context is cross-origin isolated or reports multiple logical CPU cores (`navigator.hardwareConcurrency > 1`). This prevents thread pool allocation failures, WebKit process crashes, and mobile out-of-memory errors.
2. **Quantized Model Weights on Mobile**: When running on mobile devices, the synthesis pipeline SHALL request quantized model weights (`q8` / `model_quantized.onnx`, ~36.6 MB) instead of unquantized FP32 weights (`model.onnx`, ~109 MB), reducing the memory footprint by over 60% and lowering network bandwidth requirements.
3. **Resilient Pipeline Construction**: Pipeline construction error handling in the Web Worker SHALL properly await fallback promises, ensuring that any WebGPU or precision failures cleanly fall back to WASM without leaving unhandled rejections or uninitialized states.

#### Scenario: Mobile browser forces single-threaded WASM execution
- **WHEN** a user on a mobile device (iOS Safari or Android Chrome) initiates on-device narration in a cross-origin isolated page reporting 8 CPU cores
- **THEN** the on-device Web Worker initializes ONNX WebAssembly with exactly 1 thread (`numThreads = 1`), preventing mobile browser process crash and thread exhaustion.

#### Scenario: Mobile browser loads lightweight quantized model weights
- **WHEN** an on-device narration model is downloaded on a mobile browser
- **THEN** the Web Worker downloads the quantized 8-bit ONNX model (~36.6 MB) rather than the 109 MB FP32 weights, conserving device memory and mobile data.

### Requirement: Actionable Error Diagnostics & Cloud Fallback Recovery

The reader audio player SHALL provide clear, actionable diagnostic feedback whenever audio synthesis or playback encounters an error, rather than masking failures behind opaque generic error messages:

1. **Specific Error Diagnostics**: The audio composable and player SHALL capture and display specific diagnostic context (such as memory exhaustion, worker initialization error, network failure, or HTTP error status) instead of an unexplained generic toast.
2. **Mobile Hardware Failure Cloud Recommendation**: When on-device synthesis fails on a mobile client due to hardware constraints, memory limits, or worker initialization errors, the player SHALL present an informative localized message explaining that on-device narration is constrained on this device and provide a direct 1-tap action to switch to the Google Cloud engine.
3. **Graceful Engine Switch on Error**: Activating the Cloud fallback action from the error state SHALL immediately switch the active engine mode to Google Cloud, clear the error state, and initiate Cloud narration for the slice without requiring a manual page refresh.

#### Scenario: On-device synthesis failure surfaces diagnostic reason
- **WHEN** on-device synthesis fails due to a worker or memory error
- **THEN** the reader UI displays an error notice containing actionable context explaining the failure rather than a blanket "unknown error".

#### Scenario: Mobile on-device failure offers 1-tap switch to Google Cloud TTS
- **WHEN** on-device synthesis encounters an error on a mobile device and the user's monthly cloud quota is not exhausted
- **THEN** the player displays a localized notification recommending Google Cloud narration with an action button that immediately switches to Cloud mode and plays the audio.

### Requirement: Model Download Progress Precedence & Compute Device Status Presentation

The reader audio player SHALL provide accurate visual feedback during on-device model initialization and compute execution:

1. **Download Progress Precedence**: While model weights are actively being downloaded over the network (`downloadProgress > 0` and `< 100`), the status display SHALL show the download percentage (`Downloading voice model… X%` / `Đang tải giọng đọc… X%`). The buffering indicator (`0/N`) SHALL NOT mask or shadow the download progress.
2. **Compute Device Badge Rendering**: The player SHALL bind and render the active compute backend badge (`[GPU]` or `[CPU]`) without emitting component instance template warnings, clearly informing the user whether narration is executing on hardware-accelerated WebGPU or multi-threaded CPU WebAssembly.

#### Scenario: Model download percentage takes visual precedence over buffering counter
- **WHEN** an on-device narration model is downloading 100 MB of ONNX weights over the network
- **THEN** the player displays "Downloading voice model… X%" (or "Đang tải giọng đọc… X%") with the live percentage, and does not display "Buffering audio… 0/N".

#### Scenario: Compute device badge displays active hardware backend
- **WHEN** on-device synthesis resolves the active device backend (WebGPU or WASM CPU)
- **THEN** the player displays the corresponding device badge and tooltip without logging property access errors in the browser console.
