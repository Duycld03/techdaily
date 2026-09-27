# audio-narration Spec Delta

## MODIFIED Requirements

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
