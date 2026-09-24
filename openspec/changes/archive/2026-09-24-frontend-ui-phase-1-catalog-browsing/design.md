# Design: Frontend UI Phase 1 - Content & Catalog Browsing (Finalized Preview UI to Production & Complete i18n)

## Context

TechDaily's catalog and browsing pages (`library.vue`, `review.vue` Tab 2: Deck Management, and `insights.vue`) previously wrapped content in disparate bounds or attempted to enclose the entire page within a single monolithic card (`BoardLayout`).

Through rapid prototyping and user feedback in `frontend/pages/playground/temp.vue`, the user has formally finalized and approved the **Preview UI**. The approved preview establishes:
1. An **Unboxed Direct-Canvas Layout Archetype** sitting directly on `bg-slate-50 dark:bg-canvas`.
2. High-density, developer-grade **Micro-Component Cards** (Book Cards and Flashcard Inventory Cards) with hairline borders, concise metadata, and clean action buttons.
3. Complete **Bilingual Internationalization (i18n)** eliminating hardcoded English and Vietnamese strings.

## Goals / Non-Goals

**Goals:**
- Eliminate outer wrapper card double-nesting on `library.vue` and `review.vue` (Tab 2: Deck Management).
- Transition the finalized Preview UI from `frontend/pages/playground/temp.vue` into production components with 100% fidelity.
- Refactor `FlashcardBentoCard.vue` into a sleek, compact card matching the preview (urgency badge, SM-2 metrics, 2-line answer summary, source attribution, and detail modal trigger).
- Refactor Bento overview cards (`FlashcardHeroCard.vue`, `MasteryGaugeCard.vue`, `ReviewForecastChart.vue`) and filter toolbar in `review.vue`.
- Audit and implement complete bilingual i18n dictionaries in `en.json` and `vi.json` to eliminate all hardcoded strings (e.g. `'In Progress'`, `'Ready'`, `'Completed'`, `'Processing'`, `'Loading flashcard library...'`, `'min'`, `'Lát cắt'`).
- Ensure all existing unit tests in `library.spec.ts` and `review.spec.ts` pass, and verify visual rendering in both Light and Dark modes.

**Non-Goals:**
- Modifying interactive practice surfaces (`quiz.vue`, `review.vue` Tab 1 Flashcards) — deferred to Phase 2.
- Modifying reading cockpit or TOC navigation (`read/[bookId].vue`) — deferred to Phase 3.
- Modifying visual canvases (`roadmap.vue`, `graph.vue`) or shell chrome — deferred to Phases 4 & 5.

## Decisions

### 1. Functional Tier Allocation per Catalog Page (Unboxed)

| Page | Tier 1: Header / Bento Stats | Tier 2: Filters & Search | Tier 3: Content Grid (3-Col) | Tier 4: Pagination |
|---|---|---|---|---|
| `library.vue` | Title, Stats summary, Search bar, "Import Document" action | Category chips, Sort dropdown | 3-column Book Card grid (`xl:grid-cols-3 gap-4`) | `BasePagination` component |
| `review.vue` (Tab 2) | 3 Bento stat cards directly on canvas (Due Hero, Mastery Gauge, 7-Day Forecast) | Left: Quick filter chips (`All`, `Due`, `Mastered`); Right: Search input (⌘K) | Multi-column Flashcard Inventory grid (`xl:grid-cols-3 gap-4`) | Deck pagination controls |
| `insights.vue` | Header banner (Title, Seniority, Bookmark count, Shuffle, AI Generate) | View switcher (Explore vs. Saved), Topic filter chips | Telemetry chips, Summary prose, Unified Code Block, Deep Dive | Pagination / Navigation footer |

### 2. Component Refactoring Specifications

#### A. `FlashcardBentoCard.vue`
- **Shell**: `p-4 rounded-xl border border-slate-200/90 dark:border-white/[0.08] bg-white dark:bg-canvas-subtle hover:border-brand-500/40 transition-all flex flex-col justify-between space-y-3 shadow-sm hover:shadow-md`.
- **Top Row**:
  - Urgency Pill:
    - Due today: `bg-rose-500/10 text-rose-500 border-rose-500/20` (`review.card_urgency_due`).
    - Mastered: `bg-emerald-500/10 text-emerald-500 border-emerald-500/20` (`review.card_urgency_mastered`).
    - Learning: `bg-amber-500/10 text-amber-500 border-amber-500/20` (`review.card_urgency_learning`).
  - Metrics & Controls: `EF: {ef} • {intervalText}` (`review.card_ef_interval`), plus subtle inline action icons for Edit, Reset, and Delete.
- **Body**:
  - Question: `<h4 class="text-xs sm:text-sm font-bold text-slate-900 dark:text-white leading-snug">`.
  - Answer: `<p class="text-xs text-slate-500 dark:text-slate-400 line-clamp-2">` providing a clean preview without bulky accordion expanders.
- **Footer**: Hairline `border-t border-slate-100 dark:border-white/[0.06] pt-2 flex items-center justify-between text-xs`:
  - Source: `Nguồn: {source}` / `Source: {source}` with `truncate max-w-[180px]`.
  - Action: `Chi tiết →` / `Details →` button emitting click to open full inspection/flip modal.

