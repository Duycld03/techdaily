# Proposal

## Why

When reading technical document slices with On-Device narration, users observe that the player duration displays only the length of the currently buffered pre-roll audio (e.g. `0:00 / 2:08` for 18 synthesized sentences) rather than the complete reading slice, whereas Google Cloud displays the full audio duration (e.g. `0:00 / 6:42` across all 67 sentences).

This leads to user confusion:
1. Users believe on-device narration is truncated, stopped, or missing the latter two-thirds of the document.
2. The UI does not clearly communicate that on-device narration uses streaming sentence-by-sentence synthesis, where 2:08 is immediately playable while the remaining 49 sentences continue synthesizing in the background.
3. The player does not show the total estimated duration or clear synthesis progress when paused or prepared, leaving users wondering how to "make it run to the end".

## What Changes

- **Total Estimated Duration Indicator**: While on-device synthesis is in streaming mode (`synthIndex < synthTotal`), the player calculates and surfaces the estimated total slice duration based on average sentence duration or slice word count (e.g. `0:00 / 2:08 (~6:40)` or dynamic duration expansion), making it immediately clear that 2:08 is only the buffered portion of a 6-minute slice.
- **Always-Visible Synthesis Progress Status**: In `ReaderAudioPlayer.vue`, display the streaming progress indicator (e.g. `Đang tạo 18/67 câu...`) whenever on-device audio is partially synthesized (`synthIndex < synthTotal`), both during playback and while paused/prepared, reassuring the user that the worker will continue generating to the end.
- **Seamless Chunk Continuation**: Ensure that when the user clicks "Listen", playback starts immediately with the 2:08 pre-roll while background synthesis actively streams sentences 19 through 67, seamlessly extending playback to the end of the 6-minute document without stalling.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `audio-narration`: Display estimated total slice duration and persistent synthesis progress during on-device streaming narration; ensure uninterrupted continuation through the entire slice duration.

## Impact

- **Frontend Player & Composable**: `frontend/components/reader/ReaderAudioPlayer.vue`, `frontend/composables/useSliceAudio.ts`.
- **Zero Breaking Contract Changes**: 100% backward compatible with existing cache keys and playback APIs.
