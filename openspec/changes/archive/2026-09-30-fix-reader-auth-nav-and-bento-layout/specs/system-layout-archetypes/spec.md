# Spec Delta

## ADDED Requirements

### Requirement: Bento Dashboard Row Height Synchronization
The Bento Dashboard layout (`BentoDashboardLayout.vue`) and canonical dashboard implementation (`HomeBentoDashboard.vue`) SHALL synchronize card row heights across the Action Stage (columns 1–2) and Telemetry Dock (column 3) on desktop viewports (`lg:`):
1. The Hero Active Reading Card and the Practice Streak & Consistency Card in Row 1 SHALL share the exact same rendered vertical height, eliminating unaligned horizontal seams and hollow corner notches.
2. The Practice Streak & Consistency Card SHALL implement an `h-full flex flex-col justify-between` layout, anchoring its streak count, SM-2 retention copy, and daily goal progress bar so that its footer elements align flush with the neighboring Hero Reading Card's action controls.
3. The Tier 2 Split Practice Subgrid and the Knowledge Constellation Card in Row 2 SHALL synchronize vertical heights, forming an airtight, fully enclosed 2x2 rectangular bento architecture.

#### Scenario: Row 1 cards align flush on desktop viewports
- **WHEN** the dashboard renders on viewports with width >= 1024px
- **THEN** the bottom border of the Hero Active Reading Card and the bottom border of the Practice Streak & Consistency Card align to the exact same horizontal coordinate

#### Scenario: Row 2 cards align flush on desktop viewports
- **WHEN** the dashboard renders on viewports with width >= 1024px
- **THEN** the bottom border of the Tier 2 Split Practice Subgrid and the bottom border of the Knowledge Constellation Card align to the exact same horizontal coordinate
