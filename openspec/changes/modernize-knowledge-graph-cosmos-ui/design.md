# Design: Modernize Knowledge Graph Cosmos & Today Studio UI

## Context

See `proposal.md` for motivation. The Knowledge Graph Cosmos (`frontend/pages/graph.vue`) and `/today` auxiliary components currently exhibit visual inconsistencies:
- The 3D WebGL ForceGraph engine is hardcoded to `#020617` (slate-950), which clashes with the global `#09090b` obsidian shell.
- Floating controls (`GraphControlBar.vue`, `GraphDetailDrawer.vue`, `GraphMinimap.vue`, `GraphLegend.vue`) rely on opaque slate-800/900 styles.
- Auxiliary cards on `/today` (`AISynthesisCard.vue`, `TermExplainerModal.vue`) use legacy slate containers.
- Drill options in `InterviewChallengePane.vue` use `text-xs` (12px) on mobile screens, violating the responsive typography standard.

## Goals / Non-Goals

**Goals:**
- Unify 2D Cytoscape and 3D WebGL canvas backgrounds to neutral obsidian `#09090b` (`dark:bg-canvas`).
- Convert floating control panels and drawers to standard `.glass-panel` and hairline borders (`border-white/[0.08]`).
- Clean up `/today` auxiliary components (`AISynthesisCard.vue`, `TermExplainerModal.vue`) with obsidian canvas tokens.
- Upgrade `InterviewChallengePane.vue` option text and badges to `text-sm sm:text-base` for mobile readability.
- Maintain 100% test pass rate across frontend unit tests.

**Non-Goals:**
- Modifying physics calculation algorithms in Cytoscape or Three.js.
- Altering relational graph generation queries in the ASP.NET Core backend.
- Adding new layout engine modes.

## Decisions

### 1. Canvas Background Harmonization

| Component | Current Style | Modern Studio Standard |
|---|---|---|
| `GraphCanvas3D.vue` (WebGL scene) | `dark ? '#020617' : '#f8fafc'` | `dark ? '#09090b' : '#f8fafc'` |
| `GraphCanvas3D.vue` (Sprite labels) | `rgba(15, 23, 42, 0.85)` | `rgba(18, 18, 21, 0.85)` |
| `GraphCanvas.vue` (Wrapper container) | `dark:bg-slate-950` | `dark:bg-canvas` |
| `GraphCanvas.vue` (Node dark borders/edges) | `#334155` | `#27272a` (hairline) |

### 2. Glassmorphic Control Surfaces (`GraphControlBar.vue`, `GraphMinimap.vue`, `GraphLegend.vue`)

- **`GraphControlBar.vue`**:
  - Replace `dark:bg-slate-800` on buttons and filter pills with `dark:bg-canvas-subtle` and active buttons with `dark:bg-canvas-elevated`.
  - Replace `dark:border-slate-700` with `dark:border-white/[0.08]`.
  - Replace button hover states with `dark:hover:bg-white/[0.06]`.
- **`GraphMinimap.vue`**:
  - Replace `dark:bg-slate-900/85` container with `.glass-panel dark:bg-canvas-subtle/85`.
  - Replace `dark:bg-slate-950/90` canvas with `dark:bg-canvas-elevated`.
- **`GraphLegend.vue`**:
  - Replace `dark:hover:bg-slate-800` on entity and mastery items with `dark:hover:bg-white/[0.06]`.

### 3. Detail Slide-Over Drawer (`GraphDetailDrawer.vue`)

- Replace container background `dark:bg-slate-900` with `dark:bg-canvas-subtle`.
- Replace drag handle and borders with `dark:border-white/[0.08]`.
- Upgrade metrics blocks, takeaways quote boxes, and connected nodes list to `dark:bg-canvas-elevated` and `dark:border-white/[0.08]`.

### 4. Auxiliary Components Clean-up (`/today`)

- **`AISynthesisCard.vue`**:
  - Convert root container to `.glass-panel dark:bg-canvas-subtle border-slate-200/90 dark:border-white/[0.08]`.
- **`TermExplainerModal.vue`**:
  - Convert dialog container to `dark:bg-canvas-elevated border-slate-200 dark:border-white/[0.08]`.
  - Update close button hover to `dark:hover:bg-white/[0.06]`.
- **`InterviewChallengePane.vue`**:
  - Update option letter badges: `text-xs sm:text-sm` $\rightarrow$ `text-sm sm:text-base`.
  - Update option prose text: `text-xs sm:text-sm` $\rightarrow$ `text-sm sm:text-base`.

## Risks / Trade-offs

- **Risk: WebGL Label Legibility**:
  - Changing 3D background to deep `#09090b` might affect text sprite contrast.
  - *Mitigation*: Emissive billboard text sprites already use high-contrast foreground colors (`#ffffff`, `#38bdf8`, `#f59e0b`). Testing with `rgba(18, 18, 21, 0.85)` provides crisp edge delineation.
