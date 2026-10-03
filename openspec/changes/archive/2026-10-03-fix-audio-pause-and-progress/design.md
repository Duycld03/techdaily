# Design: Audio Narration Pause Isolation, System Scrubber, and Progress Retention

## Context

TechDaily's reader audio narration (`useSliceAudio.ts` and `ReaderAudioPlayer.vue`) supports three synthesis engines: Google Cloud TTS, On-Device neural TTS (Piper WASM/WebGPU), and System TTS (Web Speech API). In System mode, a zero-duration silent WAV audio track is loaded and looped on an internal `HTMLAudioElement` to maintain browser audio focus and headphone/media session controls.

Currently:
1. `stopSilentCarrier()` pauses the audio element and sets `loop = false`. Because the silent audio data is zero seconds in duration, Chromium and WebKit immediately dispatch an `ended` event on the audio element.
2. The `ended` event handler in `ensureAudio()` does not distinguish the silent carrier from real audio, immediately calling `deps.onSliceEnded?.()`, which triggers an auto-advance to the next slice and dismisses the sleep timer.
3. System TTS sets `duration = 0`, hiding the seekable scrubber slider in `ReaderAudioPlayer.vue` and preventing seeking. `seek(time)` only sets `audio.value.currentTime`, ignoring speech synthesis.
4. When paused, `isSystemStopped = true` is set, but `play()` does not reset `isSystemStopped` when calling `synth.resume()`, leaving the player stuck at the end of the resumed sentence.
5. Switching engine tabs resets playback to 0:00 because `loadAndPlay()` does not accept an initial playback offset.

## Goals / Non-Goals

**Goals:**
- **Silent Carrier Event Isolation**: Ensure the silent carrier never emits or triggers `ended` or `onSliceEnded` events.
- **Web Speech API Scrubbing & Seeking**: Provide an interactive scrubber slider, duration estimation, elapsed time tracking, and sentence-level seeking for System TTS mode.
- **Robust Pause & Resume Lifecycle**: Ensure pause cleanly halts speech, preserves sentence position, and resumes seamlessly without auto-advance or audio collisions.
- **End-of-Slice Sleep Guard**: Ensure `end_of_slice` sleep timer actions fire strictly upon natural slice completion.
- **Cross-Engine Progress Handoff**: Preserve elapsed playback position when switching between Cloud, Device, and System engines.

**Non-Goals:**
- Modifying neural model weights or ONNX Runtime Web execution kernels.
- Changing backend server APIs or Google Cloud TTS endpoints.

## Decisions

### Decision 1: Guard and Silence Carrier Teardown
In `ensureAudio()`, guard the `ended` event listener:
```ts
el.addEventListener('ended', () => {
  if (isSilentCarrierActive || isUserPaused || isSystemEngineActive) {
    return
  }
  if (onChunkEndedCallback) {
    onChunkEndedCallback()
  } else {
    playing.value = false
    deps.onSliceEnded?.()
  }
})
```
In `stopSilentCarrier()`:
- Reset `isSilentCarrierActive = false` before altering element properties.
- Unset `el.src = ''` or clear listeners before setting `loop = false` and `el.pause()`.

### Decision 2: Sentence-Level Timing & Scrubber for System TTS
In `synthesizeOnSystem`:
- Calculate estimated sentence durations using character length and speech speed:
  ```ts
  const CHARS_PER_SECOND = 16
  const sentenceDurations = systemSentences.map(s => Math.max(1.0, (s.length / CHARS_PER_SECOND) / Math.max(0.5, speed.value)))
  const totalEstimatedDuration = sentenceDurations.reduce((sum, d) => sum + d, 0)
  duration.value = totalEstimatedDuration
  ```
- Calculate `currentTime.value` as the sum of completed sentence durations plus active elapsed time.
- Implement sentence seeking in `seek(targetTime)`:
  - Find `targetIndex` where cumulative sentence duration reaches `targetTime`.
  - Update `systemSentenceIndex = targetIndex` and `currentTime.value = targetTime`.
  - If playing, cancel current utterance via `synth.cancel()` and call `speakSentence(targetIndex)`.

### Decision 3: Clean Pause and Resume Lifecycle for Web Speech API
- **On Pause**: Set `isUserPaused = true`, `isSystemStopped = true`, and stop the silent carrier cleanly. Cancel active speech utterance if needed to avoid the 15-second Chromium SpeechSynthesis timeout bug.
- **On Play**:
  - Reset `isUserPaused = false` and `isSystemStopped = false`.
  - Start silent carrier and call `speakSentence(systemSentenceIndex)` to guarantee reliable resumption across all browsers.
- In `utterance.onend`: Check `if (isSystemStopped || isUserPaused) return`. Only call `deps.onSliceEnded?.()` when `systemSentenceIndex >= systemSentences.length`.

### Decision 4: Cross-Engine Offset Preservation
- Add `initialOffset = 0` parameter to `loadAndPlay(source: NarrationSource, autoPlay = true, initialOffset = 0)`.
- In `ReaderAudioPlayer.vue`:
  ```ts
  function onToggleEngine(mode: AudioEngine, autoPlay = playing.value): void {
    const currentOffset = currentTime.value
    setEngineMode(mode)
    if (loadedId.value && source.value) {
      pause()
      loadedId.value = source.value.chunkId
      void loadAndPlay(source.value, autoPlay, currentOffset)
    }
  }
  ```
- Forward `initialOffset` into `setSource()` for Cloud/Device, and compute the starting `systemSentenceIndex` for System TTS.

## Risks / Trade-offs

- **Speech Duration Estimation**: Character-based duration estimation is an approximation of actual speech tempo.
  - *Mitigation*: The estimate dynamically recalculates when `speed` changes. Seeking to a sentence boundary is deterministic and reliable.
- **Browser SpeechSynthesis Inconsistencies**: Some browsers drop utterances when paused for extended periods.
  - *Mitigation*: By managing playback through discrete sentence boundaries and resuming from the active sentence index, we avoid the browser's native `synth.pause()` hang while maintaining exact sentence-level progress.
