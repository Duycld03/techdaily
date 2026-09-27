# audio-narration Spec Delta

## MODIFIED Requirements

### Requirement: Seekable Full-Slice Audio Cache

The reader SHALL synthesize a slice's narration sentence by sentence, surfacing synthesis progress (for example, sentences completed of the total). When on-device synthesis begins for a multi-sentence slice, the reader SHALL buffer an initial pre-roll threshold of sentences (at least 33% of the slice's total sentences, or 100% of sentences for slices with 3 or fewer sentences) before starting audible playback, preventing audio playback from exhausting its buffer and stalling when earlier sentences are shorter than the computation time of subsequent sentences.

Once this pre-roll threshold is satisfied, audio playback SHALL begin immediately with the assembled initial sentences, while the worker continues synthesizing the remaining sentences in the background. As subsequent sentences finish synthesis, they SHALL be queued seamlessly for uninterrupted continuous playback without audio underruns or restarting from the beginning.

While synthesizing on-device sentences, the reader SHALL persist intermediate generated sentence chunks in the browser's on-device storage (IndexedDB) keyed by `(chunkId, voice, contentHash)`. If on-device synthesis is interrupted (such as by pausing, navigating away, or switching to the Cloud engine) and later re-initiated:
1. The reader SHALL inspect IndexedDB for existing partial chunks matching `(chunkId, voice, contentHash)`.
2. If matching partial chunks are found and the slice's `contentHash` matches, the reader SHALL restore the already-synthesized chunks ($0 \dots K-1$), update the synthesis progress indicator to reflect $K/N$, immediately start pre-roll playback if $K \ge \text{targetBufferCount}$, and dispatch synthesis to the worker for only the remaining ungenerated sentences ($K \dots N-1$), eliminating redundant computation.
3. If the slice's content has changed such that its `contentHash` differs, the stale partial cache entries SHALL NOT be used, any stale partial entries SHALL be deleted, and synthesis SHALL restart from sentence 0.

Upon completing synthesis of all sentences in a slice, the reader SHALL assemble the sentences into a **single complete audio object** and SHALL transition to using that complete audio as the **sole** playback source, so that playback of the whole slice is continuous and the user can seek to any position within the slice **without re-synthesizing**. The reader SHALL persist the complete audio in IndexedDB, clear the temporary partial chunks for that key, and ensure the reported total duration reflects the complete slice audio once assembled.

#### Scenario: Resuming interrupted on-device synthesis from partial cache
- **WHEN** on-device synthesis was previously interrupted after synthesizing 5 of 15 sentences, and the user re-initiates on-device narration for the identical slice
- **THEN** the reader loads the 5 cached sentence chunks from IndexedDB, sets synthesis progress to 5/15, immediately starts playback if pre-roll threshold is satisfied, and requests the worker to synthesize only sentences 6 through 15.

#### Scenario: Modified slice invalidates stale partial chunks
- **WHEN** a slice with partial cached chunks (e.g. 5 of 15 sentences) has its content revised such that its `contentHash` changes, and the user plays on-device narration
- **THEN** the reader detects the hash mismatch, discards the stale partial chunks, and synthesizes all sentences starting from index 0 under the new `contentHash`.

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

#### Scenario: Switching to Cloud engine cancels on-device worker and hides device progress
- **WHEN** on-device synthesis is in progress and the user switches the toggle to Google Cloud or initiates Cloud playback
- **THEN** the active on-device Web Worker synthesis task is cancelled immediately, the on-device progress badge (e.g. `2/45`) is hidden, and Cloud playback proceeds without ghost progress labels.

#### Scenario: Partially synthesized chunks are preserved on engine switch
- **WHEN** on-device synthesis is cancelled at 5 of 15 sentences due to switching to Google Cloud
- **THEN** the 5 completed chunks remain stored in IndexedDB under the current `contentHash`.
- **AND** when the user subsequently switches back to On-Device mode for that slice, synthesis resumes from sentence 6.

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
