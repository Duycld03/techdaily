# Proposal: Frontend UI Phase 1 - Content & Catalog Browsing (Finalized Preview UI to Production & Complete i18n)

## Why

TechDaily's catalog and browsing surfaces (`library.vue`, `review.vue` Tab 2: Deck Management, and `insights.vue`) previously suffered from cross-platform visual density regressions on standard Windows 11 1080p (1920×1080, ~1680px inner viewport, 125%–150% display scaling) and 2K displays due to monolithic outer wrapper cards (`BoardLayout`) causing double-card nesting and wasted vertical whitespace.

Through interactive iteration in `frontend/pages/playground/temp.vue`, the user has formally finalized and approved the **Preview UI** (both View 1: Technical Library and View 2: Flashcard Deck Management). The approved preview establishes:
1. **Unboxed Direct-Canvas Layout Archetype**: All functional tiers (Header/Bento stats, Filter toolbars, 3-column content grids, Pagination) sit directly on the background canvas (`bg-slate-50 dark:bg-canvas`) without enclosing wrapper cards.
2. **Sleek Micro-Component Architecture**: Individual book cards and flashcard inventory cards feature hairline borders (`border-slate-200/90 dark:border-white/[0.08]`), compact urgency/status pills, algorithm metadata (EF, interval, slice progress), and elegant action footers.
3. **Bilingual i18n Deficiencies**: The user identified that several strings across Phase 1 catalog surfaces remain hardcoded in English (e.g. `"In Progress"`, `"Ready"`, `"Completed"`, `"Processing"`, `"Loading flashcard library..."`, `"Try clearing search filters..."`, `"min"`) or hardcoded in Vietnamese (e.g. `"Lát cắt"`), breaking the bilingual UI invariant.

This updated proposal captures the finalized Preview UI decisions and scopes the full transition into production code, along with a comprehensive bilingual internationalization audit across Phase 1.

## What Changes

This change delivers 100% parity between the finalized Preview UI (`playground/temp.vue`) and production code, and achieves complete bilingual i18n:

- **1. Phase 1 Bilingual i18n Completeness (`en.json` & `vi.json`)**:
  - Add standardized localization keys for card statuses: `library.status_ready` ("Ready" / "Sẵn sàng đọc"), `library.status_in_progress` ("In Progress" / "Đang đọc"), `library.status_completed` ("Completed" / "Đã hoàn thành"), `library.status_processing` ("Processing" / "Đang xử lý").
  - Add parameterized slice progress keys: `library.slice_progress` ("Slice {current}/{total} ({percent}%)" / "Lát cắt {current}/{total} ({percent}%)"), `library.slices_total` ("{count} slices" / "{count} lát cắt").
  - Add Review Deck localization keys: `review.loading_deck` ("Loading flashcard library..." / "Đang tải kho thẻ ghi nhớ..."), `review.empty_deck_hint` ("Try clearing search filters to see all cards in your library." / "Thử xoá bộ lọc tìm kiếm để xem tất cả thẻ trong kho."), `review.time_min` ("{minutes} min" / "{minutes} phút").
  - Add Review Deck Bento & Filter keys: `review.filter_all` ("All Cards ({count})" / "Tất Cả Thẻ ({count})"), `review.filter_due` ("Due Today ({count})" / "Cần Ôn Hôm Nay ({count})"), `review.filter_mastered` ("Mastered ({count})" / "Đã Thuộc ({count})"), `review.card_urgency_due` ("Due Today" / "Đến Hạn Hôm Nay"), `review.card_urgency_mastered` ("Mastered" / "Đã Thuộc"), `review.card_urgency_learning` ("Learning" / "Đang Rèn Luyện"), `review.card_ef_interval` ("EF: {ef} • Interval: {interval}d" / "EF: {ef} • Chu kỳ: {interval} ngày"), `review.card_source` ("Source: {source}" / "Nguồn: {source}"), `review.card_details_btn` ("Details →" / "Chi tiết →").

