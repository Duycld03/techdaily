# Design: Refine Deck Filter Modal Layout & Sort Options Density

## Context

See `proposal.md` for problem statement and motivation.

In `frontend/components/review/AdvancedFilterModal.vue`, Sections 1 ("Knowledge Source"), 2 ("Mastery Stage"), and 3 ("Urgency Priority") were standardized on a clean, auto-wrapping pill chip architecture (`flex flex-wrap items-center gap-2`), which prevents text clipping for long phrases and preserves high visual density.

However, Section 4 ("Sort Options" / "Sắp xếp theo") was left using a 2-column grid (`grid grid-cols-1 sm:grid-cols-2 gap-2`) with left-aligned text (`text-left`) and full column width. Within a `max-w-2xl` modal container, each button stretches horizontally across ~300px, creating an awkward, elongated bar appearance that visually clashes with the compact chips directly above it.

## Goals / Non-Goals

**Goals:**
- Replace the rigid 2-column grid in Section 4 with the unified `flex flex-wrap items-center gap-2` chip layout.
- Align sort option button styling (`px-3.5 py-2 rounded-xl text-xs font-semibold border transition-all text-center whitespace-nowrap cursor-pointer`) with filter chips in Sections 1–3.
- Eliminate horizontal over-stretching while ensuring all sort options sit comfortably without clipping across English and Vietnamese locales.
- Ensure all existing unit tests in `reviewBentoComponents.spec.ts` continue to pass.

**Non-Goals:**
- Altering sorting algorithms, query parameters, or Pinia store logic in `useReviewStore.ts`.
- Changing other sections or modal footer actions.

## Decisions

### 1. Harmonized Chip Container Architecture
All four option sections in `AdvancedFilterModal.vue` will use identical container layout:
```html
<div class="flex flex-wrap items-center gap-2">
  <!-- Option chips -->
</div>
```
This guarantees consistent spacing (`gap-2`), natural content-driven button widths, and responsive multi-line wrapping when required.

### 2. Button Dimension & Typography Standardization
Section 4 sort buttons will transition from:
- **Old**: `grid-cols-1 sm:grid-cols-2`, `px-3.5 py-2 rounded-xl text-xs font-semibold border transition-all text-left whitespace-nowrap` (stretching full width of column).
- **New**: `flex-wrap`, `px-3.5 py-2 rounded-xl text-xs font-semibold border transition-all text-center whitespace-nowrap cursor-pointer` (sizing naturally to content width).

Visual states:
- **Active**: `bg-brand-600 text-white font-bold border-transparent shadow-sm`
- **Inactive**: `bg-slate-50 dark:bg-canvas-subtle text-slate-600 dark:text-slate-400 border-slate-200/80 dark:border-white/[0.06] hover:bg-slate-100 dark:hover:bg-white/[0.04]`

## Risks / Trade-offs

- **Risk: Button wrap points on narrow mobile viewports ($W < 400\text{px}$)**:
  - *Mitigation*: `flex-wrap` handles narrow widths gracefully, placing 2 chips per row as needed without horizontal clipping or container overflow.
