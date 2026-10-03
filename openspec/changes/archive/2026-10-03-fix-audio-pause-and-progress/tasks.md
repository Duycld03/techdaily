# Tasks

## 1. Frontend: Audio Composable (`useSliceAudio`)

- [x] 1.1 Isolate silent carrier audio element events: update `stopSilentCarrier()` and guard the `ended` event listener in `ensureAudio()` so the zero-duration silent carrier never triggers `deps.onSliceEnded?.()` upon pause, engine switch, or loop termination.
- [x] 1.2 Implement duration estimation and real-time elapsed time tracking for System TTS mode in `useSliceAudio.ts` based on sentence character counts and speech playback speed.
- [x] 1.3 Implement sentence-level seeking in `seek(time)` for System TTS mode, mapping requested seconds to the target sentence index, updating `currentTime`, and resuming speech from the target sentence without audio collision.
- [x] 1.4 Refactor System TTS pause and resume lifecycle state machine: ensure `isUserPaused` and `isSystemStopped` are correctly cleared upon `play()`, preserving `systemSentenceIndex` upon pause and restarting speech seamlessly from the active sentence.
- [x] 1.5 Add `initialOffset` parameter to `loadAndPlay()`, propagating elapsed time to `setSource()` for Cloud/Device engines and calculating the starting sentence index for System TTS mode.

## 2. Frontend: Audio Player Component (`ReaderAudioPlayer`)

- [x] 2.1 Update `ReaderAudioPlayer.vue` scrubber rendering conditions so the interactive seekable slider track, current time, and total duration display when System TTS mode is ready or playing, replacing the static text banner.
- [x] 2.2 Update `onToggleEngine()` in `ReaderAudioPlayer.vue` to capture `currentTime.value` and pass it as `initialOffset` into `loadAndPlay()`, preserving playback position across engine switches.
- [x] 2.3 Guard `onSliceEnded` and `setSleepTimer()` in `ReaderAudioPlayer.vue` so that the `end_of_slice` sleep timer triggers only upon genuine slice completion, preventing premature toast notifications or timer clearing on manual pause.

## 3. Frontend: Verification & Testing

- [x] 3.1 Update unit tests in `frontend/tests/composables/useSliceAudio.spec.ts` to test silent carrier pause isolation, System TTS seeking, sentence-level resume without stall, and cross-engine `loadAndPlay` offset.
- [x] 3.2 Update unit tests in `frontend/tests/components/reader/ReaderAudioPlayer.spec.ts` to test scrubber visibility in System mode, progress preservation during engine toggle, and sleep timer pause protection.
- [x] 3.3 Run `npm test` across all affected test suites and conduct headless browser verification across Desktop and Mobile viewports.
