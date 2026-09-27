# Tasks

## 1. Frontend Audio Composable (`useSliceAudio.ts`)

- [x] 1.1 In `frontend/composables/useSliceAudio.ts`, update `setSource(blob, initialOffset = 0)` to seek `el.currentTime = initialOffset` reliably via `loadedmetadata` listener (or immediately if `readyState >= 1`).
- [x] 1.2 In `frontend/composables/useSliceAudio.ts`, update `play()` to detect if `el.ended` or `el.currentTime >= el.duration` and reset `currentTime = 0`, allowing smooth playback restart when clicked from an ended state.
- [x] 1.3 In `frontend/composables/useSliceAudio.ts`, pass `offsetSec` to `setSource(complete, offsetSec)` during background completion so the full assembled audio preserves the exact playback timestamp without resetting to 0.
- [x] 1.4 In `frontend/composables/useSliceAudio.ts`, update `playPreRoll` to assemble all `buffers.length` available chunks when restoring from partial cache (`playPreRoll(buffers.length)`) and set `currentPlayingIndex = buffers.length - 1` so all restored sentences play seamlessly before subsequent worker chunks chain in.

## 2. Frontend Player Presentation (`ReaderAudioPlayer.vue`)

- [x] 2.1 In `frontend/components/reader/ReaderAudioPlayer.vue`, update `onToggleEngine(mode, autoPlay = playing.value)` so switching engines while paused does NOT call `loadAndPlay()`, leaving playback stopped and resetting stale timer displays.
- [x] 2.2 In `frontend/components/reader/ReaderAudioPlayer.vue`, update `onFallbackToCloud()` to pass `autoPlay = true` to maintain immediate Cloud playback when explicitly activated from the error recovery button.

## 3. Testing & Visual Verification

- [x] 3.1 Update unit tests in `frontend/tests/components/reader/ReaderAudioPlayer.spec.ts` asserting that toggling engines while paused does not trigger `loadAndPlay()` and resets time display, whereas toggling while playing continues playback.
- [x] 3.2 Update unit tests in `frontend/tests/composables/useSliceAudio.spec.ts` verifying `setSource` offset preservation via `loadedmetadata`, `play()` restart on ended audio, and comprehensive partial cache restoration (assembling all 5/15 chunks into pre-roll with remaining sentence synthesis).
- [x] 3.3 Execute Gate 1 automated testing: verify 100% pass rate on `npm test` and `dotnet test`.
- [x] 3.4 Execute Gate 2 visual verification: drive headless Chromium via `browser` in `eval` to verify engine toggling while paused, playing resume, and time updates on Desktop (1440x900) and Mobile (390x844).
