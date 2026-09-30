# Tasks

## 1. Frontend - Clean-Slate Bento Dashboard Architecture

- [x] 1.1 Rebuild `frontend/components/dashboard/HomeBentoDashboard.vue` from scratch implementing `LayoutArchetypesShowcase.vue` Demo 4 structure (Orientation banner header, 2-tier action stage, and telemetry dock).
- [x] 1.2 Implement the Executive Orientation Banner in `#header` slot with Executive Cockpit badge, target role, curriculum title, subtitle, and primary 1-click CTA launchpad.
- [x] 1.3 Implement Tier 1 (Hero Active Reading Card) in `#action-stage` with typography-first book metadata, summary, reading duration, progress bar, and continue reading action.
- [x] 1.4 Implement Tier 2 (Split Practice Subgrid) in `#action-stage` featuring Daily Micro-Drill (left) and Senior Dilemma (right) with compact status pills and interactive triggers.

## 2. Frontend - Showcase-Aligned Telemetry Dock & Component Decommissioning

- [x] 2.1 Implement the Practice Streak & Consistency Card in `#telemetry-dock` with flame badge, large tabular streak days count, and 7-day consistency bar.
- [x] 2.2 Implement the Knowledge Constellation Card in `#telemetry-dock` with concept node counts, relations count, and quick link to `/graph`.
- [x] 2.3 Decommission unused legacy components (`ConcentricMetricCard.vue` and `DomainConstellationCard.vue`) and remove their references from `HomeBentoDashboard.vue`.

## 3. Frontend - Verification & Testing (Dual-Gate)

- [x] 3.1 Update and run frontend unit tests (`npm test` in `frontend/tests/components/dashboard/HomeBentoDashboard.spec.ts`) verifying computed properties, store reactivity, routing, and i18n resolution without CSS class assertions.
- [x] 3.2 Execute dual-gate automated visual verification via headless Chromium on Desktop (1440x900) and Mobile (390x844), capturing and inspecting screenshots.
