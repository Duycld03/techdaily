# Proposal

## Why

When on-device TTS triggers playback immediately on the very first sentence chunk (chunk 0), a buffer underrun occurs whenever chunk 0 is brief (e.g. a 1-second heading or short introductory phrase). The audio element reaches the end of chunk 0 before chunk 1 finishes neural synthesis on the worker, causing playback to stall, stutter, or glitch.

Establishing a healthy pre-roll buffer threshold (at least 33% of the slice's sentences or a minimum duration buffer) before starting initial playback ensures continuous, glitch-free audio while still avoiding the previous 8-minute full-slice waiting penalty.

## What Changes

- **Pre-Roll Playback Buffer Threshold**: In `useSliceAudio.ts`, instead of initiating playback on chunk 0 (`buffers.length === 1`), establish an adaptive buffer threshold: `targetBufferCount = Math.max(1, Math.min(synthTotal, Math.ceil(synthTotal / 3)))`.
- **Pre-Roll Audio Assembly**: When `buffers.length >= targetBufferCount` is reached, concatenate the accumulated chunks into the initial playable audio segment and start playback smoothly.
- **Glitch-Free Sequential Chaining**: Continue background synthesis for remaining chunks, appending them to the playback queue so speech plays continuously without buffer starvations or restarts.
- **Unified Full-Slice WAV Cutover**: Maintain seamless transition to the complete assembled WAV upon full synthesis completion (`done`), persisting to IndexedDB (`techdaily-audio`).

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `audio-narration`: Update the progressive playback requirement in `audio-narration` to specify pre-roll buffering (buffering at least 33% of the slice's sentences or full completion for small slices) before starting audible playback, preventing audio underruns and playback stutter.

## Impact

- **User Experience**: Eliminates audible stuttering and restarts during on-device narration while keeping start latency under ~30 seconds (down from 8 minutes for a full slice).
- **Audio Quality**: Guarantees uninterrupted, smooth speech output across varying sentence lengths and CPU speeds.
- **Regression Safety**: Maintains complete compatibility with Google Cloud TTS, IndexedDB caching, and playback controls.
