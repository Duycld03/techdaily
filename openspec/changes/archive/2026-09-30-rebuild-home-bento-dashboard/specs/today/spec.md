# Spec Delta

## ADDED Requirements

### Requirement: Canonical Showcase Bento Dashboard Archetype Structure
The Home Dashboard (`HomeBentoDashboard.vue`) SHALL implement the canonical Bento Dashboard layout archetype from the Design System showcase (`LayoutArchetypesShowcase.vue` Demo 4) with an asymmetric 2:1 ratio:
1. **Orientation Banner Slot (`#header`)**:
   - SHALL display an Executive Cockpit badge (`text-xs font-semibold rounded-full bg-brand-500/15 text-brand-600 dark:text-brand-300`).
   - SHALL display the user's target engineering role or track.
   - SHALL render a prominent 1-click launchpad CTA button (`[ Bắt Đầu Học Ngay → ]` / `Start Daily Focus`) that directly navigates to the active reading slice or daily scenario drill.
2. **Action Stage (`#action-stage`, 2 Columns on Desktop)**:
   - **Tier 1 (Hero Reading Card)**: Full-width card with clean typography, source book name, reading slice progress, reading duration pill (`⏱ X min read`), summary excerpt (`line-clamp-2`), progress bar, and continue reading action.
   - **Tier 2 (Split Subgrid)**: 2-column responsive subgrid featuring the Daily Micro-Drill on the left and the Senior Scenario Dilemma on the right with compact status badges (`+10 Điểm`, `Hoàn thành: X/10`, `Cần ôn tập: 0/10`) and direct action buttons.
3. **Telemetry Dock (`#telemetry-dock`, 1 Column on Desktop)**:
   - **Streak & Consistency Card**: Prominent streak day count in large tabular numerals, flame indicator, and 7-day consistency progress bar.
   - **Knowledge Constellation Card**: Active concept nodes and associative relations count with a direct action link to the 3D Cosmos (`/graph`).

#### Scenario: User opens home page on desktop viewport
- **WHEN** an authenticated user opens `/` on a desktop viewport ($\ge 1024\text{px}$)
- **THEN** the orientation banner spans the full width of the container
- **AND** the Action Stage occupies 2 columns on the left with Tier 1 hero card and Tier 2 split subgrid
- **AND** the Telemetry Dock occupies 1 column on the right with Streak and Knowledge Constellation cards
- **AND** both columns maintain balanced vertical heights without dead space.

#### Scenario: User clicks primary orientation banner CTA
- **WHEN** a user clicks the primary CTA button in the orientation banner
- **THEN** if a reading slice exists and the daily drill is pending, the system navigates to `/read/<bookId>?slice=<order>`
- **AND** if the daily drill is completed, the system navigates to `/today` to review or practice.

### Requirement: Elimination of Legacy Multi-Ring SVG and Deep Nested Widgets
The Home Bento Dashboard SHALL NOT render legacy multi-ring circular SVG components (`ConcentricMetricCard.vue`) or legacy nested cards (`DomainConstellationCard.vue`), replacing them with flat, hairline-bordered glass cards (`rounded-2xl border border-slate-200/80 dark:border-white/[0.06]`) matching the Design System showcase.

#### Scenario: Telemetry cards rendered
- **WHEN** the dashboard renders the Telemetry Dock
- **THEN** metrics are displayed using clean tabular numerals (`tabular-nums font-bold`) and standard progress bars (`h-2 rounded-full`)
- **AND** zero SVG concentric circle computations are performed.

### Requirement: Mobile Responsiveness and Touch Safety
On mobile screens ($< 640\text{px}$), the Home Dashboard SHALL collapse the asymmetric 2:1 layout into a clean single-column vertical flow with zero horizontal scrolling.

#### Scenario: Mobile viewport viewing
- **WHEN** the dashboard is viewed on a mobile device ($390\text{px}$ width)
- **THEN** all cards collapse into a single vertical sequence
- **AND** action buttons and status pills retain full touch target heights ($\ge 36\text{px}$) and `whitespace-nowrap shrink-0`.
