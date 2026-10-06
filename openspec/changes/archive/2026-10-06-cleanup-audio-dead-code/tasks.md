# Tasks

## 1. Application Layer (Backend Constant Centralization)

- [x] 1.1 In `backend/src/TechDaily.Application/Features/Library/SynthesizeAudio/GetAudioQuotaHandler.cs`, replace local literals with `GetOrSynthesizeChunkAudioHandler.MonthlyCharacterLimit` and `NearLimitThreshold`. Verify with `dotnet test`.

## 2. Frontend Store & Composable (State & Surface Pruning)

- [x] 2.1 In `frontend/stores/useLibraryStore.ts`, remove `audioQuota` ref, `fetchAudioQuota` action, and unused `AudioQuotaInfo` type import.
- [x] 2.2 In `frontend/composables/useSliceAudio.ts`, remove dead `toggle` and `setAutoAdvance` functions, unused `pitch` state and `setPitch` setter, and unused constants `AUDIO_PITCH_STORAGE_KEY` and `AUDIO_SLEEP_TIMER_STORAGE_KEY`.

## 3. Frontend Utilities & Component References

- [x] 3.1 In `frontend/utils/audioWav.ts`, remove unused export `wavDurationSeconds`.
- [x] 3.2 In `frontend/components/reader/ReaderAudioPlayer.vue`, import and use `AUDIO_VOICE_STORAGE_KEY` instead of hardcoded literal string.
- [x] 3.3 In `frontend/pages/read/[bookId].vue`, remove unused `audioPlayerRef` ref declaration and template ref binding.

## 4. Internationalization Catalogs

- [x] 4.1 In `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`, remove dead keys `reader.audio_pitch` and `reader.audio_auto_advance_disabled_today_hint`.

## 5. Test Suite Alignment & Verification

- [x] 5.1 In `frontend/tests/utils/audioWav.spec.ts`, remove test cases asserting `wavDurationSeconds`.
- [x] 5.2 In `frontend/tests/components/reader/ReaderAudioPlayer.spec.ts`, remove mock stubs for `setPitch`, `setAutoAdvance`, and translation key `reader.audio_pitch`.
- [x] 5.3 In `frontend/tests/composables/useSliceAudio.spec.ts`, remove tests asserting `player.setPitch` and import of `AUDIO_PITCH_STORAGE_KEY`.
- [x] 5.4 Execute full test suites (`npm test` in `frontend` and `dotnet test` in `backend`) to verify zero regressions.
