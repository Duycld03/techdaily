## 1. Component Refactoring (`GraphLegend.vue`)

- [x] 1.1 Refactor Entity Hierarchy section in `frontend/components/graph/GraphLegend.vue`: replace `grid grid-cols-2` with `flex flex-col gap-1`, remove `truncate` class, and display complete labels.
- [x] 1.2 Refactor Flashcard Retention section in `frontend/components/graph/GraphLegend.vue`: replace horizontal layout with `flex flex-col gap-1`, matching entity item styling.
- [x] 1.3 Standardize card container width to `w-56` ($224\text{px}$) and update `getInitialCollapsedState()` to default to collapsed on screens $< 1024\text{px}$ (tablets and mobile).

## 2. Unit Testing & Component Verification

- [x] 2.1 Update `frontend/tests/components/graph/GraphLegend.spec.ts` to assert vertical list structure, unclipped text rendering, and `< 1024px` collapse breakpoint.
- [x] 2.2 Execute `npm --prefix frontend test` and verify all unit tests pass with zero regressions.

## 3. E2E Smoke & Visual Clearance Validation

- [x] 3.1 Execute responsive E2E smoke verification via `node frontend/e2e/test-graph-roadmap-responsive.mjs --screenshot`.
- [x] 3.2 Inspect generated screenshots across Desktop, Tablet, and Mobile to visually confirm zero label truncation and $\ge 80\text{px}$ clearance between Legend and Minimap on Tablet.
- [x] 3.3 Validate OpenSpec change integrity via `openspec validate graph-visual-legend-vertical-stack-and-clearance --type change`.
