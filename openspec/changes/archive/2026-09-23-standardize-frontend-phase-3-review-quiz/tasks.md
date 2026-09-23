# Tasks

## 1. SM-2 Flashcard Grading & Flip Responsiveness

- [x] 1.1 Update `Sm2GradingButtons.vue` to adopt `grid-cols-2 sm:grid-cols-4 gap-2.5` with touch heights $\ge 44\text{px}$
- [x] 1.2 Audit `FlashcardDeck.vue`, `FlashcardHeroCard.vue`, and `FlashcardBentoCard.vue` for mobile flip performance and zero overflow

## 2. Retention Analytics & Forecast Chart

- [x] 2.1 Refactor `ReviewForecastChart.vue` to scale bars smoothly and format values with `tabular-nums`
- [x] 2.2 Audit `MasteryGaugeCard.vue` for responsive SVG gauge scaling on mobile viewports

## 3. Quiz Arena & Keyboard Hygiene

- [x] 3.1 Audit `pages/quiz.vue` option choices for `whitespace-nowrap shrink-0` on option badges and clean session state reset
- [x] 3.2 Standardize keyboard shortcuts (1-4, Enter) using VueUse `useEventListener`
- [x] 3.3 Verify unit test suite passes for review and quiz pages via `npm test`
