# Tasks: Frontend UI Phase 2 - Interactive Practice & Arena (StudioLayout & Preview Verification)

## 1. Isolated Playground Preview Prototyping

- [x] 1.1 Scaffold interactive sandbox prototype in `frontend/pages/playground/temp.vue` rendering View A (Quiz Arena Studio with 68% question stage, syntax code block, and 4 `OptionCard.vue` interactive states + 32% telemetry dock)
- [x] 1.2 Implement View B in `frontend/pages/playground/temp.vue` (Flashcard 3D Practice Studio with centered `max-w-2xl` card container, 3D flip transform, SM-2 grading CTA pills, and companion telemetry dock with progress bar and hotkeys)
- [x] 1.3 Capture headless 1080p visual screenshots in Light Mode and Dark Obsidian Mode for both views and submit to user for visual review and approval gate

## 2. OptionCard & Quiz Arena Production Cutover
- [x] 2.1 Refactor `frontend/components/ui/OptionCard.vue` to guarantee zero layout shift across `default`, `hover`, `selected`, `correct`, and `incorrect` states with uniform padding (`p-4 sm:p-5`), fixed-size option badges (`A`, `B`, `C`, `D`), and hairline borders
- [x] 2.2 Wire `frontend/pages/quiz.vue` to `StudioLayout.vue`, dedicating `#main` (68% width) to question prompt and option choices, and `#dock` (32% width) to live countdown timer, streak multiplier, seniority pill, and question navigation map
- [x] 2.3 Implement keyboard shortcuts (`1-4` and `A-D`) for instant option selection in `frontend/pages/quiz.vue` with active element safeguards against input typing

## 3. Flashcard Practice Player & Telemetry Dock Cutover

- [x] 3.1 Refactor `frontend/components/review/FlashcardDeck.vue` and `frontend/pages/review.vue` (Tab 1: Active Review) to eliminate excessive vertical stretching and dark voids by setting bounded height (`min-h-[340px] max-h-[500px]`), centered geometry (`max-w-2xl mx-auto`), and smooth 3D CSS perspective card flipping
- [x] 3.2 Wire Tab 1 to `StudioLayout.vue`, moving session progress, SM-2 retention metrics, and keyboard shortcuts guide into the 32% `#dock`
- [x] 3.3 Add missing bilingual i18n keys in `frontend/i18n/locales/en.json` and `vi.json` for `review.session_progress` ("Session Progress" / "Tiến Độ Phiên Ôn Tập"), `quiz.streak_multiplier`, and practice telemetry strings
## 4. Automated Testing & Verification

- [x] 4.1 Update and run Vitest test suites for OptionCard and FlashcardDeck
- [x] 4.2 Run full frontend test suite (`npm test`) to ensure 100% pass rate across all suites
- [x] 4.3 Verify production pages (`/quiz` and `/review`) via browser screenshots confirming 1:1 match with the approved Playground preview
