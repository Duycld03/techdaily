# Tasks

## 1. Frontend Localization Catalogs

- [x] 1.1 Update `frontend/i18n/locales/vi.json` and `frontend/i18n/locales/en.json` to set `reader.audio_pause` to concise action verbs ("Tạm dừng" in Vietnamese and "Pause" in English) and add descriptive accessible labels (`reader.audio_pause_desc`), verifying valid JSON formatting via syntax check.

## 2. Frontend Component Layout & Responsiveness

- [x] 2.1 Refactor the play/pause toggle button in `frontend/components/reader/ReaderAudioPlayer.vue` to render the concise `audio_pause` / `audio_listen` visible text while binding the descriptive accessible label (`audio_pause_desc` / `audio_play`) to `aria-label`, verifying template compilation without warnings.
- [x] 2.2 Refine Row 1 control spacing and padding in `frontend/components/reader/ReaderAudioPlayer.vue` (`gap-1.5 sm:gap-2` on the left cluster, `px-1.5 sm:px-2.5` on the engine switch, `gap-1 sm:gap-1.5` on the right cluster) so controls fit within 320px–336px available content width on mobile viewports.

## 3. Frontend Verification & Dual-Gate Validation

- [x] 3.1 Update unit test message dictionaries and assertions in `frontend/tests/components/reader/ReaderAudioPlayer.spec.ts`, and run `npm test frontend/tests/components/reader/ReaderAudioPlayer.spec.ts` to verify 100% passing tests (Gate 1).
- [x] 3.2 Perform automated headless browser visual inspection using Chromium on `/read/[bookId]` across Desktop (1440x900) and Mobile (390x844 and 360px) viewports in both English and Vietnamese during active playback, confirming that the speed button and all utility controls remain completely visible inside the card without clipping (Gate 2).
