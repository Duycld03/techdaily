# Tasks: Reader Audio Layout, Cloud Fallback & Pacer Gating

## 1. Frontend: Audio Composable & Fallback Engine

- [x] 1.1 In `frontend/composables/useSliceAudio.ts`, implement automatic fallback to `cloud` when `synthesizeOnSystem` detects `matching.length === 0` or browser speech synthesis is unavailable.
- [x] 1.2 Emit an event or trigger a toast notification (`useToast().info`) when auto-switching from `system` to `cloud`, notifying the user that the engine switched to Cloud TTS due to unavailable browser voices.
- [x] 1.3 Ensure `engineMode.value` updates reactively to `'cloud'` on fallback and saves the preference to `localStorage`.
- [x] 1.4 Maintain client storage boundary: verify that Web Speech API and On-Device neural TTS never trigger API calls to `/api/v1/library/chunks/{chunkId}/audio` and rely solely on browser IndexedDB for caching.

## 2. Frontend: ReaderAudioPlayer Two-Row Layout & UI Refactor

- [x] 2.1 Restructure `frontend/components/reader/ReaderAudioPlayer.vue` into a container-resilient 2-row layout (Row 1: Play/Pause, Engine Switch, Compact Utilities; Row 2: Voice Picker, Scrubber / Web Speech indicator).
- [x] 2.2 Delete the debug cascade tier badge (`Bậc: {tier}`) and its associated references from `ReaderAudioPlayer.vue` template and script.
- [x] 2.3 Implement dynamic Play/Pause button label: display localized "Pause" (`t('reader.audio_pause')`) when actively playing (including during Web Speech API playback), and "Listen" (`t('reader.audio_listen')`) when paused or idle.
- [x] 2.4 Add Web Speech API active playing banner in Row 2 when `duration === 0 && playing`, displaying active speech status and voice name instead of hiding the timeline or leaving blank space.
- [x] 2.5 Ensure the two-row layout adapts cleanly without element overlap or horizontal clipping on Mobile (390px), constrained split workbench (550px), and full-width desktop (1440px+).

## 3. Frontend: Today Pacer Auto-Advance Gating

- [x] 3.1 In `frontend/components/reader/ReaderAudioPlayer.vue`, add a `disableAutoAdvance` boolean prop (default `false`) that hides/disables the Auto-Advance control and suppresses the `auto-advance` emit on track completion.
- [x] 3.2 In `frontend/components/today/DocReaderPane.vue`, pass `:disable-auto-advance="true"` to ensure narration completion on `/today` never auto-advances the slice before completing the Senior Challenge drill.
- [x] 3.3 Verify that in `frontend/pages/read/[bookId].vue`, auto-advance remains enabled and functions properly when toggled on by the user.

## 4. Frontend: Internationalization & Localization

- [x] 4.1 In `frontend/i18n/locales/vi.json` and `en.json`, add localized keys for the fallback toast notification (`reader.audio_fallback_to_cloud_toast`), Web Speech playback banner (`reader.audio_system_playing_label`), and Today auto-advance disabled hint.
- [x] 4.2 Remove obsolete `audio_cascade_tier` and `audio_cascade_active_hint` localization keys from `vi.json` and `en.json`.

## 5. Testing & Verification

- [x] 5.1 Run and update unit test suites in `frontend/tests/components/reader/ReaderAudioPlayer.spec.ts` to test the new two-row layout, engine fallback toast, dynamic button label, and `disableAutoAdvance` prop.
- [x] 5.2 Run `frontend/tests/composables/useSliceAudio.spec.ts` to verify fallback transitions and storage invariants.
- [x] 5.3 Execute headless browser automated visual verification (`browser` device) capturing screenshots for both Mobile (390x844) and Desktop (1440x900) across `/today` and `/read/[bookId]` to visually verify zero layout collisions.
