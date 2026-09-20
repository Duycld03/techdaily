# Spec Delta

## MODIFIED Requirements

### Requirement: Home Dashboard & Daily Focus Studio Responsive Bento Layout
The Home Dashboard (`pages/index.vue`) and Today's Focus Studio (`pages/today.vue`) SHALL adapt seamlessly across screen widths from $320\text{px}$ to $1440\text{px}$, ensuring Bento cards stack cleanly, SVG concentric progress metrics scale without clipping, and auxiliary modals preserve full touch accessibility.

#### Scenario: Mobile Bento Grid Reflow
- **WHEN** user views `pages/index.vue` on a mobile device ($< 768\text{px}$)
- **THEN** multi-column Bento cards (`HomeBentoDashboard.vue`, `DomainConstellationCard.vue`) SHALL collapse into a single-column stacked layout with minimum gap of `0.875rem` (`gap-3.5`)
- **AND** zero horizontal overflow or clipping SHALL occur.

#### Scenario: Concentric Metric Card Responsive Scaling
- **WHEN** user views `ConcentricMetricCard.vue` on viewports between $320\text{px}$ and $480\text{px}$
- **THEN** the SVG rings SHALL scale proportionally within their `viewBox` container without overlapping numeric score labels or legend text.

#### Scenario: Term Explainer Modal Scroll Containment
- **WHEN** user triggers the term explanation popup (`TermExplainerModal.vue`) with lengthy AI-curated markdown on a mobile screen
- **THEN** the modal body SHALL provide smooth vertical scrolling with max height constrained to `85dvh` and safe area bottom clearance.
