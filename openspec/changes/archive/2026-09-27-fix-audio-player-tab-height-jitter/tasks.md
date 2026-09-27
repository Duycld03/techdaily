# Tasks

## 1. Frontend Height Normalization

- [x] 1.1 Update `frontend/components/common/AppSelect.vue` to explicitly include `h-8` (`32px`) in `sizeClasses` for `size="sm"` to clamp the trigger button height to exactly 32px matching standard buttons.
- [x] 1.2 Update `frontend/components/reader/ReaderAudioPlayer.vue` to assign `h-8` to the "Listen" button and `min-h-[50px]` to the player container, eliminating container layout shift when toggling between Cloud and Device modes.

## 2. Frontend Testing & Verification

- [x] 2.1 Update or add unit tests in `frontend/tests/components/AppSelect.spec.ts` verifying that `AppSelect` with `size="sm"` renders with `h-8` height styling.
- [x] 2.2 Run full frontend test suite (`npm test`) to ensure all component and composable tests pass with zero regressions.
- [x] 2.3 Visually verify via headless Chromium (`eval` browser device) that toggling between Cloud and Device engines in `/playground/audio-narration` results in exactly 0px container height difference on Desktop (1440x900) and Mobile (390x844).
