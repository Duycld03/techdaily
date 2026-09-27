# Proposal

## Why

In `ReaderAudioPlayer.vue`, switching to Cloud narration mode reveals a voice selector dropdown (`AppSelect.vue`). The default `AppSelect` trigger has generous form-input vertical padding (`py-2.5`, resulting in ~42px height), whereas the adjacent "Listen" action button uses a compact button style (`py-1.5 text-sm`, ~32px height) and the segmented switch is ~28px tall. This height discrepancy unevenly expands the entire reader audio player container whenever Cloud mode is active, disrupting visual consistency.

## What Changes

- Add a `size` prop (`'sm' | 'md'`, defaulting to `'md'`) to `AppSelect.vue` allowing compact rendering with `py-1.5 text-xs sm:text-sm` and `rounded-xl`.
- Update `ReaderAudioPlayer.vue` to configure the voice selection `AppSelect` with `size="sm"` so its rendered height matches the adjacent "Listen" button.
- Ensure the floating listbox dropdown positioning and styling remain fully accessible, legible, and visually balanced in compact mode.

## Capabilities

### New Capabilities
<!-- None -->

### Modified Capabilities
- `audio-narration`: Align the Cloud voice selector control height and typography with the compact ReaderAudioPlayer action button height.

## Impact

- **Frontend Components:**
  - `frontend/components/common/AppSelect.vue`: Add optional `size` prop (`'sm' | 'md'`).
  - `frontend/components/reader/ReaderAudioPlayer.vue`: Pass `size="sm"` to `AppSelect`.
- **Backend/API:**
  - No changes required.
