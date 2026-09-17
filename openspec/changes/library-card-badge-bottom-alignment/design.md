# Design: Library Card Status Badge Bottom Alignment

## Context

See `proposal.md` for background and user-reported visual defects.

In TechDaily's technical library (`frontend/pages/library.vue`), document cards are rendered inside a responsive grid:
```html
<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
```
CSS Grid items within a given row stretch by default (`align-items: stretch`) so that all cards in that row have equal computed height, matching whichever card contains the most content.

Each individual card is styled with:
```html
<div class="p-6 sm:p-7 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 hover:border-brand-400 dark:hover:border-slate-700 transition-all flex flex-col justify-between space-y-4 group shadow-md dark:shadow-sm">
```
Because of `flex flex-col justify-between`, space inside the card is distributed between:
1. The upper content container: currently an unstyled block `<div>`.
2. The card footer: `<div class="pt-4 border-t border-slate-100 dark:border-slate-800/80 ...">`.

When a row contains books with differing title lengths (e.g. 1 line vs 2 lines) or presence/absence of an author subtitle, the outer card containers expand to equal height, and the footer is pushed to the bottom. However, because the upper `<div>` lacks flex layout properties, the status badge (bookmark resume badge, ready badge, or processing ingestion indicator) sits immediately below the title/subtitle in normal block flow. Consequently:
- Cards with 2-line titles push their status badge down near the footer divider.
- Cards with 1-line titles leave a ~28px gap above the footer divider, causing their badges to float higher up and creating a ragged, misaligned horizontal baseline across the grid row.

---

## Goals / Non-Goals

**Goals:**
- Ensure bookmark badges (`bookmarks[book.id]`), ready badges (`book.status === 'Ready'`), and background ingestion indicators (`book.status === 'Processing'`) align at the exact same horizontal baseline across adjacent cards in any grid row.
- Support dynamic variations in content height (1-line titles, 2-line titles, presence or absence of `authorOrSourceUrl`) without causing badge baseline drift.
- Maintain seamless responsive adaptation across mobile (1 column), tablet (2 columns), and desktop (3 columns) viewports.
- Rely strictly on standard CSS Flexbox utility classes without hardcoding fixed pixel heights or requiring JavaScript layout measurements.
- Establish robust automated testing in Vitest verifying flex layout classes and DOM structure.

**Non-Goals:**
- Forcing a fixed height on `<h3>` titles (e.g. `h-[3.5rem]` or `min-h-[3rem]`), which would introduce awkward whitespace on single-column mobile layouts when titles are short.
- Relocating the status badge into the card footer action row. The footer is reserved for interactive actions (Delete, Export to Obsidian, Read CTA); document reading progress and processing status belong logically to content metadata.
- Modifying backend APIs, database schemas, or reading page components.
- Altering the badge visual styles, icons, color tokens, or localization strings.

---

## Decisions

### Decision 1: Flexbox Structure Analysis (`flex-1` and `mt-auto`)
- **Choice:** Convert the upper content wrapper to `<div class="flex flex-col flex-1">` and place all status badge elements inside `<div class="mt-auto pt-3">`.
- **Rationale:**
  1. In CSS Flexbox, applying `flex-1` (`flex: 1 1 0%`) to a direct child of a flex column (`flex flex-col`) instructs it to expand and fill all remaining vertical space inside the parent container.
  2. Setting `flex flex-col` on this upper wrapper establishes an internal flex formatting context.
  3. Inside a flex column container, setting `margin-top: auto` (`mt-auto`) on a child causes that element to absorb all unoccupied vertical space above it, pushing the element flush against the bottom of the flex container.
  4. The `pt-3` utility guarantees a minimum top clearance of `0.75rem` (12px) between the title/subtitle content and the badge, preserving visual breathing room even when a title wraps to 2 lines and an author subtitle is present.
