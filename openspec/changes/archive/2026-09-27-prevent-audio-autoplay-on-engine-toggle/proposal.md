# Proposal

## Why

1. **Unwanted Autoplay on Engine Toggle**: In `ReaderAudioPlayer.vue`, switching between the "Cloud" and "Device" narration engines unconditionally invokes `loadAndPlay()` whenever audio has been loaded for the current slice. Consequently, if a user explicitly pauses or stops audio playback and then toggles the engine switch, the player immediately resumes synthesis and plays audio without user consent. Switching narration engines while paused should update the preferred engine and prepare the slice state without triggering unwanted playback.
2. **Local TTS Resume Failure & Timestamp Freeze**: During on-device streaming narration, when a user pauses playback, the player frequently fails to resume upon clicking "Nghe" (Play), remaining frozen at the old minute/timestamp. This occurs because:
   - When the worker finishes synthesizing all sentences in the background, `setSource(complete)` sets `el.src = objectUrl` and immediately assigns `el.currentTime = offsetSec`. Because `el.readyState` is `HAVE_NOTHING` (0), the assignment is ignored or resets to 0.
   - If an audio chunk or pre-roll reaches its end before the next chunk is synthesized, `onChunkEndedCallback` sets `playing.value = false`, and subsequent calling of `play()` on an ended `<audio>` element without seeking back fails or freezes.
   - When switching engines while paused, `currentTime` and `duration` were not reset, leaving stale timestamps displayed in the UI.
3. **Partial Synthesis Resume Loss on Refresh**: When on-device synthesis has generated $K$ of $N$ sentences (e.g. 5 of 15 sentences) and the user refreshes the page or re-selects Device mode:
   - The player currently slices only `buffers.slice(0, target)` (at most 2 sentences), dropping sentences 2, 3, 4 from the pre-roll and desynchronizing `currentPlayingIndex` with the worker's synthesis cursor.
   - Restoring a partial cache must concatenate ALL $K$ available chunks into the initial playable audio so the user can immediately listen to all 5 sentences while the worker finishes sentences $K \dots N-1$ in the background.

## What Changes

- **Conditional Playback on Engine Toggle**: In `ReaderAudioPlayer.vue`, preserve the playback state (`wasPlaying = playing.value`) during an engine toggle.
  - If audio was paused or stopped (`playing.value === false`), switching between Cloud and Device SHALL update `engineMode`, pause audio, reset `loadedId`, `currentTime`, `duration`, and status to idle, and SHALL NOT start audio playback. The player remains paused until the user explicitly clicks the "Listen" / "Play" button.
  - If audio was actively playing (`playing.value === true`), switching between Cloud and Device SHALL continue seamless transition by loading and playing audio under the new engine mode.
- **Explicit Cloud Error Recovery Maintained**: Activating the 1-tap `[☁ Switch to Google Cloud]` recovery button from an error state continues to initiate immediate playback, representing an explicit user intent to recover and hear speech.
- **Robust Local TTS Cutover & Resume**:
  - In `useSliceAudio.ts`, update `setSource(blob, initialOffset = 0)` to reliably apply `initialOffset` once `loadedmetadata` fires (`el.readyState >= 1`), ensuring playback position is accurately preserved when transitioning to complete audio while paused or playing.
  - Update `play()` to detect if `el.ended` or `el.currentTime >= el.duration` and reset `currentTime = 0`, allowing smooth playback restart when clicked from an ended state.
  - Manage in-flight chunk transitions with a dedicated waiting state so background synthesis delays do not permanently cancel playback or leave the timer frozen.
- **Comprehensive Partial Cache Restoration on Device Mode**:
  - When partial chunks exist ($K$ of $N$ sentences): concatenate all $K$ available chunks into the initial playable pre-roll audio, set `currentPlayingIndex = K - 1`, and dispatch the Web Worker to synthesize only the remaining sentences $K \dots N-1$.
  - Users can immediately listen to the $K$ sentences without re-synthesizing, while background synthesis streams the remaining chunks.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `audio-narration`: Update engine toggle behavior so engine transitions preserve the user's pause state and do not trigger unexpected autoplay; ensure on-device TTS audio position reliably resumes from the paused position without freezing; ensure partial synthesis progress ($K/N$) is fully restored and immediately playable on refresh or engine selection.

## Impact

- **Frontend Component & Composable**: `frontend/components/reader/ReaderAudioPlayer.vue`, `frontend/composables/useSliceAudio.ts`.
- **User Experience**: Eliminates intrusive autoplay when users change engine settings while paused; fixes resume freezing on local TTS; and preserves partial synthesis progress across page refreshes.
- **Zero Breaking Contract Changes**: Retains existing `useSliceAudio` APIs and audio storage structures.
