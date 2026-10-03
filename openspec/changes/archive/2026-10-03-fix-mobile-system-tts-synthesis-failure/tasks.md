# Tasks

## 1. Frontend: Audio Composable Sentence Chunking & Automatic Cloud Cascade

- [x] 1.1 In `frontend/composables/useSliceAudio.ts`, update `synthesizeOnSystem` to segment the narration script into sentence boundaries using `splitSentences(script)` and manage sequential sentence utterance playback queue via `utterance.onend`.
- [x] 1.2 In `frontend/composables/useSliceAudio.ts`, implement automatic runtime cascade to `synthesizeOnCloud` inside `utterance.onerror` whenever speech synthesis encounters a non-cancellation error (`synthesis-failed`, `synthesis-unavailable`, `language-unavailable`, `voice-unavailable`, or `audio-busy`), updating `engineMode` and triggering `onFallbackToCloud`.
- [x] 1.3 In `frontend/composables/useSliceAudio.ts`, isolate the silent carrier lifecycle so it cleanly resets without holding media audio focus when speech synthesis fails or completes.

## 2. Frontend: Audio Player UI Recovery & Error Diagnostics

- [x] 2.1 In `frontend/composables/useSliceAudio.ts`, update `categorizeAudioError` to return `suggestCloudFallback: true` and assign `SYSTEM_TTS_FAILED` code for errors originating from the `system` engine.
- [x] 2.2 In `frontend/components/reader/ReaderAudioPlayer.vue`, update `canFallbackToCloud` to evaluate to true for both `engineMode.value === 'device'` and `engineMode.value === 'system'` when an error occurs.
- [x] 2.3 Verify and update localized copy in `frontend/i18n/locales/en.json` and `vi.json` for seamless fallback notifications and clear diagnostic toast messaging across locales.

## 3. Frontend: Automated Unit & Visual Proof Verification

- [x] 3.1 In `frontend/tests/composables/useSliceAudio.spec.ts`, add unit tests covering sentence chunking progression for System TTS and automatic cascade to Cloud TTS upon `utterance.onerror` (`synthesis-failed`).
- [x] 3.2 In `frontend/tests/components/reader/ReaderAudioPlayer.spec.ts`, add component tests verifying that the 1-tap "Chuyển sang Google Cloud" fallback button renders and triggers Cloud playback when in `system` error state.
- [x] 3.3 Execute full frontend test suite (`npm test`) to guarantee 100% test pass rate across data contracts, audio lifecycles, and route guards.
- [x] 3.4 Execute automated headless Chromium visual inspection via browser device in `eval` across Desktop (1440x900) and Mobile (390x844) viewports for `/read/[bookId]` to verify zero visual element collisions or layout shifts in the player toolbar.
