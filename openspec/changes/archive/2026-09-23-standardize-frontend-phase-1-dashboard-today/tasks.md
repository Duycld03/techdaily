# Tasks

## 1. Home Dashboard Bento Reflow

- [x] 1.1 Update `pages/index.vue` and `HomeBentoDashboard.vue` to ensure single-column stacking (`grid-cols-1 md:grid-cols-2 lg:grid-cols-3`) on mobile viewports
- [x] 1.2 Audit `DomainConstellationCard.vue` and `AISynthesisCard.vue` for responsive padding and zero-shift borders

## 2. Metric Cards & ViewBox Scaling

- [x] 2.1 Refactor `ConcentricMetricCard.vue` SVG rings to scale fluidly with responsive `viewBox` without numeric label clipping
- [x] 2.2 Verify telemetry number formatting with `tabular-nums` across all card summary figures

## 3. Today Studio & Term Explainer Modal

- [x] 3.1 Audit `pages/today.vue` and `InterviewChallengePane.vue` for `text-sm` minimum mobile typography
- [x] 3.2 Constrain `TermExplainerModal.vue` to `max-h-[85dvh]` with smooth scrolling and bottom safe area clearance
- [x] 3.3 Verify unit test suite passes for dashboard and today pages via `npm test`
