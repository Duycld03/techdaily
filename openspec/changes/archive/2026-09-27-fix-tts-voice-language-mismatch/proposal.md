# Proposal

## Why

When switching to Google Cloud TTS mode, the client currently loads `selectedVoice` directly from `localStorage` (`techdaily_audio_voice`) without verifying that the persisted voice ID matches the language of the active document slice. If a user previously selected or listened with a Vietnamese voice (`vi-VN-Neural2-A`), switching to Cloud mode on an English document sends the Vietnamese voice ID to the backend synthesis API, resulting in English prose being synthesized using a Vietnamese voice model.

## What Changes

- **Per-Language Voice Preference Resolution**: Refactor `useSliceAudio.ts` so voice resolution strictly enforces language compatibility. When resolving the active Cloud voice, verify that `selectedVoice` starts with the required language prefix (`vi-` for Vietnamese, `en-` for English); if mismatched or absent, fall back to the default voice for the document's language.
- **Language-Scoped Voice Storage**: Store voice preferences keyed by language (e.g. `techdaily_audio_voice_vi` and `techdaily_audio_voice_en`) so user voice customizations for English and Vietnamese persist independently without clobbering each other.
- **Synchronized UI Dropdown Selection**: Ensure `ReaderAudioPlayer.vue` and `useSliceAudio.ts` share the identical voice resolution logic, so the dropdown reflects the exact voice ID transmitted to the synthesis backend.
- **Backend Voice-Language Validation**: In `GetOrSynthesizeChunkAudioHandler.cs`, validate that the requested `VoiceId` matches the document chunk's content language, rejecting cross-language voice requests with an informative domain error.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `audio-narration`: Update Google Cloud TTS voice selection requirements so that the voice ID selected for synthesis MUST match the slice content language (`en` slices only use `en-US-*` voices; `vi` slices only use `vi-VN-*` voices). Persisted voice preferences must be partitioned by language.

## Impact

- **Correctness**: Guarantees English technical documentation is never voiced using Vietnamese neural models, and vice versa.
- **User Experience**: Users can customize their preferred male/female voice for Vietnamese and English independently without cross-document interference.
