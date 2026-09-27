# audio-narration Spec Delta

## MODIFIED Requirements

### Requirement: Free-Tier Voice Selection and Polished Presentation

The reader audio engine selection controls SHALL respect the user's current playback state during engine transitions:

1. **No Autoplay When Paused**: If audio playback is currently paused or inactive (`playing === false`), switching between Google Cloud and On-Device engine modes SHALL update the selected engine mode and reset loaded slice state to idle, but SHALL NOT start audio synthesis or playback. The player SHALL remain paused until the user explicitly clicks the "Listen" / "Play" button.
2. **Continuous Playback When Active**: If audio playback is currently active (`playing === true`), switching between engine modes SHALL pause the previous engine and immediately begin synthesis and playback with the newly selected engine.
3. **Explicit Fallback Recovery Exception**: Activating the 1-tap `[☁ Switch to Google Cloud]` fallback button from an error state SHALL always switch to Cloud mode and initiate playback immediately.

#### Scenario: Switching engine mode while paused does not autoplay
- **WHEN** audio playback is currently paused on an on-device narration slice and the user clicks the "Cloud" engine button
- **THEN** the active engine mode switches to Cloud, but audio playback does NOT start automatically and the player remains in a paused state showing "Listen" ("Nghe").

#### Scenario: Switching engine mode while playing transitions seamlessly
- **WHEN** audio is actively playing on an on-device narration slice and the user clicks the "Cloud" engine button
- **THEN** on-device playback stops, the engine mode switches to Cloud, and Cloud narration begins playing automatically.

### Requirement: Seekable Full-Slice Audio Cache

The reader on-device narration engine SHALL maintain reliable playback position across chunk cutovers, pause/resume actions, and partial cache restorations:

1. **Reliable Position Preservation on Complete Cutover**: When transitioning from initial streaming chunks to the complete assembled slice audio, the reader SHALL preserve the current playback offset using the media element's `loadedmetadata` event. If the user was paused when background synthesis completed, the player SHALL set the playback offset on the complete audio file and remain paused without resetting or discarding the paused position.
2. **Reliable Resume from Paused Offset**: When the user pauses on-device narration and subsequently resumes playback, audio playback SHALL continue seamlessly from the exact paused second without stalling, failing to play, or remaining frozen at an earlier timestamp.
3. **Replay on Ended Audio**: When playback is initiated on an audio source that has reached its end (`ended` is true or `currentTime >= duration`), the reader SHALL reset the playback position to 0 and begin playback from the start.
4. **Full Partial Cache Restoration & Continuous Playback**: When on-device synthesis is initiated on a slice with existing partial progress ($K$ of $N$ sentences, where $K \ge 1$ and $K < N$):
   - The engine SHALL concatenate all $K$ available sentence chunks into the initial playable audio segment, enabling immediate playback of all $K$ sentences without re-synthesizing.
   - The synthesis progress indicator SHALL immediately display $K/N$.
   - The Web Worker SHALL be dispatched to synthesize only sentences $K \dots N-1$.
   - As subsequent sentences $K, K+1, \dots$ finish synthesis, they SHALL be chained seamlessly for uninterrupted playback.

#### Scenario: Resuming local TTS playback after pause
- **WHEN** a user pauses on-device TTS narration mid-sentence (e.g. at 0:15) while synthesis finishes in the background, and later clicks "Listen" to resume
- **THEN** audio playback resumes playing from 0:15 and continues through the remainder of the slice without freezing.

#### Scenario: Playing completed audio restarts from beginning
- **WHEN** a user clicks "Listen" on a slice whose playback has already reached the end
- **THEN** the audio restarts playing from 0:00 rather than remaining frozen at the end duration.

#### Scenario: Resuming on-device synthesis from 5/15 partial cache on page refresh
- **WHEN** a user refreshes the page on a 15-sentence slice where 5 sentences were previously synthesized and cached in IndexedDB, and initiates on-device narration
- **THEN** all 5 sentences are assembled into the initial playable audio, synthesis progress indicates 5/15, playback of the 5 sentences is immediately available, and the Web Worker synthesizes only sentences 5 through 14.
