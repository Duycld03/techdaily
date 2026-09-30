# Proposal: Rebalance Home Bento Dashboard Telemetry and Grid Alignment

## Why

Recent attempts to synchronize row heights across the Bento Dashboard using rigid CSS subgrid (`lg:grid-rows-2` and `lg:grid-rows-subgrid`) coupled with `justify-between` introduced an unnatural, oversized vertical void inside Telemetry Card 1 (Practice Streak & Consistency Card). Because the Hero Reading Card on the left is content-rich (~260-280px tall) while the Streak Card is inherently concise (~160px tall), forcing uniform row heights severely degraded aesthetic density. Furthermore, in the original natural layout, Telemetry Card 2 (Knowledge Radar Card) ended prematurely, leaving a ~55px gap at the bottom of the right column relative to the left column's practice cards.

By restoring natural independent flex-column rhythm across the two columns, keeping Telemetry Card 1 compact without forced vertical tearing, and expanding Telemetry Card 2 (Knowledge Radar) with a rich 2x2 quick-stats telemetry grid (Connected Concepts, Relations, Cards Due, Mastered Cards), both columns align flush at the bottom boundary naturally with high informational value and zero artificial whitespace.

## What Changes

- **Bento Dashboard Layout Simplification**: Revert rigid CSS subgrid declarations (`lg:grid-rows-2` and `lg:grid-rows-subgrid`) in `BentoDashboardLayout.vue` back to responsive independent column flex layouts (`flex flex-col justify-start`), eliminating cross-row height synchronization conflicts.
- **Compact Practice Streak Telemetry Card**: Revert Telemetry Card 1 in `HomeBentoDashboard.vue` to natural compact spacing (`space-y-3` / `space-y-3.5`) without `justify-between` and without trailing top borders that tear elements apart, restoring the clean, tight presentation of streak days and daily goal progress.
- **Knowledge Radar 2x2 Telemetry Expansion**: Expand Telemetry Card 2 (Knowledge Radar Card) in `HomeBentoDashboard.vue` from 2 stats to a balanced 2x2 metric grid utilizing existing store telemetry:
  1. `totalNodes` (Connected Concepts / Khái niệm liên kết)
  2. `totalEdges` (Active Relations / Mối quan hệ)
  3. `reviewStats.due` (Cards Due Today / Thẻ cần ôn luyện)
  4. `reviewStats.mastered` (Mastered Cards / Thẻ đã thành thạo)
  This adds ~55px of natural height, ensuring the bottom of the Telemetry Dock aligns flush with the bottom of the Action Stage's practice cards.
- **Preserve Disambiguated Navigation**: Maintain established navigation targets for the action cards: Tier 2 Sub-card 1 navigates to `/today?tab=challenge`, Tier 2 Sub-card 2 navigates to `/quiz`, and the Hero Card navigates to active slice reader.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `system-layout-archetypes`: Clarify the Bento Dashboard Archetype guidelines to emphasize natural column grouping and telemetry density over rigid subgrid row stretching when telemetry cards have unequal content depth.

## Impact

- **Frontend Components**:
  - `frontend/components/layout/BentoDashboardLayout.vue`: Clean flex-column architecture.
  - `frontend/components/dashboard/HomeBentoDashboard.vue`: Compact Streak Card and 2x2 Knowledge Radar grid.
- **Tests**:
  - `frontend/tests/components/dashboard/HomeBentoDashboard.spec.ts`: Verify rendering of 4 telemetry metrics and navigation handlers.
- **Zero API or DB Impact**: All telemetry metrics consume existing `graphStore` and `reviewStore` computed states.
