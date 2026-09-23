# Proposal: Standardize Frontend Phase 1 — Home Dashboard & Daily Focus Studio

## Why

The Home Dashboard (`pages/index.vue`) and Today's Focus Studio (`pages/today.vue`) are the primary daily entry points for TechDaily users. Under the new `antfu/skills` standards, these core learning surfaces require standardization to:
1. Guarantee seamless mobile viewport stacking ($\le 375\text{px}$ and $320\text{px}$) with zero horizontal layout shift or component collision.
2. Ensure SVG telemetry visuals (Concentric Rings in `ConcentricMetricCard.vue`, Radar charts in `HomeBentoDashboard.vue`, and Constellation pill metrics) scale smoothly across device pixel ratios.
3. Align AI explanation popovers (`TermExplainerModal.vue`) and challenge panes (`InterviewChallengePane.vue`, `AISynthesisCard.vue`) with strict VueUse lifecycle hygiene and markdown rendering standards.
4. Reinforce Nuxt SSR data fetching invariants to eliminate double-fetching and hydration mismatches during daily curriculum initialization.

## What Changes

- **Home Bento Dashboard (`pages/index.vue`, `HomeBentoDashboard.vue`, `DomainConstellationCard.vue`)**:
  - Audit asymmetric 3-column desktop bento grid to ensure clean single-column stacking on mobile viewports with tight vertical rhythm (`gap-3.5`).
  - Standardize 7-day consistency tracker (`grid-cols-7`) with `tabular-nums` and touch-friendly day inspection chips.
  - Verify `DomainConstellationCard` 2-column telemetry footer fits cleanly on 320px screens.
- **Daily Focus Studio (`pages/today.vue`, `DocReaderPane.vue`, `ConcentricMetricCard.vue`)**:
  - Modernize dual-mode layout (Reading Focus vs Scenario Challenge) with zero-shift view mode toggles.
  - Scale concentric progress SVG dynamically using viewBox and responsive radius formulas.
  - Enforce typography standards on reading pane: body text minimum `text-sm` (14px) on mobile, `text-base` on desktop.
- **AI Challenges & Term Explainer (`InterviewChallengePane.vue`, `AISynthesisCard.vue`, `TermExplainerModal.vue`)**:
  - Standardize `TermExplainerModal` geometry: max width `calc(100vw - 2rem)`, max height `80dvh` with smooth scroll, touch-friendly dismiss.
  - Ensure markdown prose styling (`prose prose-invert dark:prose-invert`) parses code snippets and bullet points without text overflows.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `today`: Standardize Daily Focus Studio responsive layout, concentric SVG scaling, and mobile explainer modal specifications.
- `core-platform`: Audit home bento dashboard mobile stacking invariants.

## Impact

- **Affected Files**: `frontend/pages/index.vue`, `frontend/pages/today.vue`, `frontend/components/dashboard/*.vue`, `frontend/components/today/*.vue`.
- **Testing**: Unit tests in `frontend/tests/pages/today.spec.ts`, `frontend/tests/components/dashboard/`, and mobile viewport verification.
