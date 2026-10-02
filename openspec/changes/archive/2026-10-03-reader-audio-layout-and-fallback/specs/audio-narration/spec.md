# Spec Delta: Reader Audio Layout, Cloud Fallback & Pacer Gating

## MODIFIED Requirements

### Requirement: Multi-Engine Audio Narration Toolbar Layout

The reader audio narration toolbar SHALL render using a container-resilient, two-row responsive layout that guarantees zero visual element collisions, zero text overlapping, and zero button clipping across mobile (390px), constrained split-pane containers (~550px), and unconstrained desktop viewports (1440px+).

1. **Row 1 (Primary Controls & Utilities)**:
   - The primary Play/Pause button SHALL display an icon and dynamic localized text ("Nghe" / "Listen" when idle or paused; "Tạm dừng" / "Pause" when actively playing).
   - An Engine Mode Segmented Switch SHALL allow switching between `System` (Web Speech API), `Cloud` (Google Cloud TTS), and `Device` (On-Device Neural Web Worker).
   - Utility controls (Auto-advance toggle, Sleep Timer dropdown, and Speed cycle button) SHALL be grouped compactly on the right side of Row 1.
2. **Row 2 (Voice Selection & Timeline / Status)**:
   - The context-aware Voice Selector (`AppSelect`) SHALL display voices appropriate to the active engine (System voices for `system`, Cloud voices for `cloud`).
   - For audio streams with known media duration (`duration > 0`), Row 2 SHALL host the seekable slider track and timestamp indicator (`currentTime / duration`).
   - For Web Speech API playback (`duration === 0`), Row 2 SHALL host an active synthesis status banner indicating browser voice playback rather than collapsing or rendering blank space.
3. **Debug Tier Badge Removal**:
   - The internal debug cascade tier badge (`Bậc: {tier}` / `Tier: {tier}`) SHALL NOT be rendered in the DOM or visible user interface.

#### Scenario: Audio player rendered inside constrained split-pane container
- **WHEN** the reader is viewed in a constrained container such as the Daily Studio two-pane layout (~550px width)
- **THEN** all controls across Row 1 and Row 2 remain fully visible and accessible without horizontal scrollbars, text clipping, or overlapping controls.

#### Scenario: Play button text reflects active playback state
- **WHEN** audio playback starts using either Cloud TTS, On-Device TTS, or Web Speech API
- **THEN** the main play button label dynamically updates to "Tạm dừng" (or "Pause" in English locale), and updates back to "Nghe" (or "Listen") upon pause or completion.

---

### Requirement: Graceful Speech Engine Fallback & Notification

When a user initiates playback using the `System` engine (Web Speech API) and the client environment lacks compatible voices for the slice language (or speech synthesis is entirely unavailable):

1. The reader SHALL automatically switch the active engine mode to `Cloud`.
2. The UI Segmented Switch SHALL immediately update its active state to highlight `Cloud`.
3. The Voice Picker SHALL switch to the corresponding Cloud voice selection for the slice language.
4. The system SHALL display a toast notification informing the user that the engine was transitioned to Cloud TTS due to unavailable browser voices.
5. **Storage Invariant**: Client-side synthesis (Web Speech API and on-device Web Worker) SHALL NEVER send synthesized audio payloads, audio chunks, or character tracking records to the backend PostgreSQL database (`DocumentChunkAudios`). Only Cloud TTS synthesis requests handled by the backend server SHALL persist records to `DocumentChunkAudios`.

#### Scenario: Web Speech API has no compatible voice for slice language
- **WHEN** a user with `engineMode = 'system'` plays a slice whose language has no matching voices in `window.speechSynthesis.getVoices()`
- **THEN** the engine mode automatically switches to `'cloud'`, the UI toggle updates to "Cloud", a toast notification is displayed, and audio synthesizes via the Cloud API.

#### Scenario: No backend database persistence for client-side audio
- **WHEN** audio is played via `system` (Web Speech API) or `device` (On-Device Neural TTS)
- **THEN** no POST request is issued to `/api/v1/library/chunks/{chunkId}/audio` and zero rows are inserted into `DocumentChunkAudios`.

---

### Requirement: Daily Pacer Workflow Auto-Advance Gating

In the Daily Learning Pacer workflow (`/today`), completion of slice audio narration SHALL NOT trigger automatic advancement to the next slice, regardless of the `autoAdvance` setting. 

1. Completing the daily learning milestone strictly requires the user to interact with and complete the Senior Challenge drill for the day's slice.
2. In the standalone book reader (`/read/[bookId]`), completion of slice audio narration SHALL continue to auto-advance to the next slice when the user has enabled the `autoAdvance` preference.

#### Scenario: Audio narration finishes on Today page
- **WHEN** audio playback completes for the current slice on `/today`
- **THEN** the reader remains on the current slice and does not advance to the next slice, allowing the user to solve the Senior Challenge drill.

#### Scenario: Audio narration finishes on standalone Book Reader page
- **WHEN** audio playback completes for the current slice on `/read/[bookId]` with `autoAdvance` enabled
- **THEN** the reader automatically navigates to and starts playback for the next slice in the chapter.
