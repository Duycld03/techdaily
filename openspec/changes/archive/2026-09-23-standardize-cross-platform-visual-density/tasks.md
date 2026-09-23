# Tasks

## 1. Frontend - Core Typography & Webfont Loading

- [x] 1.1 Add Google Fonts `Inter` stylesheet (weights 400, 500, 600, 700, 800) and preconnect links to `app.head.link` in `frontend/nuxt.config.ts`
- [x] 1.2 Verify `font-sans` in `frontend/tailwind.config.js` properly defaults to `Inter` across all platforms

## 2. Frontend - Scenario Challenge Dock Compaction (`InterviewChallengePane.vue`)

- [x] 2.1 Update scenario challenge heading typography in `InterviewChallengePane.vue` to `text-base sm:text-lg md:text-xl font-bold` (line 172)
- [x] 2.2 Compact option button cards in `InterviewChallengePane.vue` by reducing padding from `p-3.5 sm:p-5` to `p-3 sm:p-3.5` and border radius to `rounded-xl` (line 197)
- [x] 2.3 Adjust option text styling in `InterviewChallengePane.vue` to `text-sm sm:text-base` (lines 208, 240) and option letter badge to `w-7 h-7 text-sm` (line 200)
- [x] 2.4 Verify that in 1080p desktop split-view, all four options (A, B, C, D) and the Submit button render above the fold without dock scrolling

## 3. Frontend - Dashboard Viewport & Natural Scrolling (`HomeBentoDashboard.vue`)

- [x] 3.1 Replace fixed desktop height and clipping `lg:h-[calc(100dvh-3.5rem)] lg:overflow-hidden` with `min-h-[calc(100dvh-3.5rem)] pb-8` in `HomeBentoDashboard.vue` (line 129)
- [x] 3.2 Ensure Knowledge Constellation (Card E) and all Bento cards are reachable via smooth vertical scrolling on constrained desktop viewports

## 4. Frontend - Flashcard Deck & Review Spacing (`FlashcardDeck.vue`, `pages/review.vue`)

- [x] 4.1 Reduce flashcard container minimum height from `min-h-[340px] sm:min-h-[400px]` to `min-h-[280px] sm:min-h-[320px]` and padding from `p-6 sm:p-8` to `p-5 sm:p-6 rounded-2xl` in `FlashcardDeck.vue` (line 166)
- [x] 4.2 Compact flashcard question heading to `text-base sm:text-xl font-bold` and reduce flip button CTA margin from `mt-8 pt-6` to `mt-5 pt-4` in `FlashcardDeck.vue`
- [x] 4.3 Reduce outer page padding in `pages/review.vue` from `md:p-10` to `p-4 sm:p-6 md:p-8` and tab bar margin from `mb-6 sm:mb-8` to `mb-4 sm:mb-6`

## 5. Frontend - Library Grid Spacing (`pages/library.vue`)

- [x] 5.1 Reduce document card padding from `p-6 sm:p-7 rounded-3xl` to `p-4 sm:p-5 rounded-2xl` and internal spacing from `space-y-4` to `space-y-3` in `pages/library.vue`
- [x] 5.2 Adjust responsive card grid gap from `gap-6` to `gap-4 sm:gap-5` in `pages/library.vue`

## 6. Verification & Automated Tests

- [x] 6.1 Run Vitest unit tests for modified components (`InterviewChallengePane.spec.ts`, `today.spec.ts`, `review.spec.ts`) and verify 100% pass rate
- [x] 6.2 Run full frontend test suite (`npm run test`) to ensure zero regressions across all components
- [x] 6.3 Verify visual layout and typography rendering on both desktop viewports (Windows 11 / Ubuntu) and mobile viewports
