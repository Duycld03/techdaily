# Tasks

## 1. Clean Constellation Card Visuals

- [x] 1.1 In `frontend/components/dashboard/DomainConstellationCard.vue`, remove the conditional outer pulsing circle `<circle v-if="n.pulse" class="animate-pulse" ... />`.
- [x] 1.2 In `frontend/components/dashboard/DomainConstellationCard.vue`, remove `pulse?: boolean` from the `ConstellationNode` interface and `pulse: true` from the `dist` node definition.

## 2. Automated Testing & Verification

- [x] 2.1 In `frontend/tests/components/dashboard/DomainConstellationCard.spec.ts`, update tests to verify that no animated pulse circles exist on the constellation SVG canvas.
- [x] 2.2 Run full frontend test suite (`npm --prefix frontend test`) to confirm zero regressions.
- [x] 2.3 Validate OpenSpec changes and specifications (`openspec validate --changes` and `openspec validate --specs`).
