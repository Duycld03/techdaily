# Audio Narration Spec Delta

## MODIFIED Requirements

### Requirement: Continuous Cross-Slice Auto-Advance
The reader audio player SHALL provide an automatic slice progression mechanism that continues narration to subsequent document slices without manual user intervention.

1. **Auto-Advance Toggle**: The player SHALL render an "Auto Next" (`autoAdvance`) toggle control, defaulted to enabled (`true`) and persisted in `localStorage` (`deeppace_reader_audio_auto_advance`).
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

### Requirement: Lockscreen and Headset Media Session Control
The reader audio system SHALL integrate with the Web Media Session API (`navigator.mediaSession`) to provide background playback metadata and remote hardware controls.
1. **Metadata Synchronization**: When narration begins or advances, the system SHALL update `navigator.mediaSession.metadata` with:
   - `title`: Current slice title or slice section heading.
   - `artist`: Document or book title.
   - `album`: "DeepPace Reader".
   - `artwork`: Document cover thumbnail image.
2. **Remote Action Handlers**: The system SHALL register action handlers for:
   - `play`: Resumes audio playback or speech synthesis.
   - `pause`: Pauses active playback.
   - `nexttrack`: Advances to the next slice and initiates narration.
   - `previoustrack`: Rewinds to the previous slice or restarts the current slice.
3. **Background Persistence**: The media session SHALL remain active and responsive when the mobile screen is locked or when interacting via Bluetooth headphones/car audio controls.

#### Scenario: User clicks Next Track on Bluetooth headset
- **WHEN** narration is playing and the user presses the "Next Track" hardware button on their Bluetooth headphones
- **THEN** the media session `nexttrack` handler executes, advancing the reader to the next slice and beginning its narration.

#### Scenario: User pauses from lockscreen notification
- **WHEN** audio is playing and the user taps "Pause" on the Android lockscreen media notification
- **THEN** playback is paused and the lockscreen state updates to paused.

## ADDED Requirements

### Requirement: Canonical narration cache database
The on-device narration cache SHALL use `deeppace-audio` with its existing supported store/key schemas and eviction limits. Known legacy cache data SHALL be handled only by the shared client-storage migration. Ordinary playback SHALL not open the legacy database, and migration SHALL not invoke synthesis or change voice/content identity.

#### Scenario: Playback after cache migration
- **WHEN** a compatible legacy audio entry has transferred to the canonical database
- **THEN** the existing cache key identifies the same complete audio without re-synthesis.

#### Scenario: New cache write or partial entry
- **WHEN** narration caches a complete or partial result
- **THEN** it writes to the canonical database under the existing appropriate store/key schema
- **AND** current partial-cache validation and eviction behavior remain applicable.
