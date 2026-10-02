# Proposal: Reader Audio Layout, Cloud Fallback & Pacer Gating

## Why

The reader audio narration player (`ReaderAudioPlayer.vue`) experiences critical UI collisions, text overlapping, and button clipping across both the Daily Drill view (`/today`) and the standalone reader (`/read/[bookId]`) because 8 controls are crammed into a single non-wrapping flex row. 

Furthermore, when the Web Speech API lacks matching system voices (such as on Linux/Chromium installations without system speech daemons), the player presents an internal debug badge (`Bậc: cloud`) rather than gracefully auto-switching the engine toggle to Cloud. When playing via the Web Speech API, the entire audio timeline disappears because `duration === 0` and the button remains labeled "Nghe", making playback feel invisible and detached. Additionally, completion of narration in `/today` should never auto-advance slices, as completing today's pacer strictly requires solving the interactive Senior Challenge drill.

## What Changes

- **Responsive Two-Row Player Architecture**: Restructure `ReaderAudioPlayer.vue` into a container-resilient, two-row layout:
  - **Row 1 (Primary & Utilities)**: Play/Pause action button, Engine Mode Segmented Switch (`System` / `Cloud` / `Device`), and compact utility controls (`Auto-advance`, `Sleep Timer`, `Playback Speed`).
  - **Row 2 (Playback & Scrubber)**: Context-aware Voice Picker (`AppSelect`) and Seekable Scrubber Slider / Active Speech Synthesis status banner.
  - Guarantees zero text collisions, zero button clipping, and seamless responsiveness across mobile (390px), split-pane workbench (550px), and full-width desktop (1440px+).
- **Deprecate `Bậc: {tier}` Badge & Seamless Cloud Fallback**: Remove the `reader.audio_cascade_tier` badge and its text string from the DOM and UI. When system voices are missing or Web Speech API synthesis fails:
  - Automatically flip `engineMode` state to `'cloud'`, updating the segmented toggle highlight and switching the voice dropdown to Cloud voices.
  - Trigger a concise user toast (`useToast()`) notifying the user that the player transitioned to Cloud TTS due to unavailable browser voices.
- **Client vs Backend Storage Invariant**: Explicitly codify and uphold the boundary that Web Speech API and On-Device neural TTS never send audio records or payloads to the backend PostgreSQL database (`DocumentChunkAudios`). On-device synthesis caches exclusively into browser IndexedDB (`idb-keyval`), while backend storage is strictly reserved for Google Cloud TTS MP3 files.
- **Web Speech API Visual Feedback**: When playing via Web Speech API (`duration === 0`):
  - Toggle the main action button text dynamically between "Nghe" (Listen) and "Tạm dừng" (Pause) alongside the icon switch.
  - Display an active browser speech indicator with the active voice name instead of hiding the entire scrubber and leaving a blank space.
- **Pacer Slice Advance Gating in `/today`**: In `/today` (`DocReaderPane.vue`), disable or intercept audio-driven auto-advancement so narration completion does not jump slices. The user must complete the accompanying Senior Challenge question to conclude the day's pacer. Retain auto-advance functionality for standalone study in `/read/[bookId]`.

## Capabilities

### Modified Capabilities
- `audio-narration`: Update player layout specifications to require a two-row responsive structure, mandate automatic engine toggle updates and toast notifications on browser voice fallback, specify Web Speech API active playing states, and restrict auto-advance behavior within the Daily Pacer workflow.

## Impact

- **Frontend**:
  - `components/reader/ReaderAudioPlayer.vue`: Layout redesign, remove tier badge, handle automatic engine flipping with toast, support Web Speech active states.
  - `components/today/DocReaderPane.vue`: Prevent audio auto-advance from switching slices in pacer mode.
  - `pages/read/[bookId].vue`: Ensure auto-advance continues to work smoothly for book reading.
  - `i18n/locales/{en,vi}.json`: Add fallback toast messages and dynamic button labels; remove obsolete cascade tier keys.
- **Backend & Database**: No changes required. The existing endpoints (`POST /api/v1/library/chunks/{chunkId}/audio`) and entities continue to serve Cloud TTS without alteration.
