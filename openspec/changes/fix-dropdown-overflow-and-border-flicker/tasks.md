# Tasks

## 1. Frontend Floating Dropdown Architecture (`AppSelect.vue`)

- [x] 1.1 Upgrade `AppSelect.vue` to render the floating listbox via `<Teleport to="body">` using dynamic fixed positioning (`position: fixed`, `left`, `width`, `top`, `bottom`, `z-[60]`) computed from `triggerRef.getBoundingClientRect()`, eliminating parent container overflow clipping and scrollbar triggering.
- [x] 1.2 Implement automatic vertical collision detection and auto-flip placement in `AppSelect.vue`: when viewport clearance below the trigger is less than 260px and clearance above is greater, position the popover above the trigger button.
- [x] 1.3 Add window `scroll` (capture mode) and `resize` event synchronization in `AppSelect.vue` to update coordinates dynamically, and update `handleClickOutside` to check both `triggerRef` and the teleported `listboxRef`.

## 2. Frontend Zero-Shift Segmented Controls & Settings Auto-Persist

- [x] 2.1 Refactor `frontend/components/roadmap/RoadmapViewSwitcher.vue` to pre-allocate `border border-transparent` on base tab buttons, restrict transitions to `transition-colors duration-150`, and toggle active border color (`dark:border-white/[0.06]`) without modifying element geometry.
- [x] 2.2 Add `scrollbar-gutter: stable` to scrollable modal dialog containers in `frontend/pages/library.vue` and audit segmented tab switchers in `frontend/pages/library.vue`, `frontend/pages/review.vue`, and `frontend/pages/profile.vue` for zero-shift border compliance.
- [x] 2.3 Refactor `frontend/pages/settings.vue` to use a reactive `ref` for `commonTimezones`, deduplicate prepended zones, and automatically persist timezone changes to `profileStore.updateProfile` upon selection with success feedback.

## 3. Automated Verification & Testing

- [x] 3.1 Update and expand unit tests in `frontend/tests/components/AppSelect.spec.ts` covering teleported body rendering, auto-flip placement calculation, and click-outside dismissal.
- [x] 3.2 Update and expand unit tests in `frontend/tests/components/roadmap/RoadmapViewSwitcher.spec.ts` asserting constant border geometry across tab toggles.
- [x] 3.3 Update unit tests in `frontend/tests/pages/settings.spec.ts` asserting reactive timezone registration and auto-save on select.
- [x] 3.4 Run full frontend test suite (`npm --prefix frontend test`) and validate OpenSpec change specifications with `openspec validate --changes` and `openspec validate --specs`.
