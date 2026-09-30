# Design: Bento Dashboard Telemetry Density and Natural Column Balance

## Context

See `proposal.md` for background and user observations. The Bento Dashboard layout archetype uses a 2:1 horizontal desktop split (Action Stage on the left, Telemetry Dock on the right). Previous CSS subgrid synchronization (`lg:grid-rows-subgrid`) forced Row 1 on the right to match the height of the left card, creating an unsightly gap in the Practice Streak Card. Returning to independent vertical flex columns allows each card to size naturally according to its content depth.

## Goals / Non-Goals

**Goals:**
- Eliminate the dead empty space in Telemetry Card 1 (Practice Streak Card) by restoring compact, natural flex spacing.
- Expand Telemetry Card 2 (Knowledge Radar Card) with a 2x2 telemetry grid (Concepts, Relations, Due, Mastered) using existing Pinia store computed state.
- Ensure the bottom edge of Telemetry Card 2 aligns flush with the bottom edge of the Action Stage's practice cards on 1080p desktop screens.
- Keep established navigation actions for the practice cards: Micro-Drill (`/today?tab=challenge`), Senior Dilemma (`/quiz`), and Reader.

**Non-Goals:**
- Backend API or database schema modifications (all metrics are already loaded via `useReviewStore` and `useGraphStore`).
- Redesigning the mobile layout (cards already stack sequentially in a single column on mobile).

## Decisions

### Decision 1: Independent Flex Column Flow vs Rigid CSS Subgrid
- **Choice**: Revert `BentoDashboardLayout.vue` from `lg:grid-rows-subgrid` to `flex flex-col justify-start` in both `<section>` and `<aside>`.
- **Rationale**: When cards in the same row have drastically different content structures (Hero Reading Card has 7 lines/controls vs Streak Card with 3 lines), CSS subgrid stretches the smaller card and `justify-between` creates an awkward void. Independent columns let cards expand to their natural content height.
- **Alternatives Considered**:
  - *Keep subgrid and add dummy fillers*: Artificial or fake content degrades information clarity.
  - *Keep subgrid with `justify-start`*: Leaves dead space below the Streak Card inside Row 1, pushing the Knowledge Radar card down awkwardly.

### Decision 2: 2x2 Quick Stats Grid in Knowledge Radar Card
- **Choice**: Display four metric tiles in `HomeBentoDashboard.vue` under Telemetry Card 2:
  1. `totalNodes` (`dashboard.connected_nodes`)
  2. `totalEdges` (`dashboard.active_relations`)
  3. `reviewStats.due` (`dashboard.cards_due`)
  4. `reviewStats.mastered` (`dashboard.stat_mastered_cards` / Mastered)
- **Rationale**: Currently the card only has 2 tiles (`totalNodes` and `reviewStats.due`). Adding `totalEdges` and `reviewStats.mastered` forms a balanced 2-column by 2-row grid. This adds exactly ~55px of vertical height to Telemetry Card 2, perfectly aligning the bottom of the right column with the bottom of the left column.

## Risks / Trade-offs

- **Risk**: Slight height variation if localized strings in Vietnamese or English wrap differently.
  - **Mitigation**: All stat tiles use `truncate` or compact uppercase labels, and container heights use `h-full flex flex-col justify-between` on Card 2 only to absorb minor 2-4px subpixel differences smoothly.
