# Spec Delta

## MODIFIED Requirements

### Requirement: Reader Audio Playback Controls

The `/read/[bookId]` reader SHALL provide an audio playback control that lets the user listen to the current slice's narration, available only when the current slice is AI-formatted (hidden or disabled otherwise). Playback SHALL use an HTML5 `<audio>` element supporting play, pause, and position seeking over the assembled complete slice audio.

The play/pause toggle button SHALL render concise, balanced action labels symmetrical across playback states and locales: "Listen" in English and "Nghe" in Vietnamese when idle/paused, and "Pause" in English and "Tạm dừng" in Vietnamese when actively playing. Accessible descriptions (`aria-label`) SHALL communicate the full descriptive action (e.g. "Play narration" / "Phát giọng đọc" and "Pause narration" / "Tạm dừng giọng đọc") to assistive technologies without bloating visible button dimensions.

The reader SHALL offer a **playback speed** control spanning 0.5x to 2.0x applied client-side to the `<audio>` element's `playbackRate` **without re-synthesizing audio**; the selected speed SHALL persist across sessions and slices. The reader SHALL NOT present a voice picker, since the voice is chosen automatically by slice language.

The audio player controls layout SHALL ensure that the primary playback controls (play/pause toggle, engine mode switch) and utility controls (auto-advance toggle, sleep timer menu, and speed selector) remain fully visible and contained within the player card boundaries on mobile viewports down to 360px width during both idle and playing states, preventing any control from overflowing or being pushed outside the card.

While the language model is downloading, the reader SHALL display the download progress as an accurate whole-number percentage in the inclusive range **0 to 100**; it SHALL NOT display a value greater than 100% or otherwise mis-scaled. While synthesizing after download, the reader SHALL display a synthesis progress state.

All audio control labels, speed labels, and download/synthesis progress messages SHALL render localized text (en/vi) from the i18n catalog, and the controls SHALL follow the reader's responsive typography and `whitespace-nowrap shrink-0` layout invariants across both locales. The reader SHALL NOT use native browser dialogs for audio state; status is conveyed via in-page controls and `useToast()`.

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
