# Tasks

## 1. Frontend

- [x] 1.1 Restructure `ReaderAudioPlayer.vue` layout to decouple the scrubber from the primary controls row on mobile viewports (< 640px)
- [x] 1.2 Remove competing idle flex spacer in `ReaderAudioPlayer.vue` and allocate readable width (`min-w-[130px]` / `flex-1`) to the voice selector dropdown
- [x] 1.3 Implement dedicated full-width mobile scrubber row (`sm:hidden`) that renders only when audio is active (`loadedId === chunk?.id && duration > 0`)
- [x] 1.4 Maintain single-row inline presentation on desktop and tablet viewports (`sm:` and up, ≥ 640px)

## 2. Verification

- [x] 2.1 Run frontend test suite (`npm test`) to verify zero regressions in reader and audio component tests
- [x] 2.2 Execute automated headless browser dual-gate visual verification on Desktop (1440x900) and Mobile (390x844) in both idle and playing states
