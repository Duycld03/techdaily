# audio-narration Specification

## Purpose
Synthesizes a reader slice's AI-formatted text into speech entirely on the user's device (in-browser neural TTS), with the voice chosen automatically by slice language, and plays it back inside `/read/[bookId]` — caching the complete slice audio locally so playback is fully seekable, free, offline, and never re-synthesized on seek or replay.

## Requirements

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

While on-device narration is streaming sentence chunks and has not finished synthesizing the entire slice (`synthIndex < synthTotal`):
1. The reader SHALL compute an estimated total duration for the slice based on average sentence duration.
2. The player time display SHALL present the estimated total duration alongside the currently buffered duration (e.g. `2:08 (~6:40)` on desktop viewports) and render the active sentence synthesis progress count (`(18/67)` on mobile, `(Đang tạo 18/67 câu...)` on desktop).
3. Completed audio SHALL strictly only be committed to the permanent full-slice cache (`cache.set`) when all sentences of the slice have finished synthesis (`buffers.length === sentences.length`), preventing premature cache pollution.

The reader SHALL persist the complete slice audio in the browser's on-device storage (IndexedDB), keyed by `(chunkId, voice, contentHash)`, where `voice` is the language's on-device voice and `contentHash` is derived from the normalized narration script. A subsequent request to narrate the same `(chunkId, voice, contentHash)` — including in a later session — SHALL load the cached audio and SHALL NOT re-synthesize. When a slice's formatted content changes so its `contentHash` differs, the stale cache entry SHALL NOT be used and the audio SHALL be re-synthesized once. The audio cache SHALL enforce a bounded size (an entry or total-size cap) and evict least-recently-used entries; an evicted slice re-synthesizes on next play.

#### Scenario: Displaying total slice scope during streaming synthesis
- **WHEN** a user views an on-device slice where 18 of 67 sentences have been synthesized (2:08 of audio)
- **THEN** the player displays a synthesis progress indicator `(18/67)` and estimated duration `(~6:40)` indicating that 18 of the total 67 sentences are buffered and the remaining sentences will continue generating.

#### Scenario: Uninterrupted playback across pre-roll to completion
- **WHEN** a user listens to on-device narration on a 6-minute document slice
- **THEN** playback starts immediately with the buffered pre-roll and continuously plays through sentence 67 without halting or terminating at the initial pre-roll duration.
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

The `/read/[bookId]` reader SHALL provide an audio playback control that lets the user listen to the current slice's narration, available only when the current slice is AI-formatted (hidden or disabled otherwise). Playback SHALL support play, pause, and position seeking across all engine modes (Cloud, Device, and System).

1. **Universal Scrubber Slider**: The reader audio player SHALL render an interactive seekable scrubber slider for all active engine modes whenever slice narration is ready or playing.
2. **System Mode Scrubber & Seeking**: In System TTS mode (`engineMode === 'system'`), the player SHALL compute an estimated total duration and real-time elapsed time from sentence lengths and speech speed, rendering the scrubber slider and elapsed/total time (`mm:ss / mm:ss`). Users SHALL be able to drag or click the slider to seek to any sentence in the slice without restarting from the beginning.
3. **Action Labels**: The play/pause toggle button SHALL render concise, balanced action labels symmetrical across playback states and locales: "Listen" in English and "Nghe" in Vietnamese when idle/paused, and "Pause" in English and "Tạm dừng" in Vietnamese when actively playing. Accessible descriptions (`aria-label`) SHALL communicate the full descriptive action.

The reader SHALL offer a **playback speed** control spanning 0.5x to 2.0x applied client-side to the `<audio>` element's `playbackRate` **without re-synthesizing audio**; the selected speed SHALL persist across sessions and slices.

The audio player controls layout SHALL ensure that the primary playback controls (play/pause toggle, engine mode switch) and utility controls (auto-advance toggle, sleep timer menu, and speed selector) remain fully visible and contained within the player card boundaries on mobile viewports down to 360px width during both idle and playing states, preventing any control from overflowing or being pushed outside the card.

The playback control API surface SHALL encapsulate playback state (status, current time, duration, speed, volume, and quota) directly within the reader audio orchestrator composable. Generic library or document stores SHALL NOT maintain dead audio quota fields or unused audio fetch actions. The playback control SHALL NOT expose unused pitch adjustments, unreferenced storage keys, or redundant toggle abstractions.

