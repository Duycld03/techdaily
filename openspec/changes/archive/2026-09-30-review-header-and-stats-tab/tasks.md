# Tasks

## 1. Preparation & Localized Strings

- [x] 1.1 Read required skills before modifying frontend code (`skill://vue`, `skill://pinia`, `skill://vitest`, `skill://vueuse-functions`, `skill://antfu-design`); verify each `read` call is executed.
- [x] 1.2 In `frontend/i18n/locales/vi.json`, disambiguate retention terms by updating `review.atrisk_leech` to `"Hay quên"` and `review.ease_struggling` to `"Chưa vững"`, and add `review.tab_stats: "Thống kê"`; in `frontend/i18n/locales/en.json`, add `review.tab_stats: "Analytics"`; verify with `npm test tests/i18n/reviewRetentionKeys.spec.ts`.

## 2. Standardized Page Header Banner (pages/review.vue)

- [x] 2.1 Replace the isolated `max-w-5xl` bare tab bar with the standardized platform header matching `/quiz` (Left: `<Layers>` icon badge in `bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20`, `<h1>` primary title `$t('review.title')`, `<p>` subtitle `$t('review.subtitle')`; Right: 3-tab pill switcher); verify header elements render aligned on desktop.
- [x] 2.2 Update root page container styling to `max-w-7xl mx-auto px-4 sm:px-6 py-6 space-y-6`, ensuring consistent width and alignment between header and tab contents; verify that the header and content boundaries match without horizontal voids.

## 3. Three-Tab Navigation & State Synchronization

- [x] 3.1 In `pages/review.vue`, expand the tab switcher to three buttons: `session` (`$t('review.tab_session')` with `<Layers>`), `management` (`$t('review.tab_management')` with `<Library>`), and `stats` (`$t('review.tab_stats')` with `<BarChart3>`); verify that active tab highlighting and counts render correctly.
- [x] 3.2 Update `activeTab` type to `'session' | 'management' | 'stats'` and expand `watch(activeTab)` and `onMounted` URL synchronization to handle `?tab=stats` bidirectionally (restoring from query and updating query on switch); verify that URL updates reactively.

## 4. Decouple Inventory and Analytics Views

- [x] 4.1 Remove the two rows of 6 Bento stat cards from `activeTab === 'management'`, placing the search bar, quick filter chips, flashcard bento card grid, and pagination directly at the top of the viewport; verify that flashcards render immediately on screen without scrolling.
- [x] 4.2 Create the `activeTab === 'stats'` view containing Row 1 (`FlashcardHeroCard`, `MasteryGaugeCard`, `ReviewForecastChart` in `grid-cols-1 md:grid-cols-2 xl:grid-cols-3`) and Row 2 (`AtRiskLeechCard`, `SourceChannelRetentionCard`, `EaseFactorDistributionCard` in `grid-cols-1 lg:grid-cols-3`); verify all 6 analytics cards render cleanly in this tab.
- [x] 4.3 Ensure cross-tab navigation from analytics CTAs functions properly: `FlashcardHeroCard` `@start-review` switches `activeTab = 'session'`, and `AtRiskLeechCard` `@review` switches `activeTab = 'management'`; verify that clicking each button transitions to the expected view.

## 5. Tests & Verification

- [x] 5.1 Update `frontend/tests/pages/review.spec.ts` to assert the 3-tab navigation bar and test navigation to the `stats` tab; verify that 100% of Vitest test suites pass (`npm test`).
- [x] 5.2 Gate 2 — Visual Verification: drive headless Chromium to `/review`, capture Desktop (`1440x900`) and Mobile (`390x844`) screenshots of the new header, the clean Tab 2 inventory, and the Tab 3 analytics dashboard; present screenshots as visual proof.