- **Alternatives Considered:**
  - *Hardcoded minimum height on `<h3>` (e.g. `min-h-[56px]`):* Rejected because line-heights, OS font rendering, and subtitle presence still cause baseline discrepancies, and it leaves unwanted gaps on single-column mobile views.
  - *CSS Subgrid (`display: subgrid`):* Rejected due to inconsistent behavior across older browser engines and unnecessary complexity for a two-level card layout.
  - *JavaScript height synchronization:* Rejected as an anti-pattern for pure CSS layout problems; adds resize observers and runtime overhead.

```
┌─────────────────────────────────────────────────────────────┐
│ Card (.flex.flex-col.justify-between)                       │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ Upper Content (.flex.flex-col.flex-1)                   │ │
│ │  ┌───────────────────────────────────────────────────┐  │ │
│ │  │ Category Chip & Chunks Count                      │  │ │
│ │  │ Title (line-clamp-2 leading-snug)                 │  │ │
│ │  │ Subtitle (optional authorOrSourceUrl)             │  │ │
│ │  └───────────────────────────────────────────────────┘  │ │
│ │                                                         │ │
│ │  ↕ [flex-1 / mt-auto absorbs variable height here]      │ │
│ │                                                         │ │
│ │  ┌───────────────────────────────────────────────────┐  │ │
│ │  │ Status Container (.mt-auto.pt-3)                  │  │ │
│ │  │ [Bookmark Badge | Ready Badge | Processing Alert] │  │ │
│ │  └───────────────────────────────────────────────────┘  │ │
│ └─────────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ Card Action Footer (.pt-4.border-t)                     │ │
│ │ [Delete] [Export]                   [Continue / Read]   │ │
│ └─────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

---

### Decision 2: Component Markup Diffs in `frontend/pages/library.vue`
- **Choice:** Restructure lines 478–515 of `frontend/pages/library.vue` as follows:

```diff
-       <div>
+       <div class="flex flex-col flex-1">
          <div class="flex items-center justify-between gap-2 mb-3.5">
            <span class="px-3 py-1 rounded-lg bg-brand-100 dark:bg-brand-950/80 border border-brand-200 dark:border-brand-800/60 text-brand-800 dark:text-brand-300 text-xs font-bold">
              {{ getCategoryLabel(book.category) }}
            </span>
            <span class="text-xs text-slate-500 font-mono flex items-center gap-1">
              <Layers class="w-3.5 h-3.5" />
              {{ book.totalChunks }} {{ $t('library.chunks') }}
            </span>
          </div>

          <h3 class="text-lg font-bold text-slate-900 dark:text-white group-hover:text-brand-600 dark:group-hover:text-brand-300 transition-colors line-clamp-2 leading-snug">
            {{ book.title }}
          </h3>

          <p v-if="book.authorOrSourceUrl" class="text-xs sm:text-sm text-slate-500 mt-2 truncate font-mono">
            {{ book.authorOrSourceUrl }}
          </p>

+         <div class="mt-auto pt-3">
            <!-- Bookmark Badge if exists -->
-           <div v-if="bookmarks[book.id]" class="mt-3 inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-brand-50 dark:bg-brand-950/40 border border-brand-200 dark:border-brand-900 text-brand-700 dark:text-brand-300 text-xs sm:text-sm font-semibold">
+           <div v-if="bookmarks[book.id]" class="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-brand-50 dark:bg-brand-950/40 border border-brand-200 dark:border-brand-900 text-brand-700 dark:text-brand-300 text-xs sm:text-sm font-semibold">
              <Bookmark class="w-3.5 h-3.5 text-brand-500 fill-brand-500" />
              <span>{{ $t('library.resumes_at', { slice: bookmarks[book.id] }) }}</span>
            </div>
            <!-- Ready Badge if no bookmark -->
