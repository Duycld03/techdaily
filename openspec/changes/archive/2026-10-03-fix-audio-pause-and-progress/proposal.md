# Proposal: Fix Audio Narration Pause Auto-Advance, Progress Retention, and Seeking

## Why

When listening to reader narration using the System TTS (Web Speech API) engine or switching between synthesis engines (System, Cloud, Device), several critical usability issues degrade the learning experience:
1. **Pause Auto-Advance Bug**: Pausing playback or changing engine modes inadvertently triggers an unexpected auto-advance to the next slice. This occurs because the zero-duration background silent audio carrier (used to maintain browser audio focus and Media Session controls) emits an `ended` event upon being paused and unlooped, which the global audio event handler misinterprets as natural slice completion.
2. **Missing Scrubber & Seek in Web Speech API**: In System TTS mode, `duration` is set to 0 and the seekable scrubber slider is completely hidden (replaced with a static text banner), making it impossible for users to scrub, seek, or jump through sentences.
3. **Resume Lock & Progress Loss**: Pausing System TTS locks internal playback flags (`isSystemStopped`), causing speech resumption to stall, and switching between engines resets playback to 0:00 rather than preserving elapsed progress.

Resolving these issues ensures readers can scrub and seek through narration across all engine modes, reliably pause and resume from where they left off, switch engines seamlessly, and utilize the "End of Slice" sleep timer without premature slice skips.

## What Changes

- **Silent Carrier Event Isolation**: Decouple the zero-duration silent audio carrier from the reader's slice-completion lifecycle. Ensure the HTML5 audio `ended` listener ignores events originating from the silent carrier or occurring while user-paused, preventing inadvertent `onSliceEnded` invocations.
- **Universal Audio Scrubber & Sentence Seeking for System TTS**:
  - Provide a seekable scrubber slider track for System TTS mode, computing estimated duration and real-time elapsed time based on sentence lengths and narration rate.
  - Implement sentence-level seeking in `seek()` for System TTS, allowing users to scrub to any sentence in the slice without restarting from the beginning.
  - Surface current playback time and estimated total duration (`0:15 / 1:20`) alongside sentence indicators.
- **Web Speech API Lifecycle & Progress Synchronization**:
  - Maintain the active sentence index (`systemSentenceIndex`) upon pause.
  - Properly reset `isSystemStopped = false` upon resume, allowing Web Speech API to resume cleanly from the paused sentence without freezing or repeating from the beginning.
  - Guard `utterance.onend` so cancellation during pause, seek, or engine switching does not trigger progression.
- **Sleep Timer Guarding**: Ensure the `end_of_slice` sleep timer only triggers upon genuine natural completion of the slice's audio or all sentence utterances, never from manual pause or silent carrier teardown.
- **Cross-Engine Progress Retention**:
  - Support `initialOffset` in `loadAndPlay()` when toggling between Cloud, Device, and System engines.
  - Preserve elapsed playback time when toggling between Cloud and Device engines.
  - Map sentence progress proportionally when switching between System TTS and time-based engines (Cloud/Device).

## Capabilities

### New Capabilities
<!-- None -->

### Modified Capabilities
- `audio-narration`: Refine requirements for playback lifecycle synchronization, auto-advance end-of-slice triggers, sleep timer completion, cross-engine progress retention, and scrubber/seek support across all engines.

## Impact

- **Frontend Composables**: `frontend/composables/useSliceAudio.ts` (silent carrier management, pause/resume state machine, sentence index tracking, `loadAndPlay` offset parameter, System TTS seeking and duration estimation).
- **Frontend Components**: `frontend/components/reader/ReaderAudioPlayer.vue` (unified scrubber rendering for System mode, engine toggle offset preservation, sleep timer handling).
- **Unit Tests**: `frontend/tests/composables/useSliceAudio.spec.ts`, `frontend/tests/components/reader/ReaderAudioPlayer.spec.ts`.
