# Tasks

## 1. Frontend Component Enhancements

- [x] 1.1 Add `size` prop (`'sm' | 'md'`, default `'md'`) to `AppSelect.vue` with compact padding (`px-3 py-1.5`) and scaled icon sizing (`w-3.5 h-3.5`) for `'sm'`.
- [x] 1.2 Update `ReaderAudioPlayer.vue` to pass `size="sm"` to the voice selection `AppSelect` component.
- [x] 1.3 Verify floating dropdown positioning and alignment in `AppSelect.vue` when using compact trigger height.

## 2. Testing & Visual Verification

- [x] 2.1 Add unit tests in `frontend/tests/components/AppSelect.spec.ts` asserting that `size="sm"` applies compact trigger styling while preserving accessibility attributes.
- [x] 2.2 Run `npm test` across all frontend test suites to ensure 100% pass rate.
- [x] 2.3 Execute automated headless Chromium visual verification on Desktop (1440x900) and Mobile (390x844) viewports at `/playground/audio-narration` to confirm equal height between the Listen button and Voice Select.
