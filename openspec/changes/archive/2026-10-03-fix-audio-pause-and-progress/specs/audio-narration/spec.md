# Spec Delta: audio-narration

## ADDED Requirements

### Requirement: Cross-Engine Playback Progress Retention

When switching between synthesis engines (System, Cloud, Device) within the same slice:

1. **Time-Based Engine Switching (Cloud <-> Device)**: The reader SHALL capture `currentTime` from the active audio element and pass it as `initialOffset` to `loadAndPlay()`, resuming playback at the exact elapsed second.
2. **Hybrid Engine Switching (System <-> Cloud/Device)**:
   - When switching from System to Cloud/Device, the reader SHALL map the current sentence progress ratio (`systemSentenceIndex / totalSentences`) to the target audio duration and start playback at that elapsed position.
   - When switching from Cloud/Device to System, the reader SHALL map the elapsed time ratio (`currentTime / duration`) to the corresponding sentence index and resume narration from that sentence.

#### Scenario: Switching from Cloud to Device during playback
- **WHEN** Cloud narration is playing at 0:45 of a 2:00 slice
- **AND** the user switches the engine to "Device"
- **THEN** Device synthesis or cached audio loads and resumes playback at 0:45
- **AND** playback does not reset to 0:00

---

## MODIFIED Requirements

### Requirement: Reader Audio Playback Controls

The `/read/[bookId]` reader SHALL provide an audio playback control that lets the user listen to the current slice's narration, available only when the current slice is AI-formatted (hidden or disabled otherwise). Playback SHALL support play, pause, and position seeking across all engine modes (Cloud, Device, and System).

1. **Universal Scrubber Slider**: The reader audio player SHALL render an interactive seekable scrubber slider for all active engine modes whenever slice narration is ready or playing.
2. **System Mode Scrubber & Seeking**: In System TTS mode (`engineMode === 'system'`), the player SHALL compute an estimated total duration and real-time elapsed time from sentence lengths and speech speed, rendering the scrubber slider and elapsed/total time (`mm:ss / mm:ss`). Users SHALL be able to drag or click the slider to seek to any sentence in the slice without restarting from the beginning.
3. **Action Labels**: The play/pause toggle button SHALL render concise, balanced action labels symmetrical across playback states and locales: "Listen" in English and "Nghe" in Vietnamese when idle/paused, and "Pause" in English and "Tạm dừng" in Vietnamese when actively playing. Accessible descriptions (`aria-label`) SHALL communicate the full descriptive action.

#### Scenario: User plays the current slice narration
- **WHEN** a user viewing an AI-formatted slice activates the play control
- **THEN** the reader synthesizes (or loads cached) audio for the slice's language voice, loads it into the `<audio>` element, and begins playback with seek support.

#### Scenario: Download progress is shown as a value between 0 and 100 percent
- **WHEN** the narration model is downloading and the reader shows the download percentage
- **THEN** the displayed value is a whole number between 0 and 100 inclusive (for example `42%`), never a mis-scaled value such as `10000%`.

#### Scenario: User changes playback speed during playback
- **WHEN** a user sets the playback speed to 1.5x while audio is playing
- **THEN** the `<audio>` element's `playbackRate` updates to 1.5 immediately without re-synthesizing, and the speed preference is persisted.

#### Scenario: Speed preference persists across sessions
- **WHEN** a user who previously selected 1.25x returns to the reader in a later session
- **THEN** the reader restores 1.25x playback speed as the active preference.

#### Scenario: Concise play/pause button labels across locales
- **WHEN** audio is idle or paused in the Vietnamese locale
- **THEN** the play/pause button renders visible text "Nghe"
- **WHEN** playback is active in the Vietnamese locale
- **THEN** the play/pause button renders visible text "Tạm dừng"
- **WHEN** playback is active in the English locale
- **THEN** the play/pause button renders visible text "Pause".

#### Scenario: Playback controls contained within player card on mobile viewports
- **WHEN** viewing the audio narration controls on a mobile viewport (360px–390px width) during active playback in either English or Vietnamese
- **THEN** all controls in the top bar—including the play/pause toggle, engine switch, auto-advance button, sleep timer button, and playback speed button—remain fully visible inside the player card boundary without horizontal clipping or pushing elements outside the container.

#### Scenario: User seeks within System TTS narration
- **WHEN** System TTS narration is active on a slice with 10 sentences
- **AND** the user drags the scrubber slider to the 50% position (sentence 5)
- **THEN** the active utterance is canceled without triggering slice completion
- **AND** playback resumes immediately from sentence 5
- **AND** the displayed elapsed time reflects the updated sentence offset

#### Scenario: Scrubber visibility in System TTS mode
- **WHEN** System TTS engine is selected and slice narration is loaded
- **THEN** the seekable scrubber slider track is visible and interactive
- **AND** duration is calculated as greater than zero
- **AND** elapsed time updates as each sentence is narrated

---

### Requirement: Continuous Cross-Slice Auto-Advance

The reader audio player SHALL provide an automatic slice progression mechanism that continues narration to subsequent document slices without manual user intervention.

1. **Auto-Advance Toggle**: The player SHALL render an "Auto Next" (`autoAdvance`) toggle control, defaulted to enabled (`true`) and persisted in `localStorage` (`techdaily_reader_audio_auto_advance`).
2. **Strict Natural Completion Trigger**: Progression to the next slice SHALL trigger strictly and solely upon genuine natural completion of the slice's narration:
   - For Cloud and Device modes: When the assembled audio track reaches its natural conclusion (`ended` event) while `isUserPaused === false`.
   - For System mode: When the final sentence chunk completes (`systemSentenceIndex >= systemSentences.length`) while `isSystemStopped === false`.