While the language model is downloading, the reader SHALL display the download progress as an accurate whole-number percentage in the inclusive range **0 to 100**; it SHALL NOT display a value greater than 100% or otherwise mis-scaled. While synthesizing after download, the reader SHALL display a synthesis progress state.

All audio control labels, speed labels, and download/synthesis progress messages SHALL render localized text (en/vi) from the i18n catalog, and the catalog SHALL contain strictly active user-facing audio keys without orphaned keys for unrendered pitch settings or unused drill progression hints. The controls SHALL follow the reader's responsive typography and `whitespace-nowrap shrink-0` layout invariants across both locales. The reader SHALL NOT use native browser dialogs for audio state; status is conveyed via in-page controls and `useToast()`.

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

#### Scenario: Concise play/pause button labels across locales
- **WHEN** audio is idle or paused in the Vietnamese locale
- **THEN** the play/pause button renders visible text "Nghe"
- **WHEN** playback is active in the Vietnamese locale
- **THEN** the play/pause button renders visible text "Tạm dừng"
- **WHEN** playback is active in the English locale
- **THEN** the play/pause button renders visible text "Pause".

#### Scenario: Playback controls contained within player card on mobile viewports
- **WHEN** viewing the audio narration controls on a mobile viewport (360px–390px width) during active playback in either English or Vietnamese
- **THEN** all controls in the top bar—including the play/pause toggle, engine switch, auto-advance button, sleep timer button, and playback speed button—remain fully visible inside the player card boundary without horizontal clipping or pushing elements outside the container.

#### Scenario: User seeks within System TTS narration
- **WHEN** System TTS narration is active on a slice with 10 sentences
- **AND** the user drags the scrubber slider to the 50% position (sentence 5)
- **THEN** the active utterance is canceled without triggering slice completion
- **AND** playback resumes immediately from sentence 5
- **AND** the displayed elapsed time reflects the updated sentence offset

#### Scenario: Scrubber visibility in System TTS mode
- **WHEN** System TTS engine is selected and slice narration is loaded
- **THEN** the seekable scrubber slider track is visible and interactive
- **AND** duration is calculated as greater than zero
- **AND** elapsed time updates as each sentence is narrated

#### Scenario: Audio playback controls contain only active user interactions
- **WHEN** a user opens the reader audio player on an AI-formatted slice
- **THEN** the player card renders controls for play/pause, engine switching (System/Cloud/Device), voice selection, playback speed, auto-advance, and sleep timer
- **AND** no pitch adjustment controls or orphaned UI hooks are rendered.

#### Scenario: Audio quota state is encapsulated in the playback orchestrator
- **WHEN** the reader queries or evaluates cloud audio narration quota
- **THEN** the request and state management occur directly within the reader audio composable
- **AND** the general library store (`useLibraryStore`) does not maintain redundant quota state or fetch actions.

#### Scenario: Clean i18n audio catalog without dead keys
- **WHEN** inspecting the reader localization messages in English and Vietnamese
- **THEN** all defined audio message keys map directly to active player controls, toasts, or progress indicators.
### Requirement: GPU-Accelerated On-Device Synthesis with CPU Fallback

The reader SHALL run on-device narration synthesis on the GPU when the browser exposes GPU compute to the synthesis worker and the target neural network architecture is compatible with available WebGPU shader kernels, and SHALL fall back to CPU execution when GPU compute is unavailable or fails to initialize, so synthesis completes on any supported browser. The choice of execution backend SHALL NOT change the produced audio or any user-facing control. A backend-initialization failure SHALL NOT surface as a narration error while a working fallback exists, and the backend selection SHALL NOT be re-probed or re-failed per sentence within a synthesis run.

When the browser exposes a choice of GPU adapters, the worker SHALL request a high-performance adapter so a discrete GPU is preferred over integrated graphics. The worker SHALL report the resolved execution device to the reader, and the reader SHALL surface a localized indication of the active compute device (GPU or CPU) within the audio control, following responsive typography and `whitespace-nowrap shrink-0` layout invariants across both English and Vietnamese locales.

