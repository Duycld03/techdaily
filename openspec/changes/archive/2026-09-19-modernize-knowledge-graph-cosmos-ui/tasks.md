# Tasks

## 1. Frontend (Knowledge Graph Canvas & Engine Surfaces)

- [x] 1.1 Update `frontend/components/graph/GraphCanvas3D.vue` scene background to `#09090b` (`dark:bg-canvas`), update 3D sprite billboard label background to `rgba(18, 18, 21, 0.85)`, and refactor floating camera controls to `.glass-panel`.
- [x] 1.2 Update `frontend/components/graph/GraphCanvas.vue` wrapper container from `dark:bg-slate-950` to `dark:bg-canvas`, and update dark mode node borders and edge styles to hairline `#27272a`.
- [x] 1.3 Refactor `frontend/pages/graph.vue` empty and error state cards from `dark:bg-slate-900` to `dark:bg-canvas-subtle` and `.glass-panel`.

## 2. Frontend (Graph Control Bar, Minimap & Slide-over Drawer)

- [x] 2.1 Refactor `frontend/components/graph/GraphControlBar.vue` action buttons, filter pills, mode switches, and mobile toggle from legacy slate to `.glass-panel`, `dark:bg-canvas-subtle`, `dark:bg-canvas-elevated`, and `dark:border-white/[0.08]`.
- [x] 2.2 Refactor `frontend/components/graph/GraphDetailDrawer.vue` slide-over / bottom sheet from `dark:bg-slate-900` to `dark:bg-canvas-subtle` and internal takeaways/metrics blocks to `dark:bg-canvas-elevated`.
- [x] 2.3 Refactor `frontend/components/graph/GraphMinimap.vue` container and canvas from `dark:bg-slate-900/85` to `.glass-panel` and `dark:bg-canvas-subtle`.
- [x] 2.4 Refactor `frontend/components/graph/GraphLegend.vue` item hover states and borders to hairline standards.

## 3. Frontend (Today Studio Auxiliary Clean-up & Typography)

- [x] 3.1 Refactor `frontend/components/today/AISynthesisCard.vue` container to `.glass-panel` with `dark:bg-canvas-subtle` and `dark:border-white/[0.08]`.
- [x] 3.2 Refactor `frontend/components/today/TermExplainerModal.vue` dialog container to `dark:bg-canvas-elevated` with `dark:border-white/[0.08]`.
- [x] 3.3 Upgrade scenario challenge option badges and description text in `frontend/components/today/InterviewChallengePane.vue` to `text-sm sm:text-base` in compliance with the Responsive Typography Standard.

## 4. Automated Verification & Testing

- [x] 4.1 Run frontend test suite (`npm --prefix frontend test`) to ensure all test files pass without regression.
- [x] 4.2 Validate OpenSpec change specifications with `openspec validate --changes` and `openspec validate --specs`.
