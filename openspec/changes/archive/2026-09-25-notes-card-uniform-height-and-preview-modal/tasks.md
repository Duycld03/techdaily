# Tasks

## 1. Frontend Implementation

- [x] 1.1 Update note card typography and line clamping in `frontend/pages/notes.vue` (clamp reflection notes to 2 lines and quotes to 1 line when note exists; clamp quote to 3 lines when no note exists; enforce `h-full` to align row footers flush).
- [x] 1.2 Wire note card container click handler in `frontend/pages/notes.vue` to open the modal in Details mode, applying `@click.stop` to all inner buttons and tag chips.
- [x] 1.3 Integrate a dual-mode tab switcher into `AppModal` in `frontend/pages/notes.vue` toggling between "Details / Preview" (default) and "Edit".
- [x] 1.4 Implement Details mode in the modal, displaying the un-clamped excerpt quote, document breadcrumb, formatted Markdown reflection note via `markdown-it`, tags, and SM-2 flashcard action.
- [x] 1.5 Implement Edit mode in the modal, maintaining the reflection textarea, tag editor, Cancel, and Save actions with smooth transition back to Details mode on save.
- [x] 1.6 Add i18n localization keys for modal tabs (`tab_details`, `tab_edit`, `modal_title_details`, `modal_title_edit`) in `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`.

## 2. Testing and Dual-Gate Verification

- [x] 2.1 Run frontend Vitest test suite (`npm test` in `frontend/`) to verify unit contracts, modal opening/switching behavior, and ensure zero regressions (Gate 1).
- [x] 2.2 Execute the Automated Headless Browser Protocol via `browser` in `eval`, capturing and presenting visual screenshots for both Desktop (1440x900) and Mobile (390x844) to verify uniform row alignment and dual-mode modal display (Gate 2).
