# Proposal

## Why

Readers should get reliable, free browser speech before spending Cloud quota or downloading a local model, but engine fallback currently becomes a persisted preference and some failures stop the cascade. Reloading also loses the listening position, while the existing cross-engine timestamp contract incorrectly treats different narrations as a shared timeline.

## What Changes

- Make automatic selection prefer Web Speech API, then Google Cloud TTS, then local neural TTS. Await bounded browser voice discovery before deciding that the requested language is unavailable; load only the chosen engine.
- Separate explicit engine preference from the effective engine used by a playback session. An automatic fallback must not make Cloud or local TTS the next session's default.
- **BREAKING behavior:** restart the current slice from the beginning whenever the effective engine changes, including automatic fallback. Remove cross-engine second/percentage conversion rather than claiming exact content alignment.
- Remember listening progress on this browser for the same account, slice, engine, voice, and narration content. Restore System speech at a sentence boundary, complete cached audio at an elapsed second, and partial local audio using its sentence/global-offset context.
- Restore only after the user requests playback; reloading, mounting, choosing an engine while paused, and discovering a checkpoint must not generate audio or autoplay. Preserve the existing intentional auto-advance behavior after natural completion in the standalone reader, not on `/today`.
- Keep playback checkpoints separate from IndexedDB audio/model caches. Cached synthesis can resume generation without being mistaken for a listening bookmark; invalid or unavailable storage must not prevent listening.
- Fence canceled sessions and checkpoint writes so delayed speech, fetch, media, worker, or account-change events cannot start an obsolete track, overwrite another session's progress, or advance the reader.
- Retain explicit System/Cloud/Device choices and add an Auto choice. Reset the old ambiguous engine-selection value to Auto through a scoped storage transition; preserve unrelated preferences and existing audio caches.

## Capabilities

### New Capabilities

None. These behaviors belong to existing narration and browser-storage boundaries.

### Modified Capabilities

- `audio-narration`: System-first resolution, explicit versus effective engine selection, restart-on-engine-change semantics, session cancellation, and engine-specific listening-position restoration without autoplay.
- `client-storage`: registered, versioned engine preference and account-scoped playback checkpoints, safe retirement of the ambiguous old engine-selection value, bounded persistence, and failure/account-isolation behavior.

## Impact

- Frontend orchestration and controls: `frontend/app/composables/useSliceAudio.ts`, `frontend/app/components/reader/ReaderAudioPlayer.vue`, and focused policy/progress helpers.
- Browser storage: `frontend/app/utils/clientStorage.ts`, the existing centralized startup/migration boundary, and its tests. Existing `deeppace-audio` Blob/partial cache keys and model caching remain compatible.
- Integration: existing reader and daily-view consumers remain on their current route, bookmark, access-control, pacer, sleep-timer, and natural-completion contracts. No new automatic navigation or server learning completion is introduced by restoring a checkpoint.
- Verification: behavioral regression coverage in existing audio suites, focused persistence cases, and real-browser reload/fallback playback checks plus English/Vietnamese desktop/mobile visual evidence during implementation.
- Documentation: update the audio descriptions in `README.md`, which currently advertise a different engine order and unconditional offline/zero-latency behavior.
- No backend endpoints, DTOs, provider, database migrations, credentials, dependencies, cross-device synchronization, or background audio pre-generation are added. Existing backend Cloud cache/quota security findings are not fixed or represented as resolved by this frontend change.
