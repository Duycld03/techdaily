# Tasks

## 1. Application Layer (Backend Validation)
- [x] 1.1 Update `GetOrSynthesizeChunkAudioHandler.cs` to validate that `request.VoiceId` matches `chunk.Language`, returning an error (`VOICE_LANGUAGE_MISMATCH`) if an English chunk is requested with a Vietnamese voice or vice-versa.
- [x] 1.2 Add backend unit tests in `TechDaily.Application.Tests` covering voice-language compatibility validation.

## 2. Frontend Layer (Voice Resolution & Language-Scoped Storage)

- [x] 2.1 Refactor `resolveCloudVoiceForLanguage` in `frontend/composables/useSliceAudio.ts` to strictly sanitize and validate custom voice IDs against the slice's content language prefix.
- [x] 2.2 Update `useSliceAudio.ts` to store and retrieve voice preferences partitioned by language (`techdaily_audio_voice_en` and `techdaily_audio_voice_vi`), preventing cross-language overwrites.
- [x] 2.3 Update `ReaderAudioPlayer.vue` so the voice selector cleanly syncs with language-scoped voice resolution when opening slices or toggling Cloud mode.

## 3. Testing & Verification

- [x] 3.1 Add unit tests in `frontend/tests/composables/useSliceAudio.spec.ts` asserting that an English slice never selects a Vietnamese voice even if `techdaily_audio_voice` in `localStorage` contains a Vietnamese voice.
- [x] 3.2 Run `dotnet test` and `npm test` to verify 100% test pass rate across backend and frontend suites.
