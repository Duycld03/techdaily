# Spec Delta

## MODIFIED Requirements

### Requirement: Home Dashboard & Daily Focus Studio Responsive Bento Layout
The Home Dashboard (`pages/index.vue`) and Today's Focus Studio (`pages/today.vue`) SHALL adapt seamlessly across screen widths from $320\text{px}$ to $1440\text{px}$, ensuring Bento cards stack cleanly, SVG concentric progress metrics scale without clipping, and auxiliary modals preserve full touch accessibility.

In addition, the Home Dashboard orientation header banner (`HomeBentoDashboard.vue`) SHALL maintain strict single-line text integrity for status badges:
1. **Single-Line Badge Integrity**: The "Executive Cockpit" orientation badge SHALL render on a single horizontal line with `whitespace-nowrap shrink-0` across all viewports down to $320\text{px}$, strictly prohibiting two-line vertical text wrapping or lopsided pill distortion.
2. **Responsive Metadata Row Alignment**: The orientation banner metadata container SHALL permit flexible wrapping (`flex-wrap`) and responsive gaps (`gap-1.5 sm:gap-2`), ensuring the target role and slice progress indicators never compress the status badge into an asymmetric vertical layout on narrow mobile viewports ($< 640\text{px}$).

#### Scenario: Mobile Bento Grid Reflow
- **WHEN** user views `pages/index.vue` on a mobile device ($< 768\text{px}$)
- **THEN** multi-column Bento cards (`HomeBentoDashboard.vue`, `DomainConstellationCard.vue`) SHALL collapse into a single-column stacked layout with minimum gap of `0.875rem` (`gap-3.5`)
- **AND** zero horizontal overflow or clipping SHALL occur.

#### Scenario: Orientation Header Badge Non-Wrapping on Mobile Viewports
- **GIVEN** a user accesses the Home Dashboard on a mobile viewport ($< 640\text{px}$, such as $360\text{px}$ or $390\text{px}$)
- **WHEN** the orientation header banner is rendered
- **THEN** the "Executive Cockpit" status badge displays strictly on a single horizontal line without word splitting or vertical line breaks
- **AND** the badge retains uniform rounded pill geometry and symmetric padding (`px-2.5 py-0.5 rounded-full`)
- **AND** the adjacent target role and slice reading metadata wrap to the next line or fit without forcing the badge to collapse.

#### Scenario: Concentric Metric Card Responsive Scaling
- **WHEN** user views `ConcentricMetricCard.vue` on viewports between $320\text{px}$ and $480\text{px}$
- **THEN** the SVG rings SHALL scale proportionally within their `viewBox` container without overlapping numeric score labels or legend text.

#### Scenario: Term Explainer Modal Scroll Containment
- **WHEN** user triggers the term explanation popup (`TermExplainerModal.vue`) with lengthy AI-curated markdown on a mobile screen
- **THEN** the modal body SHALL provide smooth vertical scrolling with max height constrained to `85dvh` and safe area bottom clearance.
