# Proposal

## Why

When initiating on-device speech narration on desktop and laptop browsers, users experience severe playback start latency, appearing stuck indefinitely on "Đang đệm audio... 0/12" ("Buffering audio... 0/12"). This degradation stems from three compounding defects:
1. **Status Label Shadowing**: In `ReaderAudioPlayer.vue`, the buffering status condition (`target > 0 && synthIndex < target`) is checked before `downloadProgress`, masking model weight downloads (~100 MB) behind a misleading "0/12" buffering message. Users perceive the player as completely frozen while weights are downloading.
2. **Excessive Buffer Target**: The pre-roll buffer target is currently computed as `Math.max(2, Math.ceil(sentences.length / 3))`. On a typical 36-sentence reading slice, this forces the user to wait for 12 complete sentences to synthesize before any audio plays. On CPU WASM, synthesizing 12 sentences can take 2 to 4 minutes before first sound.
3. **Compute Starvation & UI Property Omission**: Desktop WASM thread concurrency was capped, and client-side SPA navigation into `/read/...` can leave `crossOriginIsolated` false if navigating from unisolated pages, restricting CPU WASM to 1 thread. Furthermore, `device` was omitted from destructuring in `ReaderAudioPlayer.vue`, generating Vue template runtime warnings and preventing the compute device badge (`[GPU]` / `[CPU]`) from rendering.

## What Changes

- **Prioritize Model Download Progress**: In `ReaderAudioPlayer.vue`, evaluate `downloadProgress` before buffering so users see explicit download percentages (`Đang tải giọng đọc… X%` / `Downloading voice model… X%`) during the initial model fetch instead of an opaque "0/N" buffering counter.
- **Bound Pre-Roll Buffer Target to Immediate Playback Threshold**: Cap `targetBufferCount` to a low, latency-focused constant (`Math.min(2, sentences.length)` or at most 2 sentences, ~10-15s of speech). Initial playback starts within 3-5 seconds while background synthesis streams the remaining sentences seamlessly.
- **Restore Full Desktop WASM Thread Concurrency**: Remove artificial desktop thread caps and allow desktop browsers with cross-origin isolation to utilize full available multi-core CPU capacity (`Math.min(navigator.hardwareConcurrency || 4, 8)`), while maintaining single-threaded safety for mobile environments.
- **Destructure `device` in `ReaderAudioPlayer.vue`**: Add `device` back to the destructured bindings of `useSliceAudio` to eliminate Vue template warnings and render the `[GPU]` or `[CPU]` badge accurately.
- **Universal Cross-Origin Isolation for Reader**: Ensure cross-origin isolation headers (`COOP: same-origin`, `COEP: credentialless`) are consistently respected or applied so desktop users always achieve maximum multi-threaded WASM inference speed.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `audio-narration`: Update on-device speech synthesis buffering threshold, download progress presentation precedence, and desktop compute concurrency invariants.

## Impact

- **Frontend Reader**: `frontend/components/reader/ReaderAudioPlayer.vue`, `frontend/composables/useSliceAudio.ts`, `frontend/workers/ttsSynth.worker.ts`.
- **User Experience**: Immediate feedback when downloading voice models (percentage shown); speech begins within 3-5 seconds on desktop rather than waiting 2-4 minutes for 12 sentences to buffer.
- **Zero Breaking API Changes**: Retains existing Audio API contracts and IndexedDB schemas.
