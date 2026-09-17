# Tasks: Bento Review and Profile Dashboards

## 1. Foundations & i18n Localization

- [x] 1.1 Add Vietnamese and English translation dictionary keys for the Bento Review Dashboard (hero card, mastery gauge proficiency tiers, forecast chart labels, advanced filter modal sections, and card accordion toggles) in `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`, and verify parity using `grep` or locale validation.
- [x] 1.2 Add Vietnamese and English translation dictionary keys for the Engineer Portfolio Dashboard (identity badges, milestone labels, and curriculum domain coverage categories) in `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`, and verify parity using `grep`.
- [x] 1.3 Update TypeScript interface definitions for filter state, forecast data bins, and domain progress in `frontend/stores/useReviewStore.ts` and `frontend/stores/useProfileStore.ts`, verifying that `npx nuxi typecheck` passes without errors.

## 2. Review Bento Overview Components

- [x] 2.1 Implement `frontend/components/review/FlashcardHeroCard.vue` displaying cards due today counter, dynamic estimated study duration, and a 1-click CTA button switching to the 3D review player, and verify rendering via component smoke test.
- [x] 2.2 Implement `frontend/components/review/MasteryGaugeCard.vue` with pure SVG semi-circular gauge arc, percentage display, and dynamic motivational proficiency tier badge, and verify arc stroke rendering across 0%, 50%, and 100% values.
- [x] 2.3 Implement `frontend/components/review/ReviewForecastChart.vue` displaying upcoming 7-day review volume mini bar chart with day labels, hover tooltips, and empty day indicators, and verify responsive bar heights with mock data.

## 3. Two-Tier Search & Advanced Filter Modal

- [x] 3.1 Implement `frontend/components/review/AdvancedFilterModal.vue` providing multi-section selections for Knowledge Source, Mastery Stage, Due Urgency, and Sorting options with Reset and Apply actions, and verify modal open/close and event emission.
- [x] 3.2 Implement the quick search bar in `frontend/pages/review.vue` with `⌘K` keyboard shortcut listener, essential quick chips (`All`, `Due Today`, `Mastered`), and the `[ ⚙️ Advanced Filter ]` trigger button with active filter counter badge, and verify keyboard focus behavior.

## 4. Bento Knowledge Card Grid

- [x] 4.1 Implement `frontend/components/review/FlashcardBentoCard.vue` replacing table rows with responsive bento cards, featuring prominent front prompt, source/status badges, interactive accordion toggle for markdown answers with code syntax highlighting, and SM-2 metrics footer with action buttons.
- [x] 4.2 Integrate `FlashcardBentoCard.vue` into `frontend/pages/review.vue` within a responsive 2-3 column CSS grid (`grid-cols-1 md:grid-cols-2 xl:grid-cols-3`), verifying layout integrity and that accordion expansion does not cause adjacent column jitter.
- [x] 4.3 Connect card action triggers (Edit markdown modal, SM-2 reset confirmation dialog, and soft deletion dialog) to `useReviewStore`, and verify that card updates, resets, and deletions reflect reactively in the grid and statistics.

## 5. Profile Portfolio Bento Dashboard

- [x] 5.1 Implement `frontend/components/profile/EngineerProfileHero.vue` for the right column (1/3 width on desktop), rendering large user avatar with status badge, display name, target role badge, account type badge, and stacked milestones (streak with fire icon, drills completed, quiz accuracy), and verify responsive stacking.
- [x] 5.2 Implement `frontend/components/profile/DomainGoalTracker.vue` displaying progress bars for the 4 curriculum domains (.NET, PostgreSQL, System Design, Frontend) with theme accent colors and completion percentages, and verify calculation against quiz and curriculum stats.
- [x] 5.3 Refactor `frontend/pages/profile.vue` into the asymmetric 2-column layout (Desktop 2/3 - 1/3, stacking on mobile), assembling settings & goals on the left, identity & milestones on the right, and domain mastery below, and verify form submission preserves existing notification and timezone settings.

## 6. Polish, Verification & Quality Assurance

- [x] 6.1 Verify Dark and Light mode theme transitions across all newly created Bento components, checking contrast ratios and border tokens (`slate-200` light vs `slate-800` dark).
- [x] 6.2 Verify Vietnamese diacritic text rendering across all screen sizes (mobile 375px, tablet 768px, desktop 1280px), ensuring zero text overflow, no broken badges, and smooth line wraps.
- [x] 6.3 Live visual verification of `/review` on large screen (desktop ≥1280px) in both English and Vietnamese using MCP browser tool (no scripts), verifying hero CTA, mastery gauge, 7-day forecast, search/filter modal, and card accordion.
- [x] 6.4 Live visual verification of `/review` on mobile screen (width ~375-390px) in both English and Vietnamese using MCP browser tool, verifying single-column stacking, mobile filter sheet, and touch interactions.
- [x] 6.5 Live visual verification of `/profile` on large screen (desktop ≥1280px) in both English and Vietnamese using MCP browser tool, verifying asymmetric 2-column layout (2/3 - 1/3), identity card, milestones, and domain goal tracker.
- [x] 6.6 Live visual verification of `/profile` on mobile screen (width ~375-390px) in both English and Vietnamese using MCP browser tool, verifying single-column stacked layout, identity card, and domain mastery progress bars.
- [x] 6.7 Run full client-side type-checking and automated tests (`npm run test` or Vitest) to ensure no regressions in existing review, quiz, or profile store workflows.
