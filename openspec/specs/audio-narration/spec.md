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

The reader SHALL synthesize a slice's narration sentence by sentence, surfacing synthesis progress (for example, sentences completed of the total). Upon completing synthesis of a slice, the reader SHALL assemble the sentences into a **single complete audio object** and SHALL use that complete audio as the **sole** playback source, so that playback of the whole slice is continuous and the user can seek to any position within the slice **without re-synthesizing**. The reader SHALL NOT leave playback stalled on a partial (single-sentence) fragment, and the reported total duration SHALL reflect the complete slice audio rather than an intermediate fragment.

The reader SHALL persist the complete slice audio in the browser's on-device storage (IndexedDB), keyed by `(chunkId, voice, contentHash)`, where `voice` is the language's on-device voice and `contentHash` is derived from the normalized narration script. A subsequent request to narrate the same `(chunkId, voice, contentHash)` — including in a later session — SHALL load the cached audio and SHALL NOT re-synthesize. When a slice's formatted content changes so its `contentHash` differs, the stale cache entry SHALL NOT be used and the audio SHALL be re-synthesized once. The audio cache SHALL enforce a bounded size (an entry or total-size cap) and evict least-recently-used entries; an evicted slice re-synthesizes on next play.

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

On reader routes, the application SHALL be cross-origin isolated so on-device synthesis can use multi-threaded execution. This isolation SHALL be confined to reader routes and SHALL NOT be applied to the authentication route or other routes, so cross-origin sign-in — which depends on cross-window communication — continues to function. Cross-origin resources the reader legitimately needs (web fonts, document images) SHALL continue to load under the isolation policy.

When cross-origin isolation is active, the synthesis worker SHALL configure the CPU (WASM) execution backend to run across multiple threads, scaling the thread count to the machine's available logical cores (`navigator.hardwareConcurrency`), so that a many-core CPU without a usable GPU is fully utilized for synthesis rather than running on a single thread. The configured thread count SHALL be bounded by the reported hardware concurrency to avoid oversubscription. When cross-origin isolation or multi-threaded execution is unavailable, synthesis SHALL still complete correctly on a single thread; thread configuration SHALL NOT change the produced audio.

#### Scenario: Reader route is cross-origin isolated
- **WHEN** a reader route (`/read/...`) is loaded
- **THEN** the page reports cross-origin isolation, enabling multi-threaded synthesis.

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