For Meta VITS / MMS-TTS architecture models (`Xenova/mms-tts-*`), whose duration predictors require INT64 indexing on `GatherND` operators unsupported by ONNX Runtime Web's WebGPU kernels, the synthesis engine SHALL route execution directly to the CPU (WASM) backend and SHALL NOT request WebGPU adapters or attempt WebGPU pipeline construction. This guarantees stability against unsupported INT64 shader exceptions and eliminates WebGPU initialization failures across desktop and mobile browsers.

#### Scenario: GPU backend used when available
- **WHEN** GPU compute is available to the synthesis worker and a user plays narration for a compatible model
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

#### Scenario: MMS-TTS models execute on CPU WASM without WebGPU probing
- **WHEN** a user initiates on-device narration for an English or Vietnamese slice using an MMS-TTS voice model on a device with an active GPU
- **THEN** the synthesis worker selects the CPU (WASM) backend directly without attempting WebGPU adapter initialization or throwing INT64 shader exceptions.

#### Scenario: Audio produced on CPU WASM is complete and seekable
- **WHEN** narration synthesizes on the CPU (WASM) backend
- **THEN** audio completes sentence by sentence, streams without underrun, and is cached in browser IndexedDB for seekable playback.

#### Scenario: Compute device badge indicates CPU execution
- **WHEN** on-device synthesis initializes for an MMS-TTS model
- **THEN** the reader audio player displays the localized CPU compute indicator cleanly without console warnings.

---

### Requirement: Multi-Threaded Synthesis Enabled on Reader Routes Only

On reader routes, the application SHALL be cross-origin isolated so on-device synthesis can use multi-threaded execution. This isolation SHALL be established across deployment tiers without duplicate or conflicting response headers. Response headers `Cross-Origin-Opener-Policy: same-origin` and `Cross-Origin-Embedder-Policy: credentialless` SHALL be emitted cleanly without repetition. This isolation SHALL be confined to reader routes and SHALL NOT be applied to the authentication route or other routes, so cross-origin sign-in — which depends on cross-window communication — continues to function. Cross-origin resources the reader legitimately needs (web fonts, document images) SHALL continue to load under the isolation policy.

Static assets and worker script endpoints (such as `/_nuxt/**`) SHALL deliver `Cross-Origin-Embedder-Policy: credentialless` and `Cross-Origin-Resource-Policy: cross-origin` across reverse proxy deployment tiers, ensuring that dedicated Web Workers spawned by isolated reader pages are not blocked with `ERR_BLOCKED_BY_RESPONSE` (`coep-frame-resource-needs-coep-header`).
When entering a reader route from an unisolated browsing context (such as post-login or client-side navigation from library routes), the client application SHALL ensure the browsing context acquires cross-origin isolation (executing a full document navigation if `self.crossOriginIsolated` is not yet active), so that `self.crossOriginIsolated === true` reliably holds on reader views.

When cross-origin isolation is active (`self.crossOriginIsolated === true`), the synthesis worker SHALL configure the CPU (WASM) execution backend to run across multiple threads, scaling the thread count to the machine's available logical cores (`navigator.hardwareConcurrency`), so that a many-core CPU without a usable GPU is fully utilized for synthesis rather than running on a single thread. The configured thread count SHALL be bounded by the reported hardware concurrency to avoid oversubscription. When cross-origin isolation or multi-threaded execution is unavailable, the worker SHALL clamp thread allocation to 1 without emitting console errors; thread configuration SHALL NOT change the produced audio.

#### Scenario: Clean non-duplicate cross-origin headers delivered
- **WHEN** a client requests a `/read/**` document
- **THEN** the HTTP response contains single instances of `Cross-Origin-Opener-Policy: same-origin` and `Cross-Origin-Embedder-Policy: credentialless` without duplicate header values.

#### Scenario: Worker script delivered with COEP on production reverse proxy
- **WHEN** a cross-origin isolated reader page requests a Web Worker script under `/_nuxt/**` through the production reverse proxy
- **THEN** the HTTP response contains `Cross-Origin-Embedder-Policy: credentialless` and `Cross-Origin-Resource-Policy: cross-origin`
- **AND** the browser instantiates the worker without `ERR_BLOCKED_BY_RESPONSE` or COEP violation errors.
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

