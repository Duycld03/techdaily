# Proposal

## Why

The current `/roadmap` timeline view displays curriculum modules and book chapters as disconnected grid cards without a visual narrative of sequential progression. Modernizing the timeline with an illuminated milestone spine, glowing active nodes, and studio canvas design tokens (benchmarked against EdTech Roadmap Reference Image #4) creates an engaging, high-clarity engineering study track.

## What Changes

- **Continuous Visual Milestone Spine**: Introduce a continuous vertical spine connecting chapter milestones and daily nodes with subtle progress gradients and connector nodes.
- **Illuminated Active Node Telemetry**: Accentuate today's active slice and curriculum day with an electric violet / amber glowing flame badge (`ring-2 ring-brand-500/30`), status badges, and quick-launch action button.
- **Studio Token Integration**: Upgrade chapter accordion headers, search toolbar, and day/slice cards from generic slate styling to `.glass-card`, `bg-canvas-subtle`, `bg-canvas-elevated`, and hairline borders (`border-white/[0.08]`).
- **Responsive Milestone Cards**: Ensure milestone cards and slice pills adapt gracefully across mobile and desktop with `whitespace-nowrap shrink-0` on badges to preserve bilingual layout integrity.

## Capabilities

### Modified Capabilities

- `roadmap`: Modernize the linear timeline view requirements to mandate a continuous milestone connector spine, illuminated active node telemetry, and studio design token consistency.
