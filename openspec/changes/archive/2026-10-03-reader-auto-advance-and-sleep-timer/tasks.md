# Tasks

## 1. Frontend Composable (`useSliceAudio.ts` & Audio Engine)

- [x] 1.1 Add `'system'` to `AudioEngine` type and implement `window.speechSynthesis` wrapper with reactive voices discovery, custom voice selection, pitch, and speed controls up to 2.0x.
- [x] 1.2 Implement the Cascading Engine Resolution algorithm in `useSliceAudio.ts`: Priority 1 (System TTS with matching language check) -> Priority 2 (Google Cloud TTS) -> Priority 3 (Device Web Worker as last resort upon quota exhaustion or offline).
- [x] 1.3 Implement silent audio carrier in `useSliceAudio.ts` to maintain Android Chrome background media lifecycle and prevent OS speech synthesis throttling.
- [x] 1.4 Implement procedural Web Audio chime generator (`playSliceTransitionChime`) in `useSliceAudio.ts` utilizing `AudioContext` with exponential frequency/gain sweep.
- [x] 1.5 Wire `navigator.mediaSession` metadata update and action handlers (`play`, `pause`, `nexttrack`, `previoustrack`) in `useSliceAudio.ts`.
- [x] 1.6 Add `onSliceEnded` callback and slice completion hook to `useSliceAudio.ts` for automated cross-slice progression.

## 2. Frontend Components (`ReaderAudioPlayer.vue`)

- [x] 2.1 Add "System" engine mode to engine switcher dropdown, displaying local OS voices (`vi-vn-x-vic-local` etc.) when active, and visual badges for active cascade tier.
- [x] 2.2 Add "Auto Next" (`autoAdvance`) toggle switch to the player toolbar, persisting preference to `localStorage`.
- [x] 2.3 Implement Sleep Timer dropdown/sheet with fixed presets (`15m`, `30m`, `45m`, `60m`, and `end_of_slice`) and real-time countdown badge.
- [x] 2.4 Implement 15-second exponential audio fade-out logic in `ReaderAudioPlayer.vue` when sleep timer approaches zero.
- [x] 2.5 Emit `auto-advance` and `seek-slice` events to parent view upon slice narration completion or media session track changes.

## 3. Frontend Reader View (`pages/read/[bookId].vue`)

- [x] 3.1 Bind `ReaderAudioPlayer` auto-advance event to increment `activeChunkIndex`, reset scroll position, and start narration on the next slice.
- [x] 3.2 Implement instant reading bookmark persistence (`localStorage.setItem('techdaily_bookmark_{bookId}', activeChunkIndex)`) on auto-advance.
- [x] 3.3 Synchronize reading progress and pacer streak with backend `UserBookPacer` API on slice auto-advance.
- [x] 3.4 Wire Bluetooth and lockscreen `nexttrack` / `previoustrack` navigation to jump across document slices.

## 4. Frontend Localization & Internationalization

- [x] 4.1 Add i18n keys for System TTS, Engine Cascade tiers, Auto Next toggle, Sleep Timer presets, and voice labels in `frontend/locales/en.json`.
- [x] 4.2 Add i18n keys for System TTS, Engine Cascade tiers, Auto Next toggle, Sleep Timer presets, and voice labels in `frontend/locales/vi.json`.

## 5. Verification & Testing

- [x] 5.1 Add Vitest unit tests in `frontend/test/useSliceAudio.test.ts` covering the full Cascading Resolution Hierarchy (System voice available -> System; System voice missing -> Cloud; Quota exhausted / Offline -> Device).
- [x] 5.2 Add Vitest unit tests in `frontend/test/ReaderAudioPlayer.test.ts` covering sleep timer countdown, fade-out calculation, and auto-advance toggle state.
- [x] 5.3 Run Vitest test suite (`npm test`) and verify 100% test pass rate across data contracts and lifecycle handlers.
- [x] 5.4 Execute automated headless browser inspection on Desktop (1440x900) and Mobile (390x844) viewports verifying player toolbar layout, sleep timer badge, and audio settings controls across English and Vietnamese locales.
