# Design

## Context

In `ReaderAudioPlayer.vue`, when playing on-device narration:
1. `duration` is derived from the currently loaded audio element (`el.duration`), which reflects only the assembled pre-roll buffer (e.g. 18 sentences = 2:08), while the full slice across 67 sentences is ~6:42.
2. The user sees `2:08` on Device and `6:42` on Cloud, leading them to believe Device narration stopped or failed.
3. In `ReaderAudioPlayer.vue`, `onToggle()` was re-invoking `loadAndPlay()` if `isSynthesisIncomplete` was true, restarting synthesis from scratch every time the user clicked "Listen" or "Pause" instead of smoothly playing the loaded audio.

## Goals / Non-Goals

**Goals:**
- Present clear visual feedback indicating that `2:08` is an active buffer of a ~6-minute slice.
- Display estimated total duration and persistent sentence progress `(18/67 câu)` during on-device streaming.
- Ensure clicking "Listen" immediately plays the loaded buffer and allows background synthesis to stream smoothly without restarting from sentence 0.

**Non-Goals:**
- Altering the underlying MMS-TTS model inference speed.
- Changing Cloud TTS audio generation.

## Decisions

### Decision 1: Estimated Total Duration & Clear Progress in Player Bar
In `ReaderAudioPlayer.vue`:
- Compute `estimatedTotalDuration`:
  ```ts
  const estimatedTotalDuration = computed(() => {
    if (engineMode.value !== 'device' || synthIndex.value <= 0 || synthTotal.value <= 0) return duration.value
    if (synthIndex.value >= synthTotal.value) return duration.value
    return Math.round((duration.value / synthIndex.value) * synthTotal.value)
  })
  ```
- In the time display:
  - If `synthTotal > 0 && synthIndex > 0 && synthIndex < synthTotal`:
    - Render: `{{ formatTime(currentTime) }} / {{ formatTime(duration) }} (~{{ formatTime(estimatedTotalDuration) }})`
    - Alongside: `({{ t('reader.audio_synthesizing', { current: synthIndex, total: synthTotal }) }})`
  - This informs the user that 2:08 is currently playable and the total audio will reach ~6:40 once all 67 sentences are synthesized.

### Decision 2: Prevent Repeated Synthesis Restarts on Play Click
In `ReaderAudioPlayer.vue`:
- When audio is already loaded (`loadedId === source.chunkId`) and `status === 'ready'`:
  - Clicking "Listen" should call `void play()`.
  - Background worker synthesis continues uninterrupted in the background.
  - Do NOT call `loadAndPlay()` unless the slice has changed or the player is in an error or idle state.

## Risks / Trade-offs

- **Risk:** Sentence length varies, so estimated total duration is an approximation until all sentences finish.
  - *Mitigation:* Prepend with `~` (e.g. `~6:40`) to clearly indicate it is an estimate, switching to exact duration as soon as the full audio is assembled.
