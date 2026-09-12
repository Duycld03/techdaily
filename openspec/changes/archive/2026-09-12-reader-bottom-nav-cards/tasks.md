## 1. Localization & Data Preparation

- [x] 1.1 Add card action sub-label keys (`reader.prev_slice_card_label`, `reader.next_slice_card_label`, `reader.completed_card_label`) in `frontend/i18n/locales/en.json` and `vi.json`.
- [x] 1.2 In `frontend/pages/read/[bookId].vue`, define `prevChunk` computed property alongside existing `nextChunk`.

## 2. Symmetrical Card Grid Implementation

- [x] 2.1 Refactor the bottom navigation section in `frontend/pages/read/[bookId].vue` into a responsive 2-column grid (`grid grid-cols-1 sm:grid-cols-2 gap-4`), replacing the deformed button row.
- [x] 2.2 Implement the Previous card with `v-if="prevChunk"`, uppercase label, icon, and truncated chapter title.
- [x] 2.3 Implement the Next / Completed card with dynamic right-column anchoring (`:class="{ 'sm:col-start-2': !prevChunk }"`), uppercase label, icon, and truncated chapter title.
- [x] 2.4 Add a clean progress meta header above the navigation cards displaying total progress without causing horizontal compression.

## 3. Verification & Testing

- [x] 3.1 Run frontend unit tests (`npm test`) to ensure no regressions across components and stores.
- [x] 3.2 Verify responsive layout and visual balance across desktop, tablet, and mobile viewports in both English and Vietnamese locales.
