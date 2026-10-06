# Proposal

## Why

The reader audio narration system has accumulated orphaned state, dead methods, unused constants, and redundant code across the Pinia library store, composable, utilities, and translations after successive feature iterations. Pruning this dead weight clarifies architectural boundaries, reduces cognitive overhead, and improves maintainability while preserving 100% of the active three-tier audio playback experience.

## What Changes

- **Pinia Library Store Pruning**: Remove orphaned `audioQuota` ref, `fetchAudioQuota` action, and unused `AudioQuotaInfo` type import from `useLibraryStore.ts`. The actual audio player orchestrates its own quota state directly via `useSliceAudio.ts`.
- **Composable Surface Cleanup**:
  - Remove dead `toggle(source)` helper from `useSliceAudio.ts` (component manages toggle logic directly via `play`, `pause`, and `loadAndPlay`).
  - Remove unexposed `pitch` state, `setPitch` setter, and `AUDIO_PITCH_STORAGE_KEY` constant from `useSliceAudio.ts` (no pitch adjustment exists in the UI).
  - Remove unused `AUDIO_SLEEP_TIMER_STORAGE_KEY` constant (sleep timer is session-only) and redundant `setAutoAdvance(value)` function (state is directly reactive via `useStorage`).
- **Page Template Cleanup**: Remove unused `audioPlayerRef` declaration and template binding from `pages/read/[bookId].vue`.
- **Audio Utility Pruning**: Remove unused `wavDurationSeconds` function from `utils/audioWav.ts` and its standalone assertions in `audioWav.spec.ts`.
- **i18n Catalog Pruning**: Remove unrendered translation keys `reader.audio_pitch` and `reader.audio_auto_advance_disabled_today_hint` from `en.json` and `vi.json`.
- **Constant Standardization**:
  - Replace hardcoded voice key template in `ReaderAudioPlayer.vue` with `AUDIO_VOICE_STORAGE_KEY`.
  - Reference `GetOrSynthesizeChunkAudioHandler.MonthlyCharacterLimit` and `NearLimitThreshold` in `GetAudioQuotaHandler.cs` to eliminate duplicate numeric literals.
- **Test Alignment**: Update `ReaderAudioPlayer.spec.ts` and `useSliceAudio.spec.ts` mocks and assertions to match the streamlined composable contract.

## Capabilities

### Modified Capabilities
- `audio-narration`: Prune dead composable exports, orphaned store state, unrendered translation keys, and unused audio helper utilities while preserving full three-tier playback, caching, and control invariants.

## Impact

- **Affected Systems**: Frontend (`useLibraryStore.ts`, `useSliceAudio.ts`, `ReaderAudioPlayer.vue`, `[bookId].vue`, `audioWav.ts`, i18n locales, unit tests), Backend (`GetAudioQuotaHandler.cs`).
- **Breaking Changes**: None. Internal refactoring and dead code deletion only; no externally observable APIs or database schemas modified.
- **Migration Needs**: None. Existing audio caches in IndexedDB and PostgreSQL remain fully compatible.
