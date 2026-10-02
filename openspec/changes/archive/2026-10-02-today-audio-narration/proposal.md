# Proposal: Integrate Audio Narration into Daily Focus (/today)

## Why
Currently, the dual-engine Audio Narration Player (`ReaderAudioPlayer.vue`) is exclusive to the dedicated book reader (`/read/[bookId]`), forcing users on `/today` to consume their daily learning slices purely through visual reading. Integrating the audio player directly into `/today`'s reading pane (`DocReaderPane.vue`) empowers software engineers to listen to daily focus reading slices on-device (offline neural MMS-TTS via WASM/WebGPU) or via Google Cloud TTS with male/female voices, with zero navigation friction.

## What Changes
- **TypeScript Store Contract Enhancement**: Add `isAiFormatted?: boolean` to the `DocumentChunk` interface in `frontend/stores/useDailyFocusStore.ts`, matching the existing backend payload (`GetTodayFocusResponse.DocumentChunkDto.IsAiFormatted`) and satisfying `ReaderAudioPlayer`'s `ChunkSummary` contract.
- **Daily Focus Audio Integration**: Mount `ReaderAudioPlayer.vue` inside `frontend/components/today/DocReaderPane.vue` directly beneath the chapter title heading (`h1`), providing immediate, zero-scroll access to playback controls, engine selection, speed settings, and timeline seeking across desktop and mobile viewports.
- **Slice Transition & Lifecycle Parity**: Ensure narration automatically pauses and resets when the user navigates across reading slices or switches books via the Pacer menu, and verify clean resource disposal (Web Worker termination, audio pause, URL revoking) upon component unmount.
- **Responsive & Design System Consistency**: Ensure player controls match the Dev-Learning Studio design language and maintain proper padding and visual rhythm in both Desktop dual-pane and Mobile stacked tabs.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `today-reader`: Add requirement for integrating the complete dual-engine Audio Narration Player directly into the daily reading pane (`DocReaderPane.vue`) on `/today`, supporting on-device neural synthesis and cloud voices with reactive slice synchronization and clean lifecycle management.

## Impact
- **Frontend Components & Stores**:
  - `frontend/stores/useDailyFocusStore.ts`: Interface alignment.
  - `frontend/components/today/DocReaderPane.vue`: Player mounting and styling.
  - `frontend/tests/components/DocReaderPane.spec.ts`: Unit test coverage.
- **Backend**: Zero changes required (`GetTodayFocusHandler.cs` already executes JIT AI formatting and serializes `IsAiFormatted`).
- **Dependencies**: Zero new runtime dependencies; reuses `useSliceAudio` and `ReaderAudioPlayer`.
