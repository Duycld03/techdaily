# Tasks

## 1. Frontend (Roadmap View Switcher & Mindmap Canvas)

- [x] 1.1 Refactor `frontend/components/roadmap/RoadmapViewSwitcher.vue` to use `.glass-panel` container with `dark:bg-canvas-subtle/80`, `dark:border-white/[0.08]`, and active tab styling with `dark:bg-canvas-elevated`.
- [x] 1.2 Refactor `frontend/components/roadmap/RoadmapMindmapCanvas.vue` container background to `dark:bg-canvas`, update floating search input and control toolbar to `.glass-panel`, and refactor chapter branch and slice leaf cards to `dark:bg-canvas-subtle`.

## 2. Frontend (Roadmap Page Timeline & Milestone Tracks)

- [x] 2.1 Refactor header track switcher popover in `frontend/pages/roadmap.vue` to use `dark:bg-canvas-elevated`, `dark:border-white/[0.08]`, and modern translucent hover states (`dark:hover:bg-white/[0.06]`).
- [x] 2.2 Refactor chapter milestone accordions, daily slice items, slice badges, and progress bar tracks in `frontend/pages/roadmap.vue` to eliminate all legacy `dark:bg-slate-800`, `dark:bg-slate-900`, and `dark:border-slate-700/800`.
- [x] 2.3 Refactor 30-day curriculum module cards and day milestone items in `frontend/pages/roadmap.vue` to use `dark:bg-canvas-subtle` and `dark:bg-canvas-elevated`.

## 3. Automated Verification & Testing

- [x] 3.1 Run frontend test suite (`npm --prefix frontend test`) to ensure all test files pass without regression.
- [x] 3.2 Validate OpenSpec change specifications with `openspec validate --changes` and `openspec validate --specs`.