3. **Pause and Interruption Isolation**: Pausing playback, switching synthesis engines, stopping background carrier audio, seeking, or re-synthesizing SHALL NEVER trigger `onSliceEnded` or advance the reader to the next slice.
4. **Silent Carrier Isolation**: The HTML5 audio element running the zero-duration silent carrier SHALL NOT propagate `ended` events to `onSliceEnded`. Stopping or unlooping the silent carrier MUST NOT trigger slice progression.

#### Scenario: Current slice audio finishes with auto-advance enabled
- **WHEN** audio narration for slice $N$ reaches completion and `autoAdvance` is true and slice $N+1$ exists
- **THEN** the reader automatically advances to slice $N+1$ and begins narration of slice $N+1$ without requiring the user to press Play or click Next.

#### Scenario: Auto-advance disabled at end of slice
- **WHEN** audio narration for slice $N$ finishes and `autoAdvance` is false
- **THEN** playback transitions to paused (`playing === false`), remaining on slice $N$.

#### Scenario: User pauses during System TTS playback
- **WHEN** System TTS is actively narrating a slice and `autoAdvance` is enabled
- **AND** the user clicks the "Pause" button (or triggers remote pause)
- **THEN** speech synthesis halts immediately
- **AND** the reader remains on the current slice without advancing to the next slice
- **AND** `auto-advance` is not emitted

#### Scenario: User switches engine while narration is playing
- **WHEN** narration is playing in System mode
- **AND** the user clicks the "Cloud" or "Device" engine toggle
- **THEN** System TTS and silent carrier halt cleanly
- **AND** the reader remains on the current slice without skipping to the next slice
- **AND** narration transitions to the newly selected engine

---

### Requirement: Web Speech API Sentence-Level Streaming Chunking

When synthesizing narration via System TTS (`engineMode === 'system'`), the reader SHALL segment the plain-text narration script into sentence-level chunks using punctuation-aware sentence boundaries (`splitSentences`):

1. **Input Size Bounds**: The reader SHALL NOT pass multi-thousand-character prose blocks to a single `SpeechSynthesisUtterance`. Each utterance text SHALL be bounded to individual sentence boundaries, strictly adhering to mobile platform input constraints.
2. **Sequential Sentence Progression**: The player SHALL queue and play sentence utterances sequentially via `utterance.onend`.
3. **Playback Lifecycle & Resume Synchronization**:
   - When paused, the active sentence index (`systemSentenceIndex`) SHALL be preserved.
   - Resuming from pause SHALL reset `isSystemStopped = false` and speak from `systemSentenceIndex`, continuing subsequent sentences smoothly until the slice ends.
   - Canceling an utterance during pause or engine switch SHALL NOT invoke `onSliceEnded`.
4. **Sentence Seeking**: When `seek(time)` is invoked in System mode, the system SHALL calculate the target sentence index from the requested time, halt active speech, update `systemSentenceIndex`, and begin speaking from that sentence.

#### Scenario: Long book slice is chunked for System TTS
- **WHEN** a user plays a 6,000-character reading slice using System TTS
- **THEN** the player splits the script into sentence-level utterances and plays them sequentially, preventing `getMaxSpeechInputLength` overflows and browser synthesis cutoff.

#### Scenario: User pauses and resumes in System mode
- **WHEN** System TTS is playing sentence 3 of 8
- **AND** the user pauses playback
- **AND** later clicks "Play"
- **THEN** narration resumes from sentence 3
- **AND** when sentence 3 completes, narration automatically continues through sentences 4 to 8

---

### Requirement: Fixed-Preset Sleep Timer and 15-Second Audio Fade-Out

The reader audio player SHALL provide a Sleep Timer control with fixed duration presets and a smooth audio fade-out mechanism.

1. **Fixed Presets**: The player SHALL offer selectable presets: 15 minutes (`15m`), 30 minutes (`30m`), 45 minutes (`45m`), 60 minutes (`60m`), and "End of Current Slice" (`end_of_slice`), plus an option to cancel/turn off.
2. **End-of-Slice Sleep Guard**: When `end_of_slice` is active, the sleep timer completion action (pausing playback, clearing timer, and notifying the user) SHALL trigger strictly upon natural slice completion. Manual user pauses, engine changes, or silent carrier teardown SHALL NOT trigger the sleep timer completion notification or dismiss the timer prematurely.

#### Scenario: Sleep timer reaches final 15 seconds
- **WHEN** an active 30-minute sleep timer counts down to 0:15 remaining
- **THEN** audio volume smoothly fades down from 1.0 to 0.0 over 15 seconds, and at 0:00 playback pauses without sudden auditory clipping.

#### Scenario: User selects End of Current Slice preset
- **WHEN** the user selects "End of Current Slice" as the sleep timer mode
- **THEN** the reader plays the remainder of the active slice, and upon reaching slice completion, halts playback without advancing to the next slice.

#### Scenario: User manually pauses with End of Slice sleep timer active
- **WHEN** the sleep timer is set to "End of Current Slice" (`end_of_slice`)
- **AND** the user clicks the "Pause" button mid-slice
- **THEN** playback pauses normally
- **AND** the sleep timer remains active (not dismissed)
- **AND** no "Sleep timer ended" toast is triggered
