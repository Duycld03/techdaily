# Proposal: Reader Audio Player Mobile Responsive Layout

## Why

On mobile viewports (≤640px, e.g. 390px iPhone), the reader audio player currently forces all controls into a single row using competing `flex-1` classes. This causes severe UX defects:
1. When audio is idle/paused, an extraneous flex spacer competes with the voice selector, compressing the dropdown to ~70px and truncating voice labels to illegible stubs (e.g. `"N.. ⌵"`).
2. When audio is playing, the scrubber, timer, play button, engine toggle, voice dropdown, and speed button all compete for horizontal space, causing the voice selector to collapse into an empty squished pill or vanish completely (`0px` width).
3. The scrubber slider becomes dangerously cramped (<40px), making finger seeking difficult on touch devices.

A dedicated responsive layout is required to guarantee that voice selection labels remain fully legible without aggressive truncation on mobile, and the playback scrubber has ample touch-friendly seek space without crushing other player controls.

## What Changes

- **Responsive Mobile Layout Architecture in `ReaderAudioPlayer.vue`**:
  - Remove competing unconstrained `flex-1` spacers that cannibalize the voice selector on mobile.
  - When idle/paused on mobile, allocate available space to the voice picker (`min-w-[140px]` / `flex-1`) so voice names (e.g. `"Neural2-A"`, `"Standard-A"`, `"Chất lượng cao"`) display legibly without truncation.
  - When playing on mobile (`loadedId === chunk?.id && duration > 0`), transition the scrubber and timer into a dedicated full-width bottom track (`w-full flex items-center gap-2 pt-1`), keeping primary action controls (`Play`, `Toggle`, `Voice Picker`, `Speed`) stable on the top row.
  - On desktop/tablet screens (`sm:` and up, ≥640px), maintain the sleek single-row layout where all elements sit horizontally at `min-h-[50px]`.
- **Touch-Friendly Scrubber & Seek Ergonomics**:
  - Ensure the mobile scrubber track spans 100% width on row 2, providing smooth and accurate finger seeking.
- **Visual & Layout Stability**:
  - Prevent voice selector disappearance (`0px` width) or truncation to `N..`.
  - Maintain clean spacing, prevent button collisions, and ensure zero horizontal overflow on 360px–390px mobile screens.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `audio-narration`: Update reader audio player UI requirements for responsive mobile viewports, requiring full-width touch scrubber row during playback and minimum legible width for voice selectors in idle and active states.

## Impact

- `frontend/components/reader/ReaderAudioPlayer.vue`: Layout flex and grid restructuring for mobile vs desktop breakpoints.
- Zero breaking changes to public APIs, audio composables (`useSliceAudio.ts`), or backend services.
