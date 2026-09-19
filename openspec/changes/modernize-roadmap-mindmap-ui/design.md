# Design: Modernize Roadmap & Interactive Mindmap Canvas UI

## Context

See `proposal.md` for motivation. The Roadmap page (`frontend/pages/roadmap.vue`) and its child components (`RoadmapViewSwitcher.vue`, `RoadmapMindmapCanvas.vue`) currently implement the 30-Day Senior Curriculum and active book milestones using legacy slate colors (`dark:bg-slate-800`, `dark:bg-slate-900`, `dark:bg-slate-950`, `dark:border-slate-700/800`).

This technical design defines the migration paths, token mapping invariants, and component refactoring specifications to achieve 100% visual coherence with the Dev-Learning Studio design system.

## Goals / Non-Goals

**Goals:**
- Replace 100% of legacy slate surface, border, and background tokens across `frontend/pages/roadmap.vue`, `RoadmapViewSwitcher.vue`, and `RoadmapMindmapCanvas.vue`.
- Unify the dual-view switcher into a sleek `.glass-panel` segmented tab control.
- Modernize the tree mindmap canvas container to neutral obsidian `dark:bg-canvas`, floating search/toolbars to `.glass-panel`, and tree cards to `dark:bg-canvas-subtle`.
- Preserve all existing gestures (smooth window-level drag-pan, wheel zoom, search filtering, branch accordion, 1-click bridge actions).
- Maintain 100% pass rate on existing unit tests.

**Non-Goals:**
- Modifying tree coordinate calculation algorithms in `frontend/utils/roadmapTreeLayout.ts`.
- Altering Pinia stores (`useRoadmapStore`, `useDailyFocusStore`, `useLibraryStore`).
- Introducing new third-party canvas or layout libraries.

## Decisions

### 1. Token Migration Invariants & Mapping

| Legacy Class | Modern Studio Equivalent | Semantic Meaning |
|---|---|---|
| `dark:bg-slate-950` / `dark:bg-slate-950/70` | `dark:bg-canvas` (`#09090b`) | Main deep obsidian canvas surface |
| `dark:bg-slate-900` / `dark:bg-slate-900/95` | `dark:bg-canvas-subtle` (`#121215`) or `.glass-panel` | Base card surface, panel background |
| `dark:bg-slate-800` / `dark:bg-slate-800/80` | `dark:bg-canvas-elevated` (`#18181b`) | Elevated nodes, active tabs, buttons |
| `dark:border-slate-800` / `dark:border-slate-700` | `dark:border-white/[0.08]` (`.hairline-border`) | Translucent hairline borders |
| Progress track: `dark:bg-slate-800` | `dark:bg-canvas-subtle` or `dark:bg-white/[0.06]` | Subtle sunken progress track |
| Button hover: `dark:hover:bg-slate-800` | `dark:hover:bg-white/[0.06]` | Modern translucent hover interaction |

### 2. Segmented Dual-View Switcher (`RoadmapViewSwitcher.vue`)

- **Container:**
  ```vue
  <div
    role="tablist"
    class="inline-flex items-center p-1.5 glass-panel border border-slate-200/80 dark:border-white/[0.08] rounded-2xl shadow-inner w-fit select-none"
  >
  ```
- **Active Button:**
  ```vue
  modelValue === mode
    ? 'bg-white dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 shadow-sm border border-transparent dark:border-white/[0.06]'
    : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
  ```
- **Rationale**: Uses standard `.glass-panel` styling consistent with navigation bars and mode switchers across the application.

### 3. Mindmap Canvas & Floating Controls (`RoadmapMindmapCanvas.vue`)

- **Canvas Viewport Container:**
  ```vue
  <div
    ref="containerRef"
    class="relative w-full h-[620px] sm:h-[720px] rounded-3xl bg-slate-50/80 dark:bg-canvas border border-slate-200/90 dark:border-white/[0.08] overflow-hidden select-none transition-colors"
  >
  ```
- **Floating Search Bar & Toolbars:**
  - Convert from opaque `bg-white/95 dark:bg-slate-900/95 dark:border-slate-800` to `.glass-panel border-slate-200/90 dark:border-white/[0.08] glow-subtle`.
- **Chapter Branch & Slice Leaf Cards:**
  - Inactive Chapter cards: `bg-white dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] hover:border-brand-500/40`.
  - Inactive Slice cards: `bg-white dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08]`.
  - Active Slice cards: `bg-brand-50/80 dark:bg-brand-950/40 border-brand-500/60 ring-2 ring-brand-500/30 glow-subtle`.

### 4. Timeline Milestones & Tracks (`frontend/pages/roadmap.vue`)

- **Header Banner & Track Switcher:**
  - Track menu popover: `bg-white dark:bg-canvas-elevated border-slate-200 dark:border-white/[0.08] shadow-2xl`.
  - Book track list items: hover states `dark:hover:bg-white/[0.06]`, active states `bg-brand-500/10 border-brand-500/30 text-brand-300`.
- **Timeline Milestone Cards:**
  - Accordion headers: `dark:bg-canvas-subtle border-slate-200/80 dark:border-white/[0.08]`.
  - Daily slice items: `dark:bg-canvas-subtle border-slate-200/80 dark:border-white/[0.08]`.
  - Slice badges: completed `#` badge `bg-emerald-500 text-white`, upcoming `#` badge `bg-slate-100 dark:bg-canvas-elevated text-slate-400`.
  - Progress tracks: `bg-slate-100 dark:bg-canvas-subtle rounded-full overflow-hidden`.

## Risks / Trade-offs

- **Unit Test Selectors**:
  - Tests in `RoadmapMindmapCanvas.spec.ts` and `RoadmapViewSwitcher.spec.ts` might assert specific class names or aria roles.
  - *Mitigation*: Existing tests rely on `data-testid` attributes (`data-testid="btn-zoom-in"`, `data-testid="mindmap-search-input"`, `tab-timeline`, `tab-mindmap`), which remain 100% preserved. Any class assertions (e.g. active tab styling) will be verified and aligned.
