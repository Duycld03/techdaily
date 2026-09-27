# Proposal

## Why

When reading technical slices in `/read/[bookId]`, toggling the narration engine between Google Cloud and On-Device causes an annoying visual jitter/jump (layout shift). This occurs because the Cloud engine introduces a voice selection dropdown (`AppSelect.vue`) whose rendered height (34px due to borders and text line-height) exceeds the adjacent "Listen" button and segmented control (30–32px), causing the parent reader audio container to expand from 50px to 52px and push down the slice content below.

## What Changes

- Standardize the rendered height of `AppSelect.vue` when `size="sm"` to exactly 32px (`h-8 box-border flex items-center`), ensuring the button trigger matches the 32px height of the "Listen" button (`h-8` / `py-1.5`).
- Enforce height normalization across interactive reader audio player controls (`ReaderAudioPlayer.vue`) so that toggling the voice select or switching between Cloud and Device engines produces zero container height change (`min-h-[50px]` / stable 50px or 52px total box height).
- Maintain WCAG accessibility and tap targets while eliminating cumulative layout shift (CLS) on reader views across Desktop and Mobile viewports.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `audio-narration`: Update the presentation and styling requirements for reader audio playback controls to guarantee zero container layout shift and equalized 32px control trigger heights between Cloud and Device modes.

## Impact

- Frontend: `frontend/components/common/AppSelect.vue`, `frontend/components/reader/ReaderAudioPlayer.vue`.
- User Experience: Completely eliminates content jumping when toggling narration engines in the reader.
