# Proposal: Modernize Roadmap & Interactive Mindmap Canvas UI

## Why

Following the deployment of the Dev-Learning Studio design system across the Home Command Center (`/`), Daily Studio (`/today`), and Engineer Portfolio Passport (`/profile`), the Roadmap page (`frontend/pages/roadmap.vue`) and its sub-components (`RoadmapViewSwitcher.vue`, `RoadmapMindmapCanvas.vue`) suffer from an un-migrated visual disconnect:
1. **Legacy Slate Tokens**: Over 20 hardcoded occurrences of `dark:bg-slate-800`, `dark:bg-slate-900`, `dark:bg-slate-950`, and opaque gray borders (`dark:border-slate-700`, `dark:border-slate-800`) clash with the neutral dark obsidian palette (`#09090b` canvas, `#121215` subtle, `#18181b` elevated).
2. **Disconnected Component Aesthetics**: The view mode switcher (`RoadmapViewSwitcher.vue`), floating search bar, toolbar, and mindmap tree node cards still render in heavy opaque slate boxes instead of refined translucent `.glass-panel` and hairline borders (`border-white/[0.08]`).

Modernizing `/roadmap` completes the core learning journey, providing a seamless visual continuum from daily operational drills to long-term curriculum progression and mindmap exploration.

## What Changes

- **Roadmap Page Obsidian Shell**: Update `frontend/pages/roadmap.vue` to replace all legacy slate tokens with canonical Dev-Learning Studio tokens:
  - Header banner, track switcher popover, chapter milestone cards, daily slice rows, and module containers upgraded to `dark:bg-canvas-subtle` and `dark:bg-canvas-elevated`.
  - Progress bar tracks converted from `dark:bg-slate-800` to `dark:bg-canvas-subtle` with smooth gradient fills.
  - Border treatments unified to translucent hairline borders (`border-slate-200/80 dark:border-white/[0.08]`).
- **Glassmorphic View Switcher (`RoadmapViewSwitcher.vue`)**:
  - Re-architect container as a sleek `.glass-panel` pill using `dark:bg-canvas-subtle/80` and `dark:border-white/[0.08]`.
  - Update active view tab button to `dark:bg-canvas-elevated` with crisp contrast text and subtle shadow.
- **Modernized Mindmap Canvas (`RoadmapMindmapCanvas.vue`)**:
  - Convert canvas container background from `dark:bg-slate-950/70` to obsidian `dark:bg-canvas` with subtle architectural grid overlay.
  - Re-skin floating search input and viewport control toolbar into `.glass-panel` components with hairline borders.
  - Upgrade Chapter branch and Slice leaf cards to use `dark:bg-canvas-subtle` with glowing accent borders for active nodes.
- **Automated Verification**:
  - Preserve all existing unit tests in `frontend/tests/pages/roadmap.spec.ts`, `frontend/tests/components/roadmap/RoadmapMindmapCanvas.spec.ts`, and `frontend/tests/components/roadmap/RoadmapViewSwitcher.spec.ts`.

## Capabilities

### Modified Capabilities

- `roadmap`: Update requirements to enforce the Dev-Learning Studio design tokens, glassmorphic control containers, and neutral obsidian dark aesthetics across both the linear timeline and hierarchical tree mindmap modes.

## Impact

- **API & Domain Contracts**: Zero breaking changes. `GET /api/v1/curriculum/roadmap` remains untouched.
- **State & Stores**: `useRoadmapStore`, `useDailyFocusStore`, and `useRoadmapViewMode` remain completely untouched.
- **Cross-Platform Reliability**: Eliminates visual seams when toggling light/dark mode and jumping between `/today`, `/roadmap`, and `/library`.