The synthesis worker SHALL load the narration model at the weight precision that minimizes synthesis latency on the selected execution backend, rather than the framework's default. On the CPU (WASM) backend it SHALL use full precision (fp32): the framework's default WASM weights are quantized (int8), which run materially slower on this model because ONNX Runtime Web has no fast int8 kernels, so full precision is faster on the CPU path. On the GPU (WebGPU) backend it SHALL use half precision (fp16). If the preferred precision fails to build for a backend, the worker SHALL fall back to full precision without surfacing a narration error. The choice of weight precision SHALL NOT change any user-facing control, the audio cache key `(chunkId, voice, contentHash)`, or the produced audio.

On mobile devices, the worker SHALL load quantized 8-bit (`q8`) weights (`model_quantized.onnx`, ~36.6 MB), reducing memory consumption and preventing mobile browser tab crashes.

For Meta VITS / MMS-TTS architecture models (`Xenova/mms-tts-*`), the worker SHALL NOT attempt to load half-precision (`fp16`) weights, as the remote ONNX graph definition contains type discrepancies that fail validation and corrupt the framework model pipeline cache.

#### Scenario: CPU path uses full precision for lowest latency
- **WHEN** a user plays narration and synthesis runs on the CPU (WASM) backend on desktop
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

#### Scenario: Desktop environment loads full-precision weights
- **WHEN** on-device narration is initiated on a desktop browser
- **THEN** the synthesis pipeline loads `fp32` weights and executes multi-threaded WASM inference.

#### Scenario: Mobile environment loads quantized weights
- **WHEN** on-device narration is initiated on a mobile browser (Android Chrome, iOS Safari, or mobile WebKit)
- **THEN** the synthesis pipeline loads `q8` quantized weights (~36.6 MB) and executes single-threaded WASM inference.

#### Scenario: Invalid fp16 weights are never requested for MMS-TTS
- **WHEN** the synthesis worker constructs a pipeline for `Xenova/mms-tts-vie` or `Xenova/mms-tts-eng`
- **THEN** the worker never requests `dtype: 'fp16'`, avoiding graph validation failures and model cache corruption.

---

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
The reader audio composable and player component SHALL capture, diagnose, and present specific diagnostic feedback whenever audio synthesis or playback encounters an error, rather than masking failures behind opaque generic error messages:
1. **Specific Error Diagnostics**: The audio composable and player SHALL capture and display specific diagnostic context (such as memory exhaustion, worker initialization error, network failure, or HTTP error status) instead of an unexplained generic toast.
2. **Console Diagnostics**: The composable layer SHALL log full raw error details and stack traces to `console.error('[useSliceAudio] Device TTS Error:', err)` for all worker errors, lifecycle faults, and unhandled rejections.
3. **Network Error Classification**: Resource loading failures, including WebKit/Safari's `TypeError: Load failed`, CDN connection timeouts, and offline fetch rejections, SHALL be classified as `NETWORK_ERROR` rather than `DEVICE_INIT_FAILED`.
4. **Mobile Hardware Failure Cloud Recommendation**: When on-device synthesis fails on a mobile client due to hardware constraints, memory limits, or worker initialization errors, the player SHALL present an informative localized message explaining that on-device narration is constrained on this device and provide a direct 1-tap action to switch to the Google Cloud engine.
5. **Graceful Engine Switch on Error**: Activating the Cloud fallback action from the error state SHALL immediately switch the active engine mode to Google Cloud, clear the error state, and initiate Cloud narration for the slice without requiring a manual page refresh.
6. **Unified Cloud Fallback Availability**: When audio synthesis encounters a recoverable error under either `device` mode (hardware constraints, memory limits) OR `system` mode (synthesis-failed, voice-unavailable), the player toolbar SHALL render the 1-tap Cloud fallback action button (`Chuyển sang Google Cloud` / `Switch to Cloud`), provided the user's monthly cloud quota is not exhausted.

#### Scenario: On-device synthesis failure surfaces diagnostic reason
- **WHEN** on-device synthesis fails due to a worker or memory error
- **THEN** the reader UI displays an error notice containing actionable context explaining the failure rather than a blanket "unknown error".

#### Scenario: Mobile on-device failure offers 1-tap switch to Google Cloud TTS
- **WHEN** on-device synthesis encounters an error on a mobile device and the user's monthly cloud quota is not exhausted
- **THEN** the player displays a localized notification recommending Google Cloud narration with an action button that immediately switches to Cloud mode and plays the audio.

