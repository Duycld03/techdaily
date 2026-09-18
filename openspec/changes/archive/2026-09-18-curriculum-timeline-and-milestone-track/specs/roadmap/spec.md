# Spec Delta: roadmap

## ADDED Requirements

### Requirement: Continuous Milestone Timeline Spine & Active Telemetry
The `/roadmap` timeline view SHALL render an illuminated continuous vertical timeline spine connecting sequential milestones (chapters, days, and slices). The spine SHALL visually connect module and chapter milestone cards to daily slice nodes with subtle progress gradients and connector indicators.

Today's active learning milestone (active day or active chunk slice) SHALL be visually accented along the spine with an active telemetry treatment including an amber flame or electric violet pulsing ring, glowing status pill, and a direct 1-click launch button to start or resume today's session.

#### Scenario: Visual connector spine rendering
- **WHEN** user views the `/roadmap` page in `timeline` view mode
- **THEN** chapter headers and daily slice nodes are vertically interconnected by a continuous visual connector spine indicating sequential progression.

#### Scenario: Active milestone node telemetry
- **WHEN** an active day or active document slice exists for today
- **THEN** that milestone node displays an illuminated pulsing ring, prominent active badge, and quick action button to start or resume learning without manual searching.

#### Scenario: Modernized studio surface styling
- **WHEN** milestone cards and chapter accordions are rendered in dark mode
- **THEN** they utilize elevated canvas tokens (`bg-canvas-subtle`, `bg-canvas-elevated`) and hairline borders (`border-white/[0.08]`) consistent with the core studio design system.
