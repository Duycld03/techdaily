# Tasks

## 1. Frontend: AppTimePicker Component Implementation

- [x] 1.1 Create `frontend/components/common/AppTimePicker.vue` with 12h/24h parsing and formatting helpers, trigger button with `Clock` icon, and `v-model` binding.
- [x] 1.2 Implement the teleported floating popover in `AppTimePicker.vue` with fixed bounding rect positioning, auto-flip collision detection, and glassmorphic elevation (`glass-panel dark:bg-canvas-elevated border-white/[0.08] backdrop-blur-md`).
- [x] 1.3 Add 3-column scrollable selection strips (Hours `01-12`, Minutes `00-55` step 5, and Period `AM/PM` toggle) with studio Deep Iris Violet active pill styling.
- [x] 1.4 Add outside-click/Escape dismissal, Done confirmation button, and focus trap logic.

## 2. Frontend: Settings View Integration

- [x] 2.1 In `frontend/pages/settings.vue`, replace raw `<input type="time">` controls for `preferredStudyTime` and `streakAlertTime` with `<AppTimePicker>`.

## 3. Verification & Automated Test Suite Execution

- [x] 3.1 Validate OpenSpec specifications and schema using `openspec validate --changes` and `openspec validate --specs`.
- [x] 3.2 Add comprehensive unit tests in `frontend/tests/components/common/AppTimePicker.spec.ts` covering time conversion, popover toggling, preset selection, and `v-model` emission.
- [x] 3.3 Execute full frontend test suite (`npm test` in `frontend/`) to ensure all assertions pass cleanly.
- [x] 3.4 Execute full Nuxt production build (`npm run build` in `frontend/`) to verify clean SSR compilation.
