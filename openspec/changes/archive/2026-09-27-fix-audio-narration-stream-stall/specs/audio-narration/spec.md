# audio-narration Spec Delta

## MODIFIED Requirements

### Requirement: Seekable Full-Slice Audio Cache

The reader on-device narration engine SHALL maintain uninterrupted continuous playback and stream underrun resilience across streaming synthesis and replay operations:

1. **Stream Underrun Resilience**: While background synthesis is active (`isStreaming === true`), if audio playback reaches the end of the currently buffered chunks (including the initial pre-roll buffer or subsequent chunks) before the next chunk has finished synthesis:
   - The player SHALL NOT terminate playback, SHALL NOT set `playing = false`, and SHALL NOT freeze the time display at the intermediate duration.
   - The player SHALL transition to a buffering state (`status = 'loading'`).
   - As soon as the next sentence chunk finishes synthesis in the Web Worker, the player SHALL automatically transition to playing that chunk without requiring the user to click "Listen".
2. **Incomplete Synthesis Replay Resumes Generation**: When a user clicks the "Listen" button on a slice whose on-device synthesis has not completed (i.e. `synthIndex < synthTotal` or when only partial pre-roll audio is loaded):
   - The reader SHALL resume worker synthesis for all remaining ungenerated sentences ($K \dots N-1$).
   - The reader SHALL NOT merely replay the partial pre-roll buffer from 0:00 while leaving remaining sentences unsynthesized.
3. **Preserve Worker Lifecycle on User Pause**: Pausing audio playback during on-device streaming narration SHALL pause the audio element without permanently discarding background synthesis progress (`synthIndex`, `synthTotal`), enabling the worker to continue or cleanly resume generation.

#### Scenario: Audio catches up to worker without freezing
- **WHEN** audio playback reaches the end of the 5-sentence pre-roll buffer (at 0:35) while sentence 6 is still being synthesized by the Web Worker
- **THEN** playback enters a buffering state instead of stopping, and as soon as sentence 6 arrives, playback immediately and automatically continues with sentence 6.

#### Scenario: Replaying an incomplete slice resumes synthesis
- **WHEN** on-device playback is sitting paused at the end of a 5-sentence pre-roll on an 85-sentence document, and the user clicks "Listen"
- **THEN** the reader initiates synthesis of sentences 6 through 85 while playing, updating the progress indicator (e.g. "6/85") until all sentences are complete.

### Requirement: Free-Tier Voice Selection and Polished Presentation

The reader audio engine selection controls SHALL prepare the selected engine's audio state upon engine transitions:

1. **Engine Toggle Audio Preparation**: Switching between Google Cloud and On-Device engine modes while playback is inactive SHALL update the selected engine mode and prepare the newly active engine's audio (inspecting local or server cache and displaying available duration), but SHALL NOT start audible playback until the user clicks "Listen".
2. **Immediate Readiness on User Play**: When the user clicks "Listen" following an engine switch, playback SHALL start immediately using the newly selected engine without requiring multiple toggle cycles or page reloads.

#### Scenario: Switching to Cloud while inactive loads Cloud audio state
- **WHEN** playback is inactive on an on-device slice and the user clicks the "Cloud" engine button
- **THEN** the active engine mode switches to Cloud and loads the slice's Cloud audio information without starting audible autoplay, leaving the player ready to play when "Listen" is clicked.