#### Scenario: Worker errors output detailed console diagnostics
- **WHEN** an on-device synthesis worker encounters an error during model download or sentence inference
- **THEN** `useSliceAudio` logs the raw error message to the browser console under `[useSliceAudio] Device TTS Error:` before updating player state.

#### Scenario: WebKit "Load failed" categorized as network error
- **WHEN** a mobile Safari or WebKit browser fails to download model assets and throws `TypeError: Load failed`
- **THEN** the composable categorizes the error as `NETWORK_ERROR` and displays the localized network error message rather than a device hardware failure message.

#### Scenario: Hardware error provides 1-tap Cloud fallback
- **WHEN** on-device synthesis fails due to hardware constraints and the user's monthly cloud quota is not exhausted
- **THEN** the player displays an error notice recommending Google Cloud narration with an action button that immediately switches to Cloud mode and plays the audio.

#### Scenario: System TTS error offers 1-tap switch to Google Cloud TTS
- **WHEN** System TTS encounters an error and the player enters error state
- **THEN** the player toolbar displays an actionable "Chuyển sang Google Cloud" button allowing the user to immediately trigger Cloud playback.

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

### Requirement: Serialized Worker Inference & Cancellation Mutual Exclusion

The on-device text-to-speech Web Worker and client audio player controller SHALL enforce strict mutual exclusion for model inference across concurrent or overlapping synthesis requests:

1. **Sequential Model Inference Execution**: The Web Worker SHALL serialize all sentence synthesis calls through an internal queue, guaranteeing that no two neural network inference executions (`entry.synth()`) execute concurrently on the same ONNX Runtime session.
2. **Job Sequencing and Stale Request Invalidation**: Each synthesis request received by the Web Worker SHALL be tagged with a sequential job identifier. When a new synthesis job arrives or cancellation is requested, any queued or in-flight sentences associated with superseded jobs or cancelled request tokens SHALL be discarded immediately without passing to the model inference pipeline.
3. **Unconditional Worker Cancellation on Engine Switch**: When switching narration engines or loading a new slice audio session (`loadAndPlay`), the client SHALL unconditionally issue a cancellation signal to any in-flight worker task before dispatching new synthesis requests.
4. **Worker Fault Recovery**: If the Web Worker terminates unexpectedly or emits an unhandled error (`worker.onerror`), the client layer SHALL nullify and discard the existing worker instance, ensuring that subsequent audio generation attempts spawn a fresh Web Worker rather than failing against a faulted thread.
5. **Debounced Action Triggers During Loading**: The reader audio player action buttons SHALL ignore click events while audio synthesis or buffering is actively in progress (`isLoading === true`), preventing rapid user interactions from queueing duplicate parallel synthesis requests.

#### Scenario: Switching engines mid-synthesis discards prior worker job
- **WHEN** on-device synthesis is actively generating sentences and the user switches to Google Cloud narration
- **THEN** a cancellation message is dispatched to the Web Worker, and remaining sentences from the cancelled job are discarded without running further ONNX model inference.

#### Scenario: Resuming on-device playback executes without ONNX session collision
- **WHEN** playback is paused, switched to Google Cloud, switched back to On-Device, and resumed via the Play button
- **THEN** on-device synthesis resumes sequentially from cached sentences without encountering `Another run() is already in progress` or worker deadlock.

#### Scenario: Clicking Listen button while buffering does not spawn duplicate requests
- **WHEN** the reader audio player is in the loading state (`isLoading === true`)
- **THEN** clicking the Listen/Play button is ignored and does not trigger secondary concurrent synthesis calls.

#### Scenario: Worker error resets worker reference for clean recreation
- **WHEN** the Web Worker encounters an unrecoverable runtime fault
- **THEN** the client discards the worker instance reference, allowing subsequent narration requests to instantiate a clean Web Worker instance.

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

### Requirement: Continuous Cross-Slice Auto-Advance
The reader audio player SHALL provide an automatic slice progression mechanism that continues narration to subsequent document slices without manual user intervention.

