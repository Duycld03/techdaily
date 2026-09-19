# Tasks

## 1. Frontend Component Architecture

- [x] 1.1 Create `frontend/components/common/AppSelect.vue` supporting `v-model`, `options`, leading icon slot/prop, glass-panel popover with `dark:bg-canvas-elevated`, rounded-2xl geometry, and trailing checkmarks.
- [x] 1.2 Implement full keyboard navigation (`ArrowDown`, `ArrowUp`, `Enter`, `Space`, `Escape`, `Tab`), ARIA attributes (`role="combobox"`, `role="listbox"`, `role="option"`), and click-outside dismissal in `AppSelect.vue`.
- [x] 1.3 Create dedicated unit test suite in `frontend/tests/components/AppSelect.spec.ts` covering click toggle, option selection, `v-model` emission, keyboard navigation, and click-outside dismissal.

## 2. Frontend Page Migration & Cutover

- [x] 2.1 Refactor engineering target role selector in `frontend/pages/profile.vue` to use `AppSelect` with `Briefcase` leading icon.
- [x] 2.2 Refactor timezone preference selector in `frontend/pages/settings.vue` to use `AppSelect` with `Globe` leading icon.
- [x] 2.3 Refactor grounded book selector in `frontend/pages/quiz.vue` to use `AppSelect` with `BookOpen` leading icon.
- [x] 2.4 Refactor web document import and PDF upload category selectors in `frontend/pages/library.vue` to use `AppSelect`.

## 3. Automated Verification & Testing

- [x] 3.1 Run full frontend test suite (`npm --prefix frontend test`) to ensure all test files pass without regression.
- [x] 3.2 Validate OpenSpec change specifications with `openspec validate --changes` and `openspec validate --specs`.
