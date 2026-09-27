# audio-narration Spec Delta

## MODIFIED Requirements

### Requirement: Seekable Full-Slice Audio Cache

The reader SHALL synthesize a slice's narration sentence by sentence, surfacing synthesis progress (for example, sentences completed of the total). When on-device synthesis begins, the reader SHALL start progressive audio playback as soon as the first sentence chunk is synthesized, rather than waiting for all subsequent sentences in the slice to finish synthesis. While earlier sentence chunks play, the worker SHALL continue synthesizing subsequent sentences in the background, queuing them seamlessly for uninterrupted continuous playback.

Upon completing synthesis of all sentences in a slice, the reader SHALL assemble the sentences into a **single complete audio object** and SHALL transition to using that complete audio as the **sole** playback source, so that playback of the whole slice is continuous and the user can seek to any position within the slice **without re-synthesizing**. The reader SHALL NOT leave playback stalled on a partial (single-sentence) fragment, and the reported total duration SHALL reflect the complete slice audio once assembled.

The reader SHALL persist the complete slice audio in the browser's on-device storage (IndexedDB), keyed by `(chunkId, voice, contentHash)`, where `voice` is the language's on-device voice and `contentHash` is derived from the normalized narration script. A subsequent request to narrate the same `(chunkId, voice, contentHash)` — including in a later session — SHALL load the cached audio and SHALL NOT re-synthesize. When a slice's formatted content changes so its `contentHash` differs, the stale cache entry SHALL NOT be used and the audio SHALL be re-synthesized once. The audio cache SHALL enforce a bounded size (an entry or total-size cap) and evict least-recently-used entries; an evicted slice re-synthesizes on next play.

#### Scenario: Progressive playback starts on first sentence chunk
- **WHEN** a user initiates on-device narration for a multi-sentence slice not yet in cache
- **THEN** audio playback begins as soon as the first sentence chunk is synthesized (under 10 seconds), while remaining sentences continue synthesizing in the background.

#### Scenario: Subsequent sentences queue seamlessly without audio dropouts
- **WHEN** the first sentence finishes playing while background synthesis continues
- **THEN** the subsequent sentence audio chunks play sequentially and continuously without audible gaps or stalls.

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
