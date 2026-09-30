# Spec Delta: System Layout Archetypes

## MODIFIED Requirements

### Requirement: Complete Layout Archetypes Demonstration
The `LayoutArchetypesShowcase` component and production dashboards SHALL demonstrate the `BentoDashboardLayout` with balanced engineering telemetry density and natural column flow:

1. **Four-Way Archetype Switcher**:
   - The archetype selector SHALL provide tabs for: `StudioLayout` (Flashcards Studio), `MasterDetailLayout` (Settings Master-Detail), `BoardLayout` (Notes Board), and `BentoDashboardLayout` (Executive Bento Dashboard).

2. **Bento Dashboard Layout Archetype Demo & Independent Column Flow**:
   - SHALL render `BentoDashboardLayout` with:
     - Header slot with greeting and status.
     - Action Stage (`#action-stage`) occupying 2 columns on desktop (`lg:col-span-2`) displaying primary practice cards.
     - Telemetry Dock (`#telemetry-dock`) occupying 1 column on desktop (`lg:col-span-1`) displaying consistency metrics and telemetry cards.
   - Both columns SHALL arrange their respective child cards as independent vertical flex stacks (`flex flex-col justify-start`) with consistent gap spacing (`gap-3.5 sm:gap-4`), prohibiting rigid multi-row subgrid stretching that introduces empty vertical voids inside compact telemetry cards.
   - SHALL automatically collapse into a single vertical column on tablet and mobile viewports (< 1024px) without horizontal overflow.

3. **Telemetry Dock Metric Density & Bottom Alignment**:
   - The Telemetry Dock SHALL present complementary metrics without artificial padding:
     - **Practice Streak Card**: SHALL display streak counter, SM-2 retention context, and daily time goal progress in a compact container without vertical stretching.
     - **Knowledge Radar Card**: SHALL display graph constellation metrics via a balanced 2x2 metric grid (Connected Concepts, Active Relations, Cards Due Today, Mastered Cards) and a direct navigation trigger to the 3D Cosmos view.
   - The combined natural height of the Telemetry Dock cards SHALL match the total height of the Action Stage cards, aligning flush at the bottom boundary on 1080p desktop viewports.

#### Scenario: Switching to Bento Dashboard archetype in showcase
- **WHEN** a user selects "Bento Dashboard" in the layout archetypes switcher
- **THEN** the view renders the live `BentoDashboardLayout` with a 2-column action stage and 1-column telemetry dock on desktop viewports.

#### Scenario: Telemetry cards maintain natural density without vertical void
- **WHEN** an engineer views the Home Bento Dashboard on a 1920x1080 desktop display
- **THEN** the Practice Streak Card renders in a compact format without an internal empty void
- **AND** the Knowledge Radar Card renders a 2x2 quick-stats grid displaying concepts, relations, due cards, and mastered count
- **AND** the bottom border of the Knowledge Radar Card aligns flush with the bottom border of the adjacent practice cards.

#### Scenario: Mobile viewport responsiveness
- **WHEN** a user views the Bento Dashboard on a mobile display ($< 1024\text{px}$)
- **THEN** the Action Stage and Telemetry Dock stack sequentially in a single column
- **AND** all cards preserve their full informational density with zero horizontal overflow.
