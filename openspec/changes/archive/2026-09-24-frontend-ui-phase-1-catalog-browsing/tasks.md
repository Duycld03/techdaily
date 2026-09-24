# Tasks: Frontend UI Phase 1 - Content & Catalog Browsing (Finalized Preview UI to Production & Complete i18n)

## 1. Phase 1 Bilingual i18n Completeness

- [x] 1.1 Expand `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json` with all Phase 1 catalog status keys (`library.status_ready`, `library.status_in_progress`, `library.status_completed`, `library.status_processing`), slice count keys (`library.slice_progress`, `library.slices_total`), and Review Deck keys (`review.loading_deck`, `review.empty_deck_hint`, `review.time_min`).
- [x] 1.2 Expand Review Deck Bento, Filter, and Card keys in both `en.json` and `vi.json` (`review.filter_all`, `review.filter_due`, `review.filter_mastered`, `review.card_urgency_due`, `review.card_urgency_mastered`, `review.card_urgency_learning`, `review.card_ef_interval`, `review.card_source`, `review.card_details_btn`, `review.mastery_tier_master`, `review.mastery_label`, `review.mastery_weekly_trend`, `review.forecast_title_7d`, `review.forecast_desc_7d`, `review.forecast_peak_day`, `review.forecast_daily_average`).

## 2. Technical Library Catalog Parity & i18n (`frontend/pages/library.vue`)

- [x] 2.1 Refactor book card status and slice counter in `frontend/pages/library.vue` to bind reactively to localized keys (`$t('library.status_ready')`, `$t('library.status_in_progress')`, `$t('library.status_completed')`, `$t('library.status_processing')`, `$t('library.slice_progress')`, `$t('library.slices_total')`), eliminating hardcoded English (`'In Progress'`, `'Ready'`, `'Completed'`, `'Processing'`) and hardcoded Vietnamese (`'Lát cắt'`).

## 3. Review Deck Bento Overview & Filter Parity

- [x] 3.1 Refactor `frontend/components/review/FlashcardHeroCard.vue` to match finalized preview: update typography, glow effects, white CTA button with purple text `Bắt Đầu Ôn Tập Ngay` / `Start Review Now` (`Zap` & `ChevronRight` icons), and localized time estimate `~{minutes} phút` / `~{minutes} min` via `$t('review.time_min')`.
- [x] 3.2 Refactor `frontend/components/review/MasteryGaugeCard.vue` to match finalized preview: semi-circular SVG mastery gauge, `Bậc Cao Thủ` / `Master` badge, total cards counter, and localized weekly trend indicator via `$t('review.mastery_weekly_trend')`.
- [x] 3.3 Refactor `frontend/components/review/ReviewForecastChart.vue` to match finalized preview: 7-day mini bar chart with hover tooltips and footer peak load summary via `$t('review.forecast_peak_day')`.
- [x] 3.4 Refactor filter toolbar and search bar in `frontend/pages/review.vue` (Tab 2) to align with preview layout: quick filter chips on left (`Tất Cả Thẻ`, `Cần Ôn Hôm Nay`, `Đã Thuộc` with item counts) and search input on right with `⌘K` keyboard shortcut badge. Replace hardcoded English strings (`'Loading flashcard library...'`, `'Try clearing search filters...'`) with localized `$t` bindings.

## 4. Flashcard Inventory Card Refactoring (`frontend/components/review/FlashcardBentoCard.vue`)

- [x] 4.1 Replace bulky card layout in `frontend/components/review/FlashcardBentoCard.vue` with finalized compact preview card:
  - Container: `p-4 rounded-xl border border-slate-200/90 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle hover:border-brand-500/40 shadow-sm hover:shadow-md`.
  - Header: Urgency pill (`$t('review.card_urgency_due')` rose / `$t('review.card_urgency_mastered')` emerald / `$t('review.card_urgency_learning')` amber) + SM-2 algorithm metrics (`EF: {ef} • {intervalText}`) + compact inline action controls (Edit, Reset, Delete).
  - Body: Question font-bold (`text-xs sm:text-sm`) + concise 2-line answer preview (`line-clamp-2 text-xs text-slate-500 dark:text-slate-400`).
  - Footer: Hairline divider, source citation (`Nguồn: {source}` / `Source: {source}`), and `Chi tiết →` / `Details →` button emitting click for full modal inspection.

## 5. Automated Testing & Visual Verification

- [x] 5.1 Run full Phase 1 Vitest suite across `library.spec.ts`, `review.spec.ts`, and component tests (`FlashcardBentoCard.spec.ts`, `FlashcardHeroCard.spec.ts`, `MasteryGaugeCard.spec.ts`, `ReviewForecastChart.spec.ts`), ensuring 100% pass rate.
- [x] 5.2 Verify visual rendering and responsive layout on both Light Mode and Dark Obsidian Mode via headless browser screenshots on `http://localhost:3000/library` and `http://localhost:3000/review` to guarantee 100% pixel parity with the finalized Preview UI.
