# Tasks

## 1. Frontend Audio Composable (`useSliceAudio.ts`)

- [x] 1.1 In `frontend/composables/useSliceAudio.ts`, implement stream underrun buffering with an `isWaitingForNextChunk` flag in `onChunkEndedCallback` and `onChunk`, ensuring that when audio finishes playing the current buffer while synthesis is still streaming, the player sets `status = 'loading'`, keeps `playing = true`, and automatically plays incoming chunks without stalling at the intermediate duration.
- [x] 1.2 In `frontend/composables/useSliceAudio.ts`, update `loadAndPlay(source, autoPlay = true)` to support `autoPlay = false`, preparing audio metadata, checking local/server cache, and setting duration without starting audible playback.
- [x] 1.3 In `frontend/composables/useSliceAudio.ts`, preserve worker synthesis during user pause by retaining `synthIndex` and `synthTotal` counters and allowing background generation to continue.

## 2. Frontend Player Presentation (`ReaderAudioPlayer.vue`)

- [x] 2.1 In `frontend/components/reader/ReaderAudioPlayer.vue`, update `onToggle()` so clicking "Listen" when on-device synthesis is incomplete (`synthIndex < synthTotal` or when only partial pre-roll is present) invokes `loadAndPlay(source.value)` to resume synthesis of the remaining sentences.
- [x] 2.2 In `frontend/components/reader/ReaderAudioPlayer.vue`, update `onToggleEngine` to call `loadAndPlay(source.value, autoPlay)` so switching engines while inactive prepares and loads the selected engine's audio without triggering audible autoplay.

## 3. Testing & Dual-Gate Verification

- [x] 3.1 Update unit tests in `frontend/tests/composables/useSliceAudio.spec.ts` verifying that stream underrun triggers buffering and automatically resumes on next chunk arrival, and that `autoPlay = false` prepares duration without starting audio.
- [x] 3.2 Update unit tests in `frontend/tests/components/reader/ReaderAudioPlayer.spec.ts` verifying that clicking "Listen" on an incomplete slice resumes synthesis, and that engine toggles prepare audio without autoplaying.
- [x] 3.3 Execute Gate 1 automated testing: verify 100% pass rate on `npm test` and `dotnet test`.
- [x] 3.4 Execute Gate 2 visual verification: drive headless Chromium via `browser` in `eval` to verify engine toggling and playback resumption on Desktop (1440x900) and Mobile (390x844).