1. **Auto-Advance Toggle**: The player SHALL render an "Auto Next" (`autoAdvance`) toggle control, defaulted to enabled (`true`) and persisted in `localStorage` (`deeppace_reader_audio_auto_advance`).
2. **Strict Natural Completion Trigger**: Progression to the next slice SHALL trigger strictly and solely upon genuine natural completion of the slice's narration:
   - For Cloud and Device modes: When the assembled audio track reaches its natural conclusion (`ended` event) while `isUserPaused === false`.
   - For System mode: When the final sentence chunk completes (`systemSentenceIndex >= systemSentences.length`) while `isSystemStopped === false`.
3. **Pause and Interruption Isolation**: Pausing playback, switching synthesis engines, stopping background carrier audio, seeking, or re-synthesizing SHALL NEVER trigger `onSliceEnded` or advance the reader to the next slice.
4. **Silent Carrier Isolation**: The HTML5 audio element running the zero-duration silent carrier SHALL NOT propagate `ended` events to `onSliceEnded`. Stopping or unlooping the silent carrier MUST NOT trigger slice progression.

#### Scenario: Current slice audio finishes with auto-advance enabled
- **WHEN** audio narration for slice $N$ reaches completion and `autoAdvance` is true and slice $N+1$ exists
- **THEN** the reader automatically advances to slice $N+1$ and begins narration of slice $N+1$ without requiring the user to press Play or click Next.

#### Scenario: Auto-advance disabled at end of slice
- **WHEN** audio narration for slice $N$ finishes and `autoAdvance` is false
- **THEN** playback transitions to paused (`playing === false`), remaining on slice $N$.

#### Scenario: User pauses during System TTS playback
- **WHEN** System TTS is actively narrating a slice and `autoAdvance` is enabled
- **AND** the user clicks the "Pause" button (or triggers remote pause)
- **THEN** speech synthesis halts immediately
- **AND** the reader remains on the current slice without advancing to the next slice
- **AND** `auto-advance` is not emitted

#### Scenario: User switches engine while narration is playing
- **WHEN** narration is playing in System mode
- **AND** the user clicks the "Cloud" or "Device" engine toggle
- **THEN** System TTS and silent carrier halt cleanly
- **AND** the reader remains on the current slice without skipping to the next slice
- **AND** narration transitions to the newly selected engine
### Requirement: Soft Chime Transition on Slice Boundary
The reader audio system SHALL emit a subtle, non-intrusive auditory chime whenever auto-advancing across slice boundaries.
1. **Web Audio Synthesis**: The chime SHALL be synthesized dynamically via the Web Audio API (`AudioContext`), requiring zero external network asset downloads.
2. **Acoustic Characteristics**: The chime SHALL produce a gentle dual-frequency harmonic sweep (440Hz transitioning to 880Hz, duration 200–300ms, low-gain envelope) signaling progression without startling the listener.
3. **Execution Timing**: The chime SHALL play immediately upon slice completion, directly before starting narration of the subsequent slice.

#### Scenario: Auto-advancing emits soft chime
- **WHEN** slice $N$ completes narration and auto-advance initiates transition to slice $N+1$
- **THEN** a gentle synthesized Web Audio chime is sounded, followed immediately by narration of slice $N+1$.

### Requirement: Lockscreen and Headset Media Session Control
The reader audio system SHALL integrate with the Web Media Session API (`navigator.mediaSession`) to provide background playback metadata and remote hardware controls.
1. **Metadata Synchronization**: When narration begins or advances, the system SHALL update `navigator.mediaSession.metadata` with:
   - `title`: Current slice title or slice section heading.
   - `artist`: Document or book title.
   - `album`: "DeepPace Reader".
   - `artwork`: Document cover thumbnail image.
2. **Remote Action Handlers**: The system SHALL register action handlers for:
   - `play`: Resumes audio playback or speech synthesis.
   - `pause`: Pauses active playback.
   - `nexttrack`: Advances to the next slice and initiates narration.
   - `previoustrack`: Rewinds to the previous slice or restarts the current slice.
3. **Background Persistence**: The media session SHALL remain active and responsive when the mobile screen is locked or when interacting via Bluetooth headphones/car audio controls.

#### Scenario: User clicks Next Track on Bluetooth headset
- **WHEN** narration is playing and the user presses the "Next Track" hardware button on their Bluetooth headphones
- **THEN** the media session `nexttrack` handler executes, advancing the reader to the next slice and beginning its narration.

