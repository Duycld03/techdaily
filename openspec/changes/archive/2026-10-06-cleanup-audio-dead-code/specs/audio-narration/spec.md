# Spec Delta

## MODIFIED Requirements

### Requirement: Reader Audio Playback Controls

The `/read/[bookId]` reader SHALL provide an audio playback control that lets the user listen to the current slice's narration, available only when the current slice is AI-formatted (hidden or disabled otherwise). Playback SHALL support play, pause, and position seeking across all engine modes (Cloud, Device, and System).

1. **Universal Scrubber Slider**: The reader audio player SHALL render an interactive seekable scrubber slider for all active engine modes whenever slice narration is ready or playing.
2. **System Mode Scrubber & Seeking**: In System TTS mode (`engineMode === 'system'`), the player SHALL compute an estimated total duration and real-time elapsed time from sentence lengths and speech speed, rendering the scrubber slider and elapsed/total time (`mm:ss / mm:ss`). Users SHALL be able to drag or click the slider to seek to any sentence in the slice without restarting from the beginning.
3. **Action Labels**: The play/pause toggle button SHALL render concise, balanced action labels symmetrical across playback states and locales: "Listen" in English and "Nghe" in Vietnamese when idle/paused, and "Pause" in English and "Tạm dừng" in Vietnamese when actively playing. Accessible descriptions (`aria-label`) SHALL communicate the full descriptive action.

The reader SHALL offer a **playback speed** control spanning 0.5x to 2.0x applied client-side to the `<audio>` element's `playbackRate` **without re-synthesizing audio**; the selected speed SHALL persist across sessions and slices.

The audio player controls layout SHALL ensure that the primary playback controls (play/pause toggle, engine mode switch) and utility controls (auto-advance toggle, sleep timer menu, and speed selector) remain fully visible and contained within the player card boundaries on mobile viewports down to 360px width during both idle and playing states, preventing any control from overflowing or being pushed outside the card.

The playback control API surface SHALL encapsulate playback state (status, current time, duration, speed, volume, and quota) directly within the reader audio orchestrator composable. Generic library or document stores SHALL NOT maintain dead audio quota fields or unused audio fetch actions. The playback control SHALL NOT expose unused pitch adjustments, unreferenced storage keys, or redundant toggle abstractions.

While the language model is downloading, the reader SHALL display the download progress as an accurate whole-number percentage in the inclusive range **0 to 100**; it SHALL NOT display a value greater than 100% or otherwise mis-scaled. While synthesizing after download, the reader SHALL display a synthesis progress state.

All audio control labels, speed labels, and download/synthesis progress messages SHALL render localized text (en/vi) from the i18n catalog, and the catalog SHALL contain strictly active user-facing audio keys without orphaned keys for unrendered pitch settings or unused drill progression hints. The controls SHALL follow the reader's responsive typography and `whitespace-nowrap shrink-0` layout invariants across both locales. The reader SHALL NOT use native browser dialogs for audio state; status is conveyed via in-page controls and `useToast()`.

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

#### Scenario: Audio playback controls contain only active user interactions
- **WHEN** a user opens the reader audio player on an AI-formatted slice
- **THEN** the player card renders controls for play/pause, engine switching (System/Cloud/Device), voice selection, playback speed, auto-advance, and sleep timer
- **AND** no pitch adjustment controls or orphaned UI hooks are rendered.

#### Scenario: Audio quota state is encapsulated in the playback orchestrator
- **WHEN** the reader queries or evaluates cloud audio narration quota
- **THEN** the request and state management occur directly within the reader audio composable
- **AND** the general library store (`useLibraryStore`) does not maintain redundant quota state or fetch actions.

#### Scenario: Clean i18n audio catalog without dead keys
- **WHEN** inspecting the reader localization messages in English and Vietnamese
- **THEN** all defined audio message keys map directly to active player controls, toasts, or progress indicators.
