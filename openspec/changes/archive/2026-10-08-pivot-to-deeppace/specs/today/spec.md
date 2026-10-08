# Spec Delta

## MODIFIED Requirements

### Requirement: Scenario Drill Multiple-Choice Interface & Evaluation Feedback
The Decision Challenge multiple-choice interface (`InterviewChallengePane.vue`) and reading pane (`DocReaderPane.vue`) on `/today` SHALL style option cards, score badges, and evaluation feedback according to DeepPace Studio design tokens, supporting both technical architecture trade-offs and behavioral habit decisions.

#### Scenario: Reviewed optimal choice rendering in dark mode
- **WHEN** user submits an answer to the Decision Drill on `/today` in dark mode
- **THEN** the optimal choice card displays with translucent emerald border (`dark:border-emerald-500/40`) and subtle tint (`dark:bg-emerald-500/10`)
- **AND** the letter badge displays with solid emerald fill (`bg-emerald-600 text-white`)
- **AND** the optimal choice pill badge renders with high-contrast text and crisp checkmark icon
- **AND** other unselected options display with neutral studio slate/white tokens (`dark:bg-slate-950/20` replaced with `dark:bg-canvas-subtle/30` and `dark:border-white/[0.04]`).

#### Scenario: Authoritative source excerpt rendering in dark mode
- **WHEN** user views a reading slice on `/today` that contains an authoritative source excerpt
- **THEN** the excerpt box renders with `.glass-panel` container styling with `dark:bg-canvas-subtle/80` and `dark:border-white/[0.08]`
- **AND** the header label renders with Deep Iris Violet telemetry color (`text-brand-600 dark:text-brand-400`) rather than emerald green.

### Requirement: Home Dashboard & Daily Focus Studio Responsive Bento Layout
The Home Dashboard (`pages/index.vue`) and Today's Focus Studio (`pages/today.vue`) SHALL adapt seamlessly across screen widths from $320\text{px}$ to $1440\text{px}$, ensuring Bento cards stack cleanly, SVG concentric progress metrics scale without clipping, and auxiliary modals preserve full touch accessibility.
The Home Dashboard orientation header banner (`HomeBentoDashboard.vue`) SHALL maintain single-line text integrity with status badge "Daily Practice Workspace" replacing legacy dev-only titles.

#### Scenario: Mobile Bento Grid Reflow
- **WHEN** user views `pages/index.vue` on a mobile device ($< 768\text{px}$)
- **THEN** multi-column Bento cards (`HomeBentoDashboard.vue`, `DomainConstellationCard.vue`) SHALL collapse into a single-column stacked layout with minimum gap of `0.875rem` (`gap-3.5`)
- **AND** zero horizontal overflow or clipping SHALL occur.

#### Scenario: Orientation Header Badge Non-Wrapping on Mobile Viewports
- **GIVEN** a user accesses the Home Dashboard on a mobile viewport ($< 640\text{px}$, such as $360\text{px}$ or $390\text{px}$)
- **WHEN** the orientation header banner is rendered
- **THEN** the "Daily Practice Workspace" status badge displays strictly on a single horizontal line without word splitting or vertical line breaks
- **AND** the badge retains uniform rounded pill geometry and symmetric padding (`px-2.5 py-0.5 rounded-full`)
- **AND** the adjacent target role and slice reading metadata wrap to the next line or fit without forcing the badge to collapse.

#### Scenario: Concentric Metric Card Responsive Scaling
- **WHEN** user views `ConcentricMetricCard.vue` on viewports between $320\text{px}$ and $480\text{px}$
- **THEN** the SVG rings SHALL scale proportionally within their `viewBox` container without overlapping numeric score labels or legend text.

#### Scenario: Term Explainer Modal Scroll Containment
- **WHEN** user triggers the term explanation popup (`TermExplainerModal.vue`) with lengthy AI-curated markdown on a mobile screen
- **THEN** the modal body SHALL provide smooth vertical scrolling with max height constrained to `85dvh` and safe area bottom clearance.