-           <div v-else-if="book.status === 'Ready' || (book.status as any) === 2" class="mt-3 inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-emerald-50 dark:bg-emerald-950/40 border border-emerald-200 dark:border-emerald-800 text-emerald-700 dark:text-emerald-300 text-xs sm:text-sm font-semibold">
+           <div v-else-if="book.status === 'Ready' || (book.status as any) === 2" class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-emerald-50 dark:bg-emerald-950/40 border border-emerald-200 dark:border-emerald-800 text-emerald-700 dark:text-emerald-300 text-xs sm:text-sm font-semibold">
              <CheckCircle2 class="w-3.5 h-3.5 text-emerald-500" />
              <span>{{ $t('library.ready_to_read') }}</span>
            </div>

            <!-- In-Progress Ingestion Indicator (Tier 1 Uploading) -->
-           <div v-if="book.status === 'Processing' || (book.status as any) === 1" class="mt-3.5 p-3 rounded-2xl bg-brand-500/10 dark:bg-brand-500/15 border border-brand-500/20">
+           <div v-if="book.status === 'Processing' || (book.status as any) === 1" class="p-3 rounded-2xl bg-brand-500/10 dark:bg-brand-500/15 border border-brand-500/20">
              <div class="flex items-center gap-2 text-xs font-semibold text-brand-700 dark:text-brand-300">
                <Loader2 class="w-3.5 h-3.5 text-brand-500 animate-spin shrink-0" />
                <span class="truncate">{{ getStatusMessage(book) }}</span>
              </div>
            </div>
+         </div>
        </div>
```
Note: The redundant `mt-3` and `mt-3.5` classes on the individual badge divs are safely removed or superseded by the parent container's `pt-3`, ensuring consistent spacing across all badge types.

---

### Decision 3: Responsive and Visual Consistency
- **Desktop Grid (`lg:grid-cols-3`):** Rows render 3 cards. Cards in the row match the height of the tallest card. `flex-1` and `mt-auto pt-3` guarantee badges on all 3 cards align perfectly at the same horizontal line.
- **Tablet Grid (`md:grid-cols-2`):** Rows render 2 cards. Paired cards match heights and align their status badges symmetrically.
- **Mobile Grid (`grid-cols-1`):** Single column. Each card's height is determined by its content. The `mt-auto pt-3` container cleanly places the badge directly above the footer divider without unwanted vertical stretching.

---

### Decision 4: Automated Testing Strategy
- **File:** `frontend/tests/pages/library.spec.ts`
- **Test Strategy:**
  1. Mount `LibraryPage` with mock books featuring:
     - Book 1: 1-line title with a bookmark badge.
     - Book 2: 2-line title with a bookmark badge.
     - Book 3: 1-line title without subtitle with `Ready` status.
     - Book 4: `Processing` status book.
  2. Assert that all rendered card upper wrappers contain the classes `flex`, `flex-col`, and `flex-1`.
  3. Assert that the status badge wrapper div contains the classes `mt-auto` and `pt-3`.
  4. Assert that no badge is rendered outside the `mt-auto` container.

---

## Risks / Trade-offs

- **[Risk] Margin Collapse or Spacing Conflict:** If child badges retain standalone top margins (`mt-3`, `mt-3.5`), spacing could slightly compound.
  - **Mitigation:** Remove inner top margins (`mt-3`, `mt-3.5`) on the child badge elements so that `pt-3` on the parent `<div class="mt-auto pt-3">` uniformly controls separation from the title block.
- **[Risk] Long Ingestion Status Messages in Processing Indicator:** Processing status messages may wrap to multiple lines on narrow screens.
  - **Mitigation:** The processing banner already has `truncate` on its text span and a fixed padding (`p-3`), preventing unexpected vertical expansion.

---

## Migration Plan

1. Edit `frontend/pages/library.vue` to update the upper content wrapper and wrap badge elements inside `<div class="mt-auto pt-3">`.
2. Update `frontend/tests/pages/library.spec.ts` with test assertions for `flex flex-col flex-1` and `mt-auto pt-3`.
3. Run `npm --prefix frontend test` to verify test suite health.
4. Rollback strategy: Revert git commit in `frontend/pages/library.vue`; no state, schema, or backend migration required.
