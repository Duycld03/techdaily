# Tasks

## 1. Bug Fixes & Content Isolation

- [x] 1.1 Fix the dual-card rendering bug in `frontend/pages/review.vue` by adding `v-else` to the completion state container so it is strictly mutually exclusive with the active review deck.
- [x] 1.2 Fix the flashcard answer leak in `frontend/components/review/FlashcardDeck.vue` by removing `card.topicSummary` from the front face and restricting the front face exclusively to the question prompt and metadata.

## 2. Dev-Learning Studio Flashcard Modernization & Keyboard Ergonomics

- [x] 2.1 Refactor `frontend/components/review/FlashcardDeck.vue` with Dev-Learning Studio 3D flip perspective (`perspective: 1000px`), Obsidian glass surfaces (`.glass-card`), category pill (`CategoryBadge`), repetition badge, and SM-2 telemetry pills.
- [x] 2.2 Add keyboard navigation listeners to `frontend/components/review/FlashcardDeck.vue` (`Space`/`Enter` to flip; number keys `1`-`4` to grade when flipped; input shielding and unmount cleanup).
- [x] 2.3 Upgrade `frontend/components/review/Sm2GradingButtons.vue` with studio glass styling, calibrated accent tints (Rose, Amber, Sky, Emerald), keyboard shortcut badges `[1]`, `[2]`, `[3]`, `[4]`, and dynamic i18n subtext bindings.
- [x] 2.4 Add bilingual localization keys to `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json` for flip hints, answer headings, and SM-2 interval descriptions.
## 3. Automated Verification & Quality Assurance

- [x] 3.1 Run frontend test suite (`npm --prefix frontend test`) to verify all existing and new component tests pass.
- [x] 3.2 Validate OpenSpec change specifications and sync requirements with `openspec validate --changes` and `openspec validate --specs`.
