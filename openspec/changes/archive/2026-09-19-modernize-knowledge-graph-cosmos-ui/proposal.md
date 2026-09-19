# Proposal: Modernize Knowledge Graph Cosmos & Today Studio UI

## Why

While the dedicated Architecture Knowledge Cosmos page (`frontend/pages/graph.vue`) features a Cyber Neon Telemetry HUD ribbon and dual 2D/3D visual rendering, its foundation suffers from noticeable color token drift:
1. **Canvas Background Disconnect**: The 3D WebGL galaxy renderer is hardcoded with a bluish slate background (`#020617`) and the 2D Cytoscape canvas is wrapped in `dark:bg-slate-950`, causing an abrupt boundary seam with the neutral obsidian `#09090b` shell.
2. **Legacy Control Surfaces**: The floating control bar (`GraphControlBar.vue`), slide-over detail drawer (`GraphDetailDrawer.vue`), and minimap (`GraphMinimap.vue`) are built with opaque slate boxes (`dark:bg-slate-800`, `dark:bg-slate-900`, `dark:border-slate-700/800`) instead of the refined `.glass-panel` and translucent hairline borders (`border-white/[0.08]`).
3. **Daily Focus Auxiliary Clean-up (`/today`)**: In the reading studio, `AISynthesisCard.vue` and `TermExplainerModal.vue` still use `dark:bg-slate-900`, and `InterviewChallengePane.vue` violates the mobile typography invariant by rendering option labels and text in micro `text-xs` (12px) instead of the required `text-sm` (14px).

Modernizing these surfaces establishes 100% design system harmony across the entire technical exploration and study workflow.

## What Changes

- **Obsidian Graph Canvas Surfaces**:
  - `GraphCanvas3D.vue`: Update Three.js canvas background color from `#020617` (slate-950) to `#09090b` (`bg-canvas` - neutral obsidian). Update 3D sprite label backgrounds from `rgba(15, 23, 42, 0.85)` (slate-900) to `rgba(18, 18, 21, 0.85)` (`canvas-elevated`).
  - `GraphCanvas.vue`: Update 2D canvas wrapper from `dark:bg-slate-950` to `dark:bg-canvas`. Update base node edge/border colors in dark mode to translucent hairline `#27272a`.
- **Glassmorphic Graph Studio Controls**:
  - `GraphControlBar.vue`: Re-skin action buttons, 2D/3D mode toggles, filter pills, and mobile toggle to use `.glass-panel`, `dark:bg-canvas-subtle`, `dark:bg-canvas-elevated`, and `dark:border-white/[0.08]`.
  - `GraphDetailDrawer.vue`: Upgrade slide-over desktop drawer and mobile bottom sheet to `dark:bg-canvas-subtle` and `dark:border-white/[0.08]`, with internal metrics and takeaways boxes styled with `dark:bg-canvas-elevated`.
  - `GraphMinimap.vue`: Update container and canvas backgrounds to `.glass-panel` with `dark:bg-canvas-subtle` and `dark:border-white/[0.08]`.
  - `GraphLegend.vue`: Update hover states and borders to hairline standards.
- **Today Studio Auxiliary Clean-up (`/today`)**:
  - `AISynthesisCard.vue`: Update container from `dark:bg-slate-900 dark:border-slate-800` to `.glass-panel` with `dark:bg-canvas-subtle` and `dark:border-white/[0.08]`.
  - `TermExplainerModal.vue`: Upgrade modal container from `dark:bg-slate-900` to `dark:bg-canvas-elevated` with `dark:border-white/[0.08]`.
  - `InterviewChallengePane.vue`: Upgrade option badges and option text from `text-xs sm:text-sm` to `text-sm sm:text-base` for seamless mobile readability in compliance with the Responsive Typography Standard.
- **Automated Verification**:
  - Ensure 100% pass rate across frontend unit tests (`npm --prefix frontend test`).

## Capabilities

### Modified Capabilities

- `knowledge-graph`: Update requirements to enforce obsidian `#09090b` canvas backgrounds, glassmorphic floating control surfaces, and neutral canvas tokens across both 2D and 3D rendering modes.
- `today`: Update requirement to enforce responsive typography standard on interview challenge options and modern glassmorphic styling on AI synthesis and term explainer overlays.

## Impact

- **API & Domain Contracts**: Zero breaking changes. `GET /api/v1/graph` and `GET /api/v1/daily/today` remain untouched.
- **State & Stores**: `useKnowledgeGraphStore` and `useDailyFocusStore` remain completely untouched.
- **UX Consistency**: Delivers a seamless, immersive dark mode experience across both technical deep dives and daily reading.
