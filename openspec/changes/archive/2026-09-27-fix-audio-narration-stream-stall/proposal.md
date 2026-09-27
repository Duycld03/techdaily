# Proposal

## Why

During on-device text-to-speech narration, users encounter a complete playback stall where the player reaches the end of initial buffered sentences (e.g. 35 seconds across 5 sentences) and permanently freezes at `0:35 / 0:35`. Refreshing the page, clicking "Listen", or toggling between "Cloud" and "Device" engines fails to continue synthesis for the rest of the slice (e.g. sentences 5 through 84).

This happens due to four interconnected lifecycle defects in `useSliceAudio.ts` and `ReaderAudioPlayer.vue`:
1. **Stream Underrun Freezes Playback**: When playback reaches the end of the initial pre-roll buffer before the next sentence chunk is synthesized by the Web Worker, `onChunkEndedCallback` immediately sets `playing = false` and terminates playback instead of entering a buffering state or continuing to wait for incoming chunks.
2. **"Listen" Button Loops Old Buffer Instead of Resuming Synthesis**: When the player reaches the end of the buffered audio, `loadedId` remains set to the current slice ID. Clicking "Listen" invokes `onToggle()`, which sees `loadedId === source.chunkId` and invokes `play()`, endlessly replaying the same 35-second buffer from 0:00 without ever dispatching the worker to synthesize the remaining sentences.
3. **Engine Toggle While Inactive Discards State Without Loading**: Switching to Cloud mode while playback is inactive passes `autoPlay = false`, which clears the slice audio and resets the player to idle without fetching or preparing the Cloud audio. The user perceives that switching engines does nothing.
4. **Premature Worker Cancellation on Pause**: Pausing playback unconditionally cleared `synthIndex`, `synthTotal`, and cancelled worker synthesis, terminating background generation and preventing subsequent sentences from ever being produced.

## What Changes

- **Stream Underrun Resilience & Buffering**: When audio playback reaches the end of currently available chunks while synthesis is still streaming (`isStreaming === true`), the player enters a temporary buffering state (`status = 'loading'`) and automatically plays incoming chunks as soon as they arrive, rather than terminating playback and freezing at the intermediate duration.
- **Auto-Resume Incomplete Synthesis on Play**: In `ReaderAudioPlayer.vue`, when the user clicks "Listen", if synthesis for the slice is incomplete (`synthIndex < synthTotal` or status is idle/ended without full assembly), `onToggle()` initiates synthesis of the remaining sentences (`loadAndPlay(source)`) rather than merely looping the partial pre-roll buffer.
- **Engine Toggle Prepares Audio Without Forcing Play**: When switching between Cloud and Device engines while inactive, the player loads the newly selected engine's audio into the player (checking cache and setting total duration) so the audio is immediately ready to play, without forcing audible autoplay.
- **Resilient Worker Lifecycle**: Ensure background synthesis is not permanently destroyed upon temporary user pauses, preserving `synthIndex` and `synthTotal` counters and allowing background generation to continue or cleanly resume.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `audio-narration`: Update on-device streaming playback so audio seamlessly buffers and chains subsequent sentence chunks when pre-roll finishes; ensure replaying an incomplete slice resumes synthesis of remaining sentences; and ensure engine toggles prepare audio without forcing autoplay.

## Impact

- **Frontend Composable & UI**: `frontend/composables/useSliceAudio.ts`, `frontend/components/reader/ReaderAudioPlayer.vue`.
- **Zero Breaking Contract Changes**: Preserves all existing `useSliceAudio` public interfaces, storage keys, and database schemas.
