# Tasks

## 1. Global Stylesheet Normalization (Frontend)

- [x] 1.1 Add base-level select normalization rules in `frontend/assets/css/main.css` (`appearance: none;`, SVG chevron data URI background, right padding `2.5rem`, pointer cursor, and `<option>` light/dark theme contrast).
- [x] 1.2 Add reusable `.select-input` component utility class in `frontend/assets/css/main.css` under `@layer components`.

## 2. Form Select Controls Audit & Refactoring (Frontend)

- [x] 2.1 Refactor target role `<select>` in `frontend/pages/profile.vue` to use `.select-input pl-9` and ensure chevron clearance.
- [x] 2.2 Refactor Grounded in Book `<select>` in `frontend/pages/quiz.vue` to use `.select-input`.
- [x] 2.3 Refactor timezone `<select>` in `frontend/pages/settings.vue` to use `.select-input`.
- [x] 2.4 Refactor document category `<select>` elements in `frontend/pages/library.vue` (Markdown import modal, PDF upload modal, and crawler modal) to use `.select-input`.

## 3. Automated Verification & Testing

- [x] 3.1 Run full frontend test suite (`npm --prefix frontend test`) to ensure all test files pass without regression.
- [x] 3.2 Validate OpenSpec change specifications with `openspec validate --changes` and `openspec validate --specs`.
