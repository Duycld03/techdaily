# Design: Standardize Library and Notes Header Layout & Typography

## Context

The platform design system (`quiz.vue`, `insights.vue`) establishes consistent visual standards for page headers:
- Icon Badge: `w-10 h-10 rounded-2xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center shrink-0`
- Icon Glyph: `w-5 h-5 sm:w-6 sm:h-6` with `:stroke-width="1.5"`
- Title (`<h1>`): `text-xl sm:text-2xl font-black tracking-tight text-slate-900 dark:text-white`
- Subtitle (`<p>`): `text-xs sm:text-sm text-slate-500 dark:text-slate-400 font-medium`

Currently, `library.vue` and `notes.vue` deviate from this standard:
- `library.vue` uses an undersized `w-9 h-9 rounded-xl` container with `text-base sm:text-lg font-bold` heading.
- `notes.vue` uses a tiny `w-8 h-8 rounded-xl` container with `text-sm sm:text-base font-bold` heading.
- Furthermore, `notes.vue` leaves the right side of its `#header` slot empty while cramming both the full-text search input and the horizontal tag filter chips into `#filters`, creating visual clutter on mobile and an empty void on 1080p desktop screens.

## Goals / Non-Goals

**Goals:**
- Unify `library.vue` and `notes.vue` on a 2-Tier Header Layout architecture:
  - **Tier 1 (Header Top Bar)**: Left has standardized Icon Badge + Title + Subtitle; Right has standardized Search Input (`h-10 rounded-xl`) + Primary Action Button (Library: `+ Thêm Tài Liệu`).
  - **Tier 2 (Filter Chips Bar)**: Full container width dedicated exclusively to horizontal filter chips (Categories in Library, Tags in Notes).
- Harmonize typography, icon containers, and search inputs to match the platform standard (`w-10 h-10 rounded-2xl`, `text-xl sm:text-2xl font-black`, `rounded-xl h-10`).
- Ensure rock-solid responsiveness on both Mobile (390px) and Desktop (1080p/1440p) in English and Vietnamese.

**Non-Goals:**
- Changing note card showcase layouts or details modals.
- Changing library book card grids, reading logic, or ingestion modals.
- Backend API, DTO, or database schema modifications.

## Decisions

### 1. Unified 2-Tier Catalog Header Pattern

Both `library.vue` (direct canvas) and `notes.vue` (`BoardLayout flat`) will adopt the exact same 2-tier visual rhythm:

```
TIER 1: HEADER TOP BAR (Row 1)
+-----------------------------------------------------------------------------------------------------+
| [Icon w-10] Title (text-xl sm:text-2xl font-black)          [ Search Input (h-10) ] [ + CTA Button ] |
|             Subtitle (text-xs sm:text-sm)                                                           |
+-----------------------------------------------------------------------------------------------------+

TIER 2: FILTER CHIPS BAR (Row 2)
+-----------------------------------------------------------------------------------------------------+
| [ Tất Cả (N) ] [ Chip 1 ] [ Chip 2 ] [ Chip 3 ] [ Chip 4 ] [ Chip 5 ] ... (overflow-x-auto)         |
+-----------------------------------------------------------------------------------------------------+
```

### 2. Slot Mapping in `notes.vue` (`BoardLayout`)

- **`#header` Slot**:
  - Contains a responsive flex container: `flex flex-col sm:flex-row sm:items-center justify-between gap-3 w-full`.
  - Left child: `flex items-center gap-3` with `w-10 h-10 rounded-2xl` icon container, `<Highlighter class="w-5 h-5 sm:w-6 sm:h-6" :stroke-width="1.5" />`, `<h1>`, and `<p>`.
  - Right child: `v-if="notesStore.highlights.length > 0"` search box (`relative w-full sm:w-72`) with `h-10 rounded-xl` input, magnifying glass icon, and clear button.
- **`#filters` Slot**:
  - Contains `v-if="notesStore.highlights.length > 0"` tag chips row with `w-full overflow-x-auto no-scrollbar py-0.5`.
  - Tags now have the entire container width to scroll smoothly without colliding with or being compressed by the search input.

### 3. Sizing and Typography Token Harmonization in `library.vue`

- Icon container: Update from `w-9 h-9 rounded-xl` to `w-10 h-10 rounded-2xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20`.
- Icon glyph: Update from `<BookOpen class="w-5 h-5">` to `<BookOpen class="w-5 h-5 sm:w-6 sm:h-6" :stroke-width="1.5" />`.
- Title: Update from `text-base sm:text-lg font-bold` to `text-xl sm:text-2xl font-black tracking-tight text-slate-900 dark:text-white`.
- Subtitle: Standardize to `text-xs sm:text-sm text-slate-500 dark:text-slate-400 font-medium`.
- Search Input: Set height explicitly to `h-10` and ensure `rounded-xl` matching the "+ Thêm Tài Liệu" button height (`h-10`).
- Category filter row: Preserved on Row 2 with `flex flex-wrap items-center gap-2 w-full py-0.5`.

## Risks / Trade-offs

- **Mobile Viewport Wrapping**: On 390px screens, placing Title and Search on Tier 1 could cause vertical stacking if not wrapped cleanly.
  - *Mitigation*: The container uses `flex-col sm:flex-row sm:items-center justify-between gap-3 w-full`. On mobile, it stacks naturally: Left section on top, full-width search input underneath, followed by Tier 2 filter chips.
- **Vitest Test Breakage**: Tests asserting specific DOM structures or class existence.
  - *Mitigation*: Keep all functional attributes (`data-testid`, `v-model`, `@click`, `@keyup.enter`) identical. No test theatrical CSS assertions are touched.