#### B. `FlashcardHeroCard.vue`
- Shell: `rounded-2xl bg-gradient-to-br from-brand-600 to-indigo-700 text-white p-5 shadow-lg shadow-brand-500/10 flex flex-col justify-between h-full min-h-[190px]`.
- Background glow: Subtle radial gradients (`blur-2xl`).
- Stats counter: Large bold count + localized `cards due` label.
- CTA Button: `w-full h-9 px-4 rounded-xl bg-white text-brand-700 hover:bg-slate-100 font-bold text-xs sm:text-sm flex items-center justify-center gap-2 shadow-md transition-all active:scale-[0.98]` with `Zap` and `ChevronRight` icons.
- Time estimate: `~{minutes} phút` in Vietnamese and `~{minutes} min` in English via `$t('review.time_min', { minutes })`.

#### C. `MasteryGaugeCard.vue`
- Shell: `rounded-2xl glass-card text-slate-900 dark:text-white p-5 shadow-sm flex flex-col justify-between h-full min-h-[190px]`.
- Semi-circular SVG gauge: Arc radius 45, circumference 141.37, displaying calculated mastery percentage (e.g. `78% Làm Chủ` / `78% Mastered`).
- Mastery badge: `Bậc Cao Thủ` / `Master` tier chip.
- Footer summary: Total cards count + weekly trend (`+12% tuần này` / `+12% this week`).

#### D. `ReviewForecastChart.vue`
- Shell: `rounded-2xl glass-card text-slate-900 dark:text-white p-5 shadow-sm flex flex-col justify-between h-full min-h-[190px]`.
- 7-Day bar distribution with hover tooltips and dynamic gradient for today's bar.
- Footer summary: Peak day and daily average load (`Đỉnh điểm: {count} thẻ` / `Peak: {count} cards`).

#### E. Deck Filter & Search Bar (`review.vue`)
- Left: Quick filter chips (`Tất Cả Thẻ ({count})`, `Cần Ôn Hôm Nay ({count})`, `Đã Thuộc ({count})`).
- Right: Search input with `Search` icon on left and `⌘K` keyboard shortcut badge on right.

### 3. Comprehensive Phase 1 i18n Dictionary Expansion

| Namespace | Key | English (`en.json`) | Vietnamese (`vi.json`) |
|---|---|---|---|
| `library` | `status_ready` | "Ready" | "Sẵn sàng" |
| `library` | `status_in_progress` | "In Progress" | "Đang đọc" |
| `library` | `status_completed` | "Completed" | "Đã hoàn thành" |
| `library` | `status_processing` | "Processing" | "Đang xử lý" |
| `library` | `slice_progress` | "Slice {current}/{total} ({percent}%)" | "Lát cắt {current}/{total} ({percent}%)" |
| `library` | `slices_total` | "{count} slices" | "{count} lát cắt" |
| `review` | `loading_deck` | "Loading flashcard library..." | "Đang tải kho thẻ ghi nhớ..." |
| `review` | `empty_deck_hint` | "Try clearing search filters to see all cards in your library." | "Thử xoá bộ lọc tìm kiếm để xem tất cả thẻ trong kho." |
| `review` | `time_min` | "{minutes} min" | "{minutes} phút" |
| `review` | `filter_all` | "All Cards ({count})" | "Tất Cả Thẻ ({count})" |
| `review` | `filter_due` | "Due Today ({count})" | "Cần Ôn Hôm Nay ({count})" |
| `review` | `filter_mastered` | "Mastered ({count})" | "Đã Thuộc ({count})" |
| `review` | `card_urgency_due` | "Due Today" | "Đến Hạn Hôm Nay" |
| `review` | `card_urgency_mastered` | "Mastered" | "Đã Thuộc" |
| `review` | `card_urgency_learning` | "Learning" | "Đang Rèn Luyện" |
| `review` | `card_ef_interval` | "EF: {ef} • Interval: {interval}d" | "EF: {ef} • Chu kỳ: {interval} ngày" |
| `review` | `card_source` | "Source: {source}" | "Nguồn: {source}" |
| `review` | `card_details_btn` | "Details →" | "Chi tiết →" |
| `review` | `mastery_tier_master` | "Master" | "Bậc Cao Thủ" |
| `review` | `mastery_label` | "Mastered" | "Làm Chủ" |
| `review` | `mastery_weekly_trend` | "+{percent}% this week" | "+{percent}% tuần này" |
| `review` | `forecast_title_7d` | "Review Load Forecast (7 Days)" | "Dự Báo Tải Ôn Tập (7 Ngày)" |
| `review` | `forecast_desc_7d` | "Daily due card distribution" | "Phân bổ thẻ đến hạn theo ngày" |
| `review` | `forecast_peak_day` | "Peak: {count} cards ({day})" | "Đỉnh điểm: {count} thẻ ({day})" |
| `review` | `forecast_daily_average` | "~{count} cards/day" | "~{count} thẻ/ngày" |

## Risks / Trade-offs

- **Risk: Vitest test assertion regressions on DOM classes and text content**:
  - *Mitigation*: Update existing test cases in `library.spec.ts`, `review.spec.ts`, and component test suites to expect the localized strings and new element structures, ensuring all 432+ unit tests continue to pass.
- **Risk: Text wrapping on small mobile screens**:
  - *Mitigation*: Enforce `whitespace-nowrap shrink-0` on badges, buttons, and chips with responsive `gap-2` and `overflow-x-auto` on filter bars.
