# Proposal: Dashboard Top-Aligned Compact Layout

## Why
On taller desktop viewports (e.g. 1080p displays or full-screen browser windows), the current Home Dashboard uses `justify-between` on the outer container and column flexboxes. This causes the cards to disperse to the extreme top and bottom edges, creating large unnatural empty gaps between the cards and detaching them visually (as seen in Image #1). When viewed on more vertically constrained viewports (Image #2), the layout naturally looks dense, cohesive, and intentional.

Refactoring the dashboard layout to be **top-aligned (`justify-start`)** with consistent, snug spacing (`gap-3.5 sm:gap-4`) ensures that all cards remain tightly grouped as a unified command center directly beneath the welcome banner, regardless of viewport height.

## What Changes
- **Top-Aligned Container Layout**: Replace `justify-between` with `justify-start gap-3.5 sm:gap-4` in `TodayBentoDashboard.vue` across the main page container and both column flexboxes.
- **Natural Card Sizing & Balance**: Remove unbounded `flex-1` stretching that forces cards to inflate into empty voids; allow Card A (Active Reading Slice) and Card B (Scenario Challenge) to maintain their optimal proportions while hugging the top.
- **Right Column Cluster**: Cluster Card C (Concentric Metrics), Card D (7-Day Consistency), and Card E (Knowledge Radar) snugly with consistent gaps, perfectly aligned with the left column.
- **Desktop Zero-Scroll Invariant**: Preserve single-screen visibility above the fold on desktop viewports while avoiding awkward vertical separation on tall displays.

## Capabilities

### Modified Capabilities
- `core-platform`: Update the `Home Command Center Dashboard & Zero-Scroll Desktop Layout` requirement to specify top-aligned clustering (`justify-start`) with consistent gaps rather than `justify-between` vertical dispersion on tall viewports.

## Impact
- `frontend/components/today/TodayBentoDashboard.vue`: Update flex alignment and gap utilities.
- Visual coherence: Fixes the detached floating cards on tall screens while preserving the zero-scroll behavior.
- Zero breaking changes to API, database, or state management.