#### Scenario: User pauses from lockscreen notification
- **WHEN** audio is playing and the user taps "Pause" on the Android lockscreen media notification
- **THEN** playback is paused and the lockscreen state updates to paused.

### Requirement: Fixed-Preset Sleep Timer and 15-Second Audio Fade-Out
The reader audio player SHALL provide a Sleep Timer control with fixed duration presets and a smooth audio fade-out mechanism.
1. **Fixed Presets**: The player SHALL offer selectable presets: 15 minutes (`15m`), 30 minutes (`30m`), 45 minutes (`45m`), 60 minutes (`60m`), and "End of Current Slice" (`end_of_slice`), plus an option to cancel/turn off.
2. **Countdown Display**: While active, the reader audio player toolbar SHALL display a real-time countdown badge indicating remaining time (e.g. `29:45` or slice icon).
3. **15-Second Audio Fade-Out**: When a timed sleep timer reaches its final 15 seconds, the system SHALL smoothly attenuate audio volume exponentially from 1.0 down to 0.0 over the 15-second window prior to pausing.
4. **End-of-Slice Stop**: When set to "End of Current Slice", the player SHALL disable auto-advance for the current slice, pausing playback upon slice completion with volume intact.
5. **State Preservation**: When the timer expires, playback SHALL pause and volume SHALL be restored to 1.0 for the next manual playback session, leaving the reader bookmark at the exact position reached.
6. **End-of-Slice Sleep Guard**: When `end_of_slice` is active, the sleep timer completion action (pausing playback, clearing timer, and notifying the user) SHALL trigger strictly upon natural slice completion. Manual user pauses, engine changes, or silent carrier teardown SHALL NOT trigger the sleep timer completion notification or dismiss the timer prematurely.

#### Scenario: Sleep timer reaches final 15 seconds
- **WHEN** an active 30-minute sleep timer counts down to 0:15 remaining
- **THEN** audio volume smoothly fades down from 1.0 to 0.0 over 15 seconds, and at 0:00 playback pauses without sudden auditory clipping.

#### Scenario: User selects End of Current Slice preset
- **WHEN** the user selects "End of Current Slice" as the sleep timer mode
- **THEN** the reader plays the remainder of the active slice, and upon reaching slice completion, halts playback without advancing to the next slice.

#### Scenario: User manually pauses with End of Slice sleep timer active
- **WHEN** the sleep timer is set to "End of Current Slice" (`end_of_slice`)
- **AND** the user clicks the "Pause" button mid-slice
- **THEN** playback pauses normally
- **AND** the sleep timer remains active (not dismissed)
- **AND** no "Sleep timer ended" toast is triggered
### Requirement: Multi-Engine Audio Narration Toolbar Layout
The reader audio narration toolbar SHALL render using a container-resilient, two-row responsive layout that guarantees zero visual element collisions, zero text overlapping, and zero button clipping across mobile (390px), constrained split-pane containers (~550px), and unconstrained desktop viewports (1440px+).
1. **Row 1 (Primary Controls & Utilities)**:
   - The primary Play/Pause button SHALL display an icon and dynamic localized text ("Nghe" / "Listen" when idle or paused; "Tạm dừng" / "Pause" when actively playing).
   - An Engine Mode Segmented Switch SHALL allow switching between `System` (Web Speech API), `Cloud` (Google Cloud TTS), and `Device` (On-Device Neural Web Worker).
   - Utility controls (Auto-advance toggle, Sleep Timer dropdown, and Speed cycle button) SHALL be grouped compactly on the right side of Row 1.
2. **Row 2 (Voice Selection & Timeline / Status)**:
   - The context-aware Voice Selector (`AppSelect`) SHALL display voices appropriate to the active engine (System voices for `system`, Cloud voices for `cloud`).
   - For audio streams with known media duration (`duration > 0`), Row 2 SHALL host the seekable slider track and timestamp indicator (`currentTime / duration`).
   - For Web Speech API playback (`duration === 0`), Row 2 SHALL host an active synthesis status banner indicating browser voice playback rather than collapsing or rendering blank space.
3. **Debug Tier Badge Removal**:
   - The internal debug cascade tier badge (`Bậc: {tier}` / `Tier: {tier}`) SHALL NOT be rendered in the DOM or visible user interface.

