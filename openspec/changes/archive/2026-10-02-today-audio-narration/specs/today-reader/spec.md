# Spec Delta: today-reader

## ADDED Requirements

### Requirement: Daily Focus Audio Narration Player Integration
The daily reading pane (`DocReaderPane.vue`) on `/today` SHALL integrate the complete dual-engine Audio Narration Player (`ReaderAudioPlayer.vue`), enabling software engineers to listen to AI-curated daily focus reading slices in-place without navigating away to `/read/[bookId]`.

1. **Gating on AI Curation (`isAiFormatted`):**
   - The audio player SHALL be available and render active playback controls whenever the active slice is AI-formatted (`chunk.isAiFormatted === true`).
   - If the active slice is not yet AI-formatted (`isAiFormatted === false`), the audio playback container SHALL NOT render active playback controls, preserving the distraction-free reading experience.

2. **Dual-Engine Audio Support (Device & Cloud):**
   - **On-Device Neural TTS:** The player SHALL support client-side on-device neural synthesis via Web Worker (WASM / WebGPU), streaming synthesis with a low-latency 2-sentence pre-roll buffer, and full-slice IndexedDB audio caching.
   - **Google Cloud TTS:** The player SHALL support cloud voice synthesis via the backend API, providing female and male voice options for English (`en`) and Vietnamese (`vi`), and displaying remaining quota feedback.
   - **Controls & Speed:** The player SHALL support speed adjustment ($0.75\times$ to $2.0\times$), play/pause toggle, and seekable progress timeline.

3. **Prominent Zero-Scroll Header Placement:**
   - The audio player container SHALL be positioned directly beneath the chapter title (`h1`), preceding the summary callout and markdown deep-dive content.
   - On both desktop dual-pane and mobile stacked layouts, the player controls SHALL be immediately accessible without requiring vertical scrolling past the executive summary or key takeaways.

4. **Slice Navigation & Track Switch Reset:**
   - When the user navigates between slices (e.g. Next Slice, Previous Slice) or switches document books via the Pacer menu, the audio player SHALL immediately pause active playback and reset the audio buffer state for the incoming slice.

5. **Resource Lifecycle & Route Exit Teardown:**
   - When the user navigates away from `/today` to any other view (`/review`, `/quiz`, `/notes`), the audio playback SHALL stop immediately, revoke any active object URLs, and terminate background Web Workers to prevent audio persistence or resource leakage.

#### Scenario: User listens to AI-formatted daily focus slice on /today
- **GIVEN** an authenticated user is viewing `/today` with an AI-curated reading slice (`chunk.isAiFormatted === true`)
- **WHEN** the user inspects the daily reading pane
- **THEN** the audio narration player renders directly below the chapter title heading
- **AND** the user can toggle playback between On-Device and Cloud TTS engines and listen to the slice.

#### Scenario: Audio player omitted for uncurated slice
- **GIVEN** a reading slice that has not completed AI formatting (`chunk.isAiFormatted === false` or undefined)
- **WHEN** the user views the reading pane on `/today`
- **THEN** the audio player controls are hidden or unavailable.

#### Scenario: Switching reading slices pauses audio and resets buffer
- **GIVEN** narration is currently playing for slice $N$ on `/today`
- **WHEN** the user navigates to slice $N+1$ or switches to a different book in the Pacer menu
- **THEN** audio playback for slice $N$ halts immediately
- **AND** the player resets its loaded state, ready to load or synthesize slice $N+1$.

#### Scenario: Leaving /today disposes audio playback and background workers
- **GIVEN** narration audio is actively playing or synthesizing on `/today`
- **WHEN** the user navigates to another page (such as `/review` or `/library`)
- **THEN** audio playback ceases immediately
- **AND** the synthesis Web Worker and media object URLs are disposed of cleanly.
