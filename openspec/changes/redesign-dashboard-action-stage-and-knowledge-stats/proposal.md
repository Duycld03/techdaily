# Proposal

## Why

The Home Dashboard (`/`) currently suffers from severe visual imbalance and confusing UX:
1. The primary Action Stage in the left 2 columns was collapsed into a single card with `h-full flex flex-col justify-between`. Because the right column contains 3 stacked cards (~600px tall), the left card is unnaturally stretched, creating a massive ~350px empty dark void in the center of the dashboard.
2. The Daily Drill completion badge (`[✓] Đã Hoàn Thành: 0/10` / `[✓] Completed: 0/10`) was misplaced into the header of the Active Reading card next to the reading duration, misleading users into believing it denotes reading progress. Furthermore, when a drill is submitted with an incorrect answer (`Score = 0`), the UI displays a green success checkmark and emerald styling alongside `0/10`, representing contradictory and broken status semantics.
3. The "Chòm Sao Tri Thức" (Domain Knowledge Constellation) widget in the right column renders a 100% hardcoded static SVG with fixed English pillar labels ("Distributed", "Runtime", "Database", "Architecture", "Craft") that never change regardless of the user's studied subjects or library contents, wasting vertical screen space without providing real data insights.

## What Changes

- **Restore 2 Distinct Action Stage Cards in Bento Grid**:
  - **Card A (Active Reading Hero)**: Dedicated to the daily curriculum slice. Contains book icon, title, slice order, estimated reading minutes, chapter title, summary, progress bar, and primary "Continue Reading →" CTA button. Strips out all daily drill badges.
  - **Card B (Today's Practice & Scenario Cockpit)**: Dedicated to the architectural interview dilemma / drill. Contains target icon, practice badge, semantic drill status badge, question/situation teaser, and "Solve Challenge →" or "Review Solution →" CTA button.
- **Fix Semantic Drill Status Badge**:
  - Unsubmitted / Pending: Amber badge `+10 Points / Điểm thưởng` (icon `Target`).
  - Passed / Correct (`Score > 0`): Emerald badge `✓ Completed: 10/10` (icon `CheckCircle2`).
  - Failed / Incorrect (`Score == 0`): Rose / Amber badge `✗ Needs Review: 0/10` (icon `AlertCircle`), eliminating the misleading green checkmark on zero score.
- **Replace Static SVG with Genuine Knowledge Breakdown Table**:
  - Replace the static 5-point constellation SVG in `DomainConstellationCard.vue` with a live knowledge breakdown widget backed by `graphStore.rawData.stats` and `graphStore.rawData.nodes`.
  - Display actual artifact metrics: SM-2 Spaced Repetition flashcards count, personal highlights & reflections count, learned document slices count, and connected concepts / relations summary.
  - Retain the direct link to the interactive 3D Cosmos (`/graph`).

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Updates requirement for Home Command Center Dashboard Bento layout to enforce the 2-card Action Stage (Reading Hero + Practice Session), correct drill completion badge semantics, and real Knowledge Graph telemetry breakdown replacing the hardcoded SVG.

### Impact
- `frontend/components/dashboard/HomeBentoDashboard.vue`: Split unified card into Card A (Reading Slice) and Card B (Today's Practice), fix drill badge placement and styling.
- `frontend/components/dashboard/DomainConstellationCard.vue`: Replace static SVG with dynamic knowledge distribution table and real graph statistics.
- `frontend/tests/`: Update dashboard component tests and snapshot/contract assertions.
- Zero breaking backend API changes; all required data is already exposed via `/api/v1/today` and `/api/v1/graph`.
