# Design

## Context

The TechDaily reader uses a three-tier cascade audio narration system (`System` -> `Cloud` -> `Device`). The frontend uses `useSliceAudio.ts` to orchestrate audio streaming, IndexedDB chunk caching, and `<audio>` element playback, while `ReaderAudioPlayer.vue` handles user interaction. Backend `.NET` handlers manage cloud TTS synthesis and quota tracking.

Successive feature iterations left behind orphaned store fields (`useLibraryStore.audioQuota`), unreferenced experimental APIs (`pitch` controls), dead helper functions (`toggle`, `setAutoAdvance`, `wavDurationSeconds`), unrendered i18n keys, and duplicate quota constants.

## Goals / Non-Goals

**Goals:**
- Delete dead state, unused methods, orphan types, unused constants, and unrendered i18n keys across frontend and backend.
- Decouple `useLibraryStore` from audio narration concerns, consolidating all quota state within `useSliceAudio.ts`.
- Standardize voice storage keys and centralize backend quota constants.
- Maintain 100% test passing rate across both Vitest and xUnit test suites.

**Non-Goals:**
- Modifying audio synthesis workflows, Web Worker logic, or IndexedDB caching algorithms.
- Changing database schema or EF Core entities (`DocumentChunkAudios` remains active).
- Redesigning the visual layout of `ReaderAudioPlayer.vue`.

## Decisions

### 1. Pinia Store Decoupling
- **Decision**: Remove `audioQuota`, `fetchAudioQuota`, and `import type { AudioQuotaInfo }` from `frontend/stores/useLibraryStore.ts`.
- **Rationale**: `ReaderAudioPlayer.vue` already relies exclusively on `useSliceAudio.ts` for quota fetching and reactive quota exhaustion handling. `useLibraryStore` should focus solely on documents, navigation, and bookmarks.

### 2. Streamlining Composable API Surface
- **Decision**: Remove the following from `frontend/composables/useSliceAudio.ts`:
  - `toggle()`: Never called; `ReaderAudioPlayer.vue` maintains its own `onToggle()` method.
  - `pitch`, `setPitch()`: Web Speech pitch controls are not present in the design system.
  - `AUDIO_PITCH_STORAGE_KEY`: Associated with dead pitch controls.
  - `AUDIO_SLEEP_TIMER_STORAGE_KEY`: Sleep timer is session-scoped, not persisted to `localStorage`.
  - `setAutoAdvance()`: Redundant helper; `autoAdvance` is a reactive `useStorage` ref toggled directly.
- **Rationale**: Minimizes cognitive overhead and eliminates unused properties from the composable return signature.

### 3. Deleting Orphaned Utility `wavDurationSeconds`
- **Decision**: Remove `wavDurationSeconds` from `frontend/utils/audioWav.ts` and its test case in `tests/utils/audioWav.spec.ts`.
- **Rationale**: The runtime application computes playback duration via `<audio>` element events or sentence duration accumulation. `wavDurationSeconds` existed solely for a single isolated unit test.

### 4. Template & Translation Cleanliness
- **Decision**:
  - Remove unused `audioPlayerRef` from `pages/read/[bookId].vue`.
  - Remove `reader.audio_pitch` and `reader.audio_auto_advance_disabled_today_hint` from `en.json` and `vi.json`.
  - Replace literal template strings in `ReaderAudioPlayer.vue` with `AUDIO_VOICE_STORAGE_KEY`.
- **Rationale**: Avoid dangling component refs and keep translation files synchronized with real UI elements.

### 5. Backend Constant Centralization
- **Decision**: Update `GetAudioQuotaHandler.cs` to reuse `GetOrSynthesizeChunkAudioHandler.MonthlyCharacterLimit` and `NearLimitThreshold`.
- **Rationale**: Eliminates magic numbers duplicated across multiple use-case handlers.

## Risks / Trade-offs

- **Risk**: Test mock breakages in `ReaderAudioPlayer.spec.ts` and `useSliceAudio.spec.ts` due to removed composable methods.
  - **Mitigation**: Update test mocks in the same change to remove obsolete method stubs (`setPitch`, `setAutoAdvance`) and verify all 666+ frontend tests and 351 backend tests pass.
