# Tasks

## 1. Library Studio Modernization

- [x] 1.1 Update `frontend/pages/library.vue` container, header, category filter pills, and search input with Dev-Learning Studio tokens (`dark:bg-canvas`, `dark:bg-canvas-subtle`, `border-white/[0.08]`).
- [x] 1.2 Update `frontend/pages/library.vue` book catalog cards, progress bars, bookmark resume badges, and card action buttons to `.glass-card` styling with Iris Violet accents.
- [x] 1.3 Modernize import modal dialog, PDF dropzone, and crawler form in `frontend/pages/library.vue` to `.glass-panel` styling.

## 2. Notes Studio Modernization

- [x] 2.1 Update `frontend/pages/notes.vue` container, header, search bar, and horizontal tag chip bar to use Studio tokens and Iris Violet active state.
- [x] 2.2 Update `frontend/pages/notes.vue` highlight cards, excerpt quotes, flashcard creation triggers, and inline note editor to `.glass-card` styling.

## 3. Insights Studio Modernization

- [x] 3.1 Update `frontend/pages/insights.vue` header banner, shuffle trigger, and generate button with `.glass-panel` elevation and Studio styling.
- [x] 3.2 Modernize view mode switcher, category chips, insight card reader, and AI generation modal in `frontend/pages/insights.vue`.

## 4. Automated Testing & Verification

- [x] 4.1 Run full frontend test suite (`npm --prefix frontend test`) to ensure zero regressions across all 53 test files.
- [x] 4.2 Validate OpenSpec changes and specifications (`openspec validate --changes` and `openspec validate --specs`).
