# Spec Delta: system-layout-archetypes (Phase 1 Catalog Browsing)

## ADDED Requirements

### Requirement: Unboxed Direct-Canvas Catalog Browsing Standard
The system SHALL standardize catalog and content browsing surfaces (`library.vue`, `review.vue` Tab 2: Deck Management, and `insights.vue`) on the **Unboxed Direct-Canvas Layout Archetype**, prohibiting monolithic outer wrapper cards (`BoardLayout` or giant enclosing `glass-card`) that produce double-card nesting. Functional tiers (Header/Bento stats, Filter bars, Auto-flowing 3-column grids, and Pagination) MUST sit directly on the page background canvas (`bg-slate-50 dark:bg-canvas`) within a consistent container (`max-w-7xl mx-auto`).

#### Scenario: Desktop 1080p unboxed catalog grid distribution
- **WHEN** a user accesses the library or review deck management page on a desktop viewport ($W \ge 1280\text{px}$)
- **THEN** content cards MUST distribute across a 3-column responsive auto-flowing grid (`xl:grid-cols-3 gap-4`) sitting directly on the canvas without an outer enclosing card container or nested vertical scrollbars.

#### Scenario: Mobile and tablet responsive degradation
- **WHEN** a user views an unboxed catalog browsing surface on tablet ($768\text{px} \le W < 1280\text{px}$) or mobile ($W < 768\text{px}$)
- **THEN** the grid MUST automatically collapse to 2 columns on tablet and 1 column on mobile without clipping content cards.

### Requirement: Finalized Preview UI Micro-Component Parity
The system SHALL implement production components for Phase 1 catalog surfaces matching the structural, typographical, and density specifications finalized and verified in the Playground Preview UI (`playground/temp.vue`):
1. **Book Cards (`library.vue`)**: MUST feature `p-5 rounded-2xl` hairline borders, category badge, slice bookmark counter with Bookmark icon, title, author/source URL, status pill, progress bar, format badge, and compact reading action button with `GraduationCap` icon.
2. **Flashcard Inventory Cards (`FlashcardBentoCard.vue`)**: MUST feature `p-4 rounded-xl` hairline borders, urgency badge (`Due Today` / `Mastered` / `Learning`), SM-2 algorithm metrics (`EF: x.xx • Interval: xd`), bold question, 2-line answer preview (`line-clamp-2`), source citation, and compact action controls (`Details →`, Edit, Reset, Delete).
3. **Bento Stat Cards (`FlashcardHeroCard.vue`, `MasteryGaugeCard.vue`, `ReviewForecastChart.vue`)**: MUST feature unified `min-h-[190px]` vertical rhythm, gradient hero styling with high-contrast white CTA button, semi-circular SVG mastery gauge, and 7-day mini bar chart with tooltips.

#### Scenario: Rendering flashcard inventory cards in deck management
- **WHEN** the user views flashcards in Deck Management (`review.vue` Tab 2)
- **THEN** each card MUST render with the compact, hairline-bordered layout showing urgency badge, SM-2 metrics, 2-line answer summary, and source attribution without large collapsible accordion wrappers.

### Requirement: Phase 1 Bilingual i18n Completeness & Zero Hardcoded Strings
Every user-facing label, status, counter, unit, and empty state across Phase 1 catalog browsing surfaces MUST be resolved via `$t` through comprehensive translation dictionaries in `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`. Hardcoded language literals (e.g. `'In Progress'`, `'Ready'`, `'Completed'`, `'Processing'`, `'Loading flashcard library...'`, `'min'`, `'Lát cắt'`) are strictly prohibited.

#### Scenario: Switching locales on book progress status
- **WHEN** the user switches the active locale between English (`en`) and Vietnamese (`vi`)
- **THEN** book reading statuses MUST reactively display localized equivalents (e.g. "In Progress" in English vs "Đang đọc" in Vietnamese, and "Slice 8/23 (35%)" in English vs "Lát cắt 8/23 (35%)" in Vietnamese).

#### Scenario: Switching locales on flashcard urgency and time estimates
- **WHEN** the user views the review deck in Vietnamese
- **THEN** urgency badges MUST display "Đến Hạn Hôm Nay" / "Đã Thuộc" / "Đang Rèn Luyện", and the time estimate MUST display "phút" rather than "min".

### Requirement: Bilingual Action Button Density Invariant
Action buttons, tags, and badge indicators within catalog cards and filter toolbars MUST specify `whitespace-nowrap shrink-0` and hairline border styling (`border-slate-200/80 dark:border-white/[0.08]`) to prevent text wrapping and visual collisions across both English and Vietnamese locales.

#### Scenario: Viewing bilingual button labels
- **WHEN** switching between English and Vietnamese locales on any catalog surface
- **THEN** action buttons (such as "Continue Reading" / "Đọc tiếp" and "Import Document" / "Nhập tài liệu") MUST maintain single-line layout without label wrapping.
