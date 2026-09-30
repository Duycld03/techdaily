# Proposal

## Why

The home dashboard (`HomeBentoDashboard.vue`) on `/` currently exhibits visual and architectural fragmentation resulting from incremental refactoring, leaving behind an awkward hybrid between the new typography-first direction and obsolete legacy sub-components (`ConcentricMetricCard.vue` and `DomainConstellationCard.vue`). Rebuilding the dashboard from scratch according to the canonical Design System showcase (`LayoutArchetypesShowcase.vue` Demo 4 and `BentoMetricCard.vue`) delivers an authentic Executive Bento Cockpit: an asymmetric 2:1 ratio, crisp typography with tabular numbers, unified card padding and hairline borders, and balanced column heights on 1080p desktop viewports.

## What Changes

- **Clean-Slate Dashboard**: Rebuild `frontend/components/dashboard/HomeBentoDashboard.vue` from the ground up, eliminating legacy wrapper baggage and adhering strictly to `/showcase` Demo 4.
- **Executive Orientation Header Banner**: Implement a top banner slot with an Executive Cockpit badge, user target role, active practice title, subtitle, and high-contrast 1-click CTA launchpad (`[ Bắt Đầu Học Ngay → ]` / `Start Daily Focus`).
- **Two-Tier Action Stage (2 Columns)**:
  - **Tier 1 (Hero Reading Card)**: Full-width card with clean typography, source book name, reading slice progress, reading duration pill, summary excerpt, and primary reading action.
  - **Tier 2 (Split Subgrid)**: 2-column balanced grid featuring the Daily Micro-Drill on the left and the Senior Scenario Dilemma on the right with compact status badges and direct CTA triggers.
- **Showcase-Standard Telemetry Dock (1 Column)**:
  - **Streak & Consistency Card**: Prominent streak day count with tabular numbers, flame badge, and 7-day consistency progress bar.
  - **Knowledge Constellation Card**: Active concept nodes and associative relations count with direct link to the 3D Cosmos (`/graph`).
- **Retirement of Legacy Components**: Deprecate and remove obsolete `ConcentricMetricCard.vue` and `DomainConstellationCard.vue` from the home dashboard.
- **Data Contracts & Testing**: Maintain direct Pinia store bindings (`useDailyFocusStore`, `useReviewStore`, `useKnowledgeGraphStore`, `useAuthStore`) with comprehensive Vitest tests verifying routing, state reactivity, and i18n without CSS class assertions.

## Capabilities

### Modified Capabilities
- `today`: Spec delta defining the canonical Bento Dashboard visual hierarchy, orientation banner, 2-tier action stage, and showcase-aligned telemetry dock.

## Impact

- **Frontend**: `frontend/components/dashboard/HomeBentoDashboard.vue`, `frontend/components/dashboard/ConcentricMetricCard.vue`, `frontend/components/dashboard/DomainConstellationCard.vue`.
- **i18n**: `frontend/i18n/locales/en.json`, `frontend/i18n/locales/vi.json`.
- **Testing**: `frontend/tests/components/dashboard/HomeBentoDashboard.spec.ts` and automated visual inspection across Desktop (1440x900) and Mobile (390x844).