#### Scenario: Audio player rendered inside constrained split-pane container
- **WHEN** the reader is viewed in a constrained container such as the Daily Studio two-pane layout (~550px width)
- **THEN** all controls across Row 1 and Row 2 remain fully visible and accessible without horizontal scrollbars, text clipping, or overlapping controls.

#### Scenario: Play button text reflects active playback state
- **WHEN** audio playback starts using either Cloud TTS, On-Device TTS, or Web Speech API
- **THEN** the main play button label dynamically updates to "Tạm dừng" (or "Pause" in English locale), and updates back to "Nghe" (or "Listen") upon pause or completion.

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

### Requirement: Daily Pacer Workflow Auto-Advance Gating
In the Daily Learning Pacer workflow (`/today`), completion of slice audio narration SHALL NOT trigger automatic advancement to the next slice, regardless of the `autoAdvance` setting. 
1. Completing the daily learning milestone strictly requires the user to interact with and complete the Senior Challenge drill for the day's slice.
2. In the standalone book reader (`/read/[bookId]`), completion of slice audio narration SHALL continue to auto-advance to the next slice when the user has enabled the `autoAdvance` preference.

#### Scenario: Audio narration finishes on Today page
- **WHEN** audio playback completes for the current slice on `/today`
- **THEN** the reader remains on the current slice and does not advance to the next slice, allowing the user to solve the Senior Challenge drill.

#### Scenario: Audio narration finishes on standalone Book Reader page
- **WHEN** audio playback completes for the current slice on `/read/[bookId]` with `autoAdvance` enabled
- **THEN** the reader automatically navigates to and starts playback for the next slice in the chapter.

### Requirement: Web Speech API Sentence-Level Streaming Chunking
When synthesizing narration via System TTS (`engineMode === 'system'`), the reader SHALL segment the plain-text narration script into sentence-level chunks using punctuation-aware sentence boundaries (`splitSentences`):
1. **Input Size Bounds**: The reader SHALL NOT pass multi-thousand-character prose blocks to a single `SpeechSynthesisUtterance`. Each utterance text SHALL be bounded to individual sentence boundaries, strictly adhering to mobile platform input constraints (including Android `TextToSpeech.getMaxSpeechInputLength()` of 4,000 characters).
2. **Sequential Sentence Progression**: The player SHALL queue and play sentence utterances sequentially via `utterance.onend`.
3. **Playback Lifecycle & Resume Synchronization**:
   - When paused, the active sentence index (`systemSentenceIndex`) SHALL be preserved.
   - Resuming from pause SHALL reset `isSystemStopped = false` and speak from `systemSentenceIndex`, continuing subsequent sentences smoothly until the slice ends.
   - Canceling an utterance during pause or engine switch SHALL NOT invoke `onSliceEnded`.
4. **Sentence Seeking**: When `seek(time)` is invoked in System mode, the system SHALL calculate the target sentence index from the requested time, halt active speech, update `systemSentenceIndex`, and begin speaking from that sentence.

#### Scenario: Long book slice is chunked for System TTS
- **WHEN** a user plays a 6,000-character reading slice using System TTS
- **THEN** the player splits the script into sentence-level utterances and plays them sequentially, preventing `getMaxSpeechInputLength` overflows and browser synthesis cutoff.

#### Scenario: User pauses and resumes in System mode
- **WHEN** System TTS is playing sentence 3 of 8
- **AND** the user pauses playback
- **AND** later clicks "Play"
- **THEN** narration resumes from sentence 3
- **AND** when sentence 3 completes, narration automatically continues through sentences 4 to 8

---

### Requirement: Canonical narration cache database
The on-device narration cache SHALL use `deeppace-audio` with its existing supported store/key schemas and eviction limits. Known legacy cache data SHALL be handled only by the shared client-storage migration. Ordinary playback SHALL not open the legacy database, and migration SHALL not invoke synthesis or change voice/content identity.

#### Scenario: Playback after cache migration
- **WHEN** a compatible legacy audio entry has transferred to the canonical database
- **THEN** the existing cache key identifies the same complete audio without re-synthesis.

#### Scenario: New cache write or partial entry
- **WHEN** narration caches a complete or partial result
- **THEN** it writes to the canonical database under the existing appropriate store/key schema
- **AND** current partial-cache validation and eviction behavior remain applicable.

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
