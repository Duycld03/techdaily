# Design

## Context

See `proposal.md` for background and problem motivation.

During on-device streaming TTS in `useSliceAudio.ts`, sentences are synthesized one by one by `ttsSynth.worker.ts`. The player begins playing as soon as a pre-roll buffer threshold is met (e.g. 5 sentences = ~35s). 

Currently, when the audio reaches the end of the pre-roll at 35 seconds:
1. `onChunkEndedCallback` checks `currentPlayingIndex + 1 < buffers.length`. If chunk 5 has not completed synthesis yet, it sets `playing.value = false`, freezing playback at 35s.
2. `cancelWorkerSynthesis()` was clearing progress counters (`synthIndex = 0, synthTotal = 0`) on pause or engine switch.
3. In `ReaderAudioPlayer.vue`, `onToggle()` checks `if (loadedId === source.chunkId) play()`, looping the 35s pre-roll without resuming worker synthesis.
4. Switching to Cloud mode while inactive resets the player to idle without loading or fetching Cloud audio.

## Goals / Non-Goals

**Goals:**
- Eliminate playback freezing when audio reaches the end of currently available chunks before subsequent chunks finish synthesis.
- Ensure audio enters a buffering state (`loading`) on stream underrun and automatically resumes as soon as the next chunk arrives.
- Ensure clicking "Nghe" on an incomplete slice resumes synthesis of remaining sentences.
- Ensure toggling between Cloud and Device engines while inactive prepares audio without forcing audible playback.
- Preserve background synthesis during user pause.

**Non-Goals:**
- Modifying underlying ONNX Runtime or HuggingFace Transformers.js models.
- Changing server-side Cloud TTS API contracts or database schemas.

## Decisions

### Decision 1: Stream Underrun Buffering State in `useSliceAudio.ts`
Introduce an explicit `isWaitingForNextChunk` flag in `synthesizeOnDevice`:
- In `onChunkEndedCallback()`:
  - If `isStreaming === true` and `currentPlayingIndex + 1 >= buffers.length`:
    - Set `isWaitingForNextChunk = true`.
    - Set `status.value = 'loading'`.
    - Maintain `playing.value = true`. Do NOT mark playback as ended.
- In `onChunk()`:
  - When the next chunk `currentPlayingIndex + 1` arrives:
    - If `isWaitingForNextChunk`:
      - Set `isWaitingForNextChunk = false`.
      - Call `playChunk(currentPlayingIndex + 1)`.
      - Playback continues seamlessly without user intervention.

### Decision 2: Incomplete Synthesis Detection in `ReaderAudioPlayer.vue`
Update `onToggle()` in `ReaderAudioPlayer.vue`:
- Check whether the current slice audio is complete. On device engine, if `synthIndex.value < synthTotal.value` or `status.value !== 'ready'`:
  - Invoke `loadAndPlay(source.value)`.
  - This loads the partial cache and dispatches the worker to synthesize the remaining sentences.
- Only if the audio is fully assembled and complete does it invoke `play()`.

### Decision 3: Audio Preparation on Engine Toggle Without Autoplay
Update `loadAndPlay(source: NarrationSource, autoPlay = true)` in `useSliceAudio.ts`:
- When `autoPlay === false`:
  - For Cloud: checks cache or queries `/api/v1/library/chunks/{chunkId}/audio`, sets `duration`, sets `status = 'ready'`, but omits `play()`.
  - For Device: checks partial cache, sets `duration`, prepares `preRollWav`, sets `status = 'ready'`, but omits `play()`.
- In `ReaderAudioPlayer.vue`:
  - `onToggleEngine` passes `autoPlay` into `loadAndPlay(source.value, autoPlay)`.
  - If paused, the player updates the duration and displays the scrubber ready for 1-click play.

### Decision 4: Preserving Worker Synthesis During Pause
- Modify `cancelWorkerSynthesis()` so it is NOT invoked on user `pause()`.
- Only cancel worker synthesis when:
  1. Switching to Cloud engine (`setEngineMode('cloud')`).
  2. Navigating to a different slice or unmounting the reader (`dispose()`).
  3. User leaves the reader route.
- Pausing playback pauses `audio.value.pause()` while allowing background synthesis to continue filling `buffers` and IndexedDB.

## Risks / Trade-offs

- **Risk:** Background synthesis continues while paused, consuming CPU.
  - *Mitigation:* Background synthesis runs on a Web Worker with single-thread WASM or GPU, saving to IndexedDB. Once complete, it terminates and frees resources. If the user navigates away, `dispose()` immediately terminates the worker.
