## Context

The reader component (`frontend/pages/read/[bookId].vue`) currently renders a horizontal navigation row at the bottom of the article. When the chapter title of the next slice is long, interpolating it inside an inline button causes visual skewing: the next button grows disproportionately wide while the previous button and progress counter get squeezed, breaking their text across multiple lines.

See `proposal.md` for user motivation and problem breakdown.

## Goals / Non-Goals

**Goals:**
- Implement a symmetrical two-card navigation component (`grid grid-cols-1 sm:grid-cols-2 gap-4`) at the footer of each reader slice.
- Display structured card headers (uppercase action labels) and truncated chapter titles so long text gracefully truncates (`truncate`) rather than causing layout reflows or button deformation.
- Anchor the Next card to the right column on desktop even when on the first slice (using `sm:col-start-2` when `activeChunkIndex === 0`).
- Ensure 100% responsiveness on mobile viewports (<640px) with comfortable full-width touch targets.
- Maintain bilingual responsive layout invariants (`whitespace-nowrap shrink-0` on action labels, verified in both English and Vietnamese).

**Non-Goals:**
- Changing global reader layout or top bar navigation.
- Introducing a floating sticky bottom bar (opted for documentation-standard in-page symmetrical card grid).
- Modifying backend APIs or database schemas.

## Decisions

### 1. Two-Column Grid vs Flexbox
- **Choice:** CSS Grid with `grid grid-cols-1 sm:grid-cols-2 gap-4`.
- **Rationale:** Grid enforces strict 50/50 width parity on desktop viewports (≥640px) regardless of whether one chapter title is 5 characters and the other is 80 characters.
- **Alternative Considered:** `flex justify-between`. Flexbox allows sibling elements to shift width dynamically unless strict `basis-1/2` is forced, increasing the risk of lopsided buttons.

### 2. Card Visual Hierarchy
- **Previous Card (Left):**
  - Sub-label: `← ChevronLeft` + `$t('reader.prev_slice_card_label')` (`text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400`).
  - Title: `prevChunk?.chapterTitle` (`text-sm sm:text-base font-bold text-slate-800 dark:text-slate-200 truncate mt-1`).
- **Next Card (Right):**
  - Sub-label: `$t('reader.next_slice_card_label')` + `ChevronRight →` (`text-xs font-bold uppercase tracking-wider text-brand-600 dark:text-brand-400`).
  - Title: `nextChunk?.chapterTitle` (or `$t('reader.return_library')` when finished) (`text-sm sm:text-base font-bold text-slate-900 dark:text-white truncate mt-1`).
  - Styled with subtle brand accent border on dark mode (`border-brand-500/30 hover:border-brand-500/70 bg-brand-50/20 dark:bg-brand-950/20`).

### 3. First Slice Placement
- **Choice:** When `activeChunkIndex === 0`, the Previous card is omitted and the Next card takes `sm:col-start-2`.
- **Rationale:** Keeps the primary action (Next Slice) consistently on the right-hand side where users expect it, without awkward left-shifting.

### 4. Progress Summary Placement
- **Choice:** Render a subtle meta row immediately above the cards:
  - Left: Progress count (`9 / 141 lát cắt (6%)`).
  - Right: Reading status indicator (`Đang đọc` / `Hoàn thành`).

## Risks / Trade-offs

- **[Risk]** Very long chapter titles might get truncated prematurely on small screens.
  - **Mitigation:** The active chapter title is already prominent in the H1 and top navigation. Symmetrical cards prioritize navigation safety and clarity over rendering 100+ character strings in full. A native browser `title="..."` attribute will provide the full tooltip on hover.
