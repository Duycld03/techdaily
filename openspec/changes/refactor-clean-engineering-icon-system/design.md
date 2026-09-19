# Design: Standardize Lucide Icon System and Streamline Studio Visuals

## Context

Across the `frontend/` codebase, `lucide-vue-next` is imported across 42+ files. While Lucide is the optimal library for developer tools, current usage suffers from:
1. Default 2px stroke weight appearing heavy and chunky on high-contrast obsidian (`#09090b`) dark surfaces.
2. `Sparkles` being misused as a spinning loading indicator (`<Sparkles class="animate-spin" />`) in 3 files (`AISynthesisCard.vue`, `DocReaderPane.vue`, `pages/read/[bookId].vue`), creating a gimmicky, carnival impression.
3. Navigation using `Sparkles` for Insights instead of an analytical/exploration metaphor (`Compass`).
4. Milestone and stat widgets using a scatter of rainbow colors (`text-violet-500`, `text-emerald-500`, `text-sky-500`, `text-amber-500`) with unstyled raw icons.

## Goals / Non-Goals

**Goals:**
- Establish a refined 1.5px stroke weight standard (`:stroke-width="1.5"`) on core interactive icons, navigation links, and Bento cards.
- Enforce a 3-tier size hierarchy:
  - Micro (`w-3.5 h-3.5` / 14px) for inline tags and metadata chips.
  - Standard (`w-4 h-4` / 16px) for navigation links, action buttons, and inputs.
  - Feature Tile (`w-5 h-5` / 20px) for prominent card and section headers.
- Standardize the translucent icon tile container: `p-2.5 rounded-2xl bg-brand-500/10 border border-brand-500/20 text-brand-600 dark:text-brand-400 shrink-0`.
- Replace all spinning sparkle loading animations with clean engineering loaders (`<Loader2 class="animate-spin" />`).
- Reassign Insights navigation from `Sparkles` to `Compass` in `AppHeader.vue`, `AppSidebar.vue`, and `AppCommandPalette.vue`.
- Harmonize milestone cards (`EngineerMilestonesCard.vue`) to studio neutral and brand palette tokens.

**Non-Goals:**
- Removing or altering confetti functionality (canvas-confetti is preserved for review and drill completion).
- Migrating to a different icon library.

## Decisions

### 1. Stroke Weight Standard (`:stroke-width="1.5"`)

- **Decision**: Set `:stroke-width="1.5"` on primary navigation, feature bento cards, reader headers, and interactive action buttons.
- **Rationale**: The default 2px stroke weight was designed for light mode interfaces. Against obsidian `#09090b` with high-contrast text, 2px strokes appear heavy and distracting. Reducing to 1.5px creates the sharp, precise, geometric aesthetic popularized by Linear, Raycast, and Vercel.

### 2. Semantic Loading State Replacement

- **Decision**: Replace `<Sparkles class="animate-spin" />` in `AISynthesisCard.vue`, `DocReaderPane.vue`, and `[bookId].vue` with `<Loader2 class="animate-spin" />`.
- **Rationale**: A spinning star icon conveys magical randomness rather than a deterministic engineering process. `Loader2` is the recognized standard for technical platform operations.

```html
<!-- Before -->
<Sparkles class="w-6 h-6 text-brand-600 dark:text-brand-400 animate-spin" />

<!-- After -->
<Loader2 class="w-6 h-6 text-brand-600 dark:text-brand-400 animate-spin" :stroke-width="1.5" />
```

### 3. Purposeful Navigation & Insights Iconography

- **Decision**: Replace `Sparkles` with `Compass` for the Insights route in `AppHeader.vue`, `AppSidebar.vue`, and `AppCommandPalette.vue`.
- **Rationale**: Insights is an analytical engineering radar and knowledge explorer. `Compass` accurately conveys exploration and system orientation, whereas `Sparkles` connotes generic AI chat generation.

### 4. Milestone Cards Palette Normalization

- **Decision**: In `EngineerMilestonesCard.vue`, replace raw rainbow colors (`text-violet-500`, `text-emerald-500`, `text-sky-500`, `text-amber-500`) with standardized tile containers:
  ```html
  <div class="w-8 h-8 rounded-xl bg-slate-100 dark:bg-white/[0.04] border border-slate-200/80 dark:border-white/[0.08] flex items-center justify-center shrink-0 text-slate-700 dark:text-slate-300">
    <Component :is="icon" class="w-4 h-4" :stroke-width="1.5" />
  </div>
  ```
- **Rationale**: Eliminates visual noise and rainbow clutter while maintaining clear card identity through concise typographic labels and progress bars.

## Risks / Trade-offs

- **Manual `:stroke-width` Propagation**: Lucide icons accept `stroke-width` as a prop. Setting it on key structural surfaces provides immediate visual refinement without requiring a global wrapper component.