- **2. Technical Library (`frontend/pages/library.vue`)**:
  - Replace hardcoded English status strings (`'Processing'`, `'In Progress'`, `'Completed'`, `'Ready'`) with localized `$t` bindings.
  - Replace hardcoded Vietnamese string `"Lát cắt"` with localized parameterized `$t('library.slice_progress', ...)` and `$t('library.slices_total', ...)`.
  - Maintain the finalized sleek book card layout (p-5 rounded-2xl border, category badge, bookmark count, title, author, progress bar, action footer with GraduationCap icon).

- **3. Review Deck Management (`frontend/pages/review.vue` Tab 2 & Components)**:
  - **`FlashcardHeroCard.vue`**: Align with preview; update typography, glow effects, clean white CTA button with purple text `Bắt Đầu Ôn Tập Ngay` / `Start Review Now` (`Zap` and `ChevronRight` icons), localized time estimate `~{minutes} phút` / `~{minutes} min`.
  - **`MasteryGaugeCard.vue`**: Align with preview; semi-circular SVG gauge with 78% mastery, `Bậc Cao Thủ` badge, weekly trend indicator `+12% tuần này` / `+12% this week`.
  - **`ReviewForecastChart.vue`**: Align with preview; 7-day bar chart with hover tooltips, peak load footer summary `Đỉnh điểm: {count} thẻ` / `Peak: {count} cards`.
  - **Filters & Search Bar**: Direct-canvas layout with quick filter chips on the left (`Tất Cả Thẻ`, `Cần Ôn Hôm Nay`, `Đã Thuộc`) and search input on the right with `⌘K` keyboard shortcut badge.
  - **`FlashcardBentoCard.vue`**: Completely replace the bulky, expandable card with the finalized compact preview card:
    - Container: `p-4 rounded-xl border border-slate-200/90 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle hover:border-brand-500/40 shadow-sm hover:shadow-md`.
    - Header: Urgency badge (`Đến Hạn Hôm Nay` / `Đã Thuộc` / `Đang Rèn Luyện`) + SM-2 algorithm metrics (`EF: x.xx • Chu kỳ: x ngày` / `EF: x.xx • Interval: xd`) + compact action buttons (Edit, Reset, Delete).
    - Body: Bold question text + concise 2-line answer preview (`line-clamp-2`).
    - Footer: Source reference (`Nguồn: ...`) + `Chi tiết →` / `Details →` button to open the full modal/drawer.

- **4. Architectural Insights (`frontend/pages/insights.vue`)**:
  - Maintain unboxed direct-canvas layout, terminal header with `FileCode2` icon, tabbed solution/anti-pattern toggle, and responsive analytics.

## Capabilities

### Modified Capabilities

- `system-layout-archetypes`:
  - Establish the **Unboxed Direct-Canvas Layout Archetype** across all Phase 1 catalog surfaces.
  - Standardize micro-component visual parity (Book Cards, Flashcard Inventory Cards, Bento Stat Cards) matching the finalized Preview UI.
  - Enforce complete bilingual internationalization with zero hardcoded language strings.

## Impact

- **Affected Surfaces**:
  - `frontend/i18n/locales/en.json`
  - `frontend/i18n/locales/vi.json`
  - `frontend/pages/library.vue`
  - `frontend/pages/review.vue`
  - `frontend/components/review/FlashcardBentoCard.vue`
  - `frontend/components/review/FlashcardHeroCard.vue`
  - `frontend/components/review/MasteryGaugeCard.vue`
  - `frontend/components/review/ReviewForecastChart.vue`
  - `frontend/pages/insights.vue`
- **Dependencies**: No new external dependencies; leverages existing Nuxt `@nuxtjs/i18n`, UnoCSS/Tailwind, and Lucide icons.
- **Breaking Changes**: None. API contracts and Pinia stores remain untouched.
