# Design

## Context

See `proposal.md` for motivation.

Current State:
- `frontend/pages/index.vue` serves as the Home Dashboard route (`/`).
- It imports and renders `frontend/components/today/TodayBentoDashboard.vue`, a component originally conceived when the Bento Grid resided inside `/today`.
- Inside `TodayBentoDashboard.vue:95`, `onMounted()` executes:
  ```ts
  if (!reviewStore.forecast) {
    reviewStore.fetchForecast().catch(() => {})
  }
  ```
  Since `useReviewStore` does not define `fetchForecast`, JavaScript throws a synchronous `TypeError: reviewStore.fetchForecast is not a function`. During full page refreshes (F5), this unhandled lifecycle exception is caught by Vue's error handler, which invokes Nuxt's `showError(...)` with status code 500, displaying `error.vue`.
- Card E renders `frontend/components/today/CyberRadarWidget.vue`, an arcade-style rotating sweep radar with cyan/neon green badges ("TELEMETRY LIVE ONLINE") that visually clash with the rest of the dark studio Bento cards.

## Goals / Non-Goals

**Goals:**
- Eliminate the F5 / direct load 500 error on `/` by aligning store calls with the actual API of `useReviewStore` and `useKnowledgeGraphStore`.
- Relocate and cleanly rename the dashboard component to `frontend/components/dashboard/HomeBentoDashboard.vue`.
- Replace the arcade `CyberRadarWidget.vue` with an engineering-grade `DomainConstellationCard.vue` that fits the studio design tokens (Obsidian canvas, Electric Violet accents, hairline borders).
- Provide automated unit test coverage for `HomeBentoDashboard.vue` and `DomainConstellationCard.vue` to guarantee regression defense.

**Non-Goals:**
- Modifying backend APIs, EF Core models, or database schemas (the backend is healthy and unchanged).
- Altering the Focus Studio components inside `frontend/pages/today.vue` (`DocReaderPane`, `InterviewChallengePane`).

## Decisions

### 1. Store Hardening & Defensive Lifecycle Execution
- **Spaced Repetition Stats**:
  - In `useReviewStore`, active data is stored in `deckStatistics` and `totalCardsDue`, populated via `fetchDeckCards({ pageSize: 1 })` or `fetchReviewDeck()`.
  - In `HomeBentoDashboard.vue`, replace the call to `fetchForecast` with `reviewStore.fetchDeckCards({ pageSize: 1 }).catch(() => {})`.
  - Compute `reviewStats` safely:
    ```ts
    const reviewStats = computed(() => {
      const stats = reviewStore.deckStatistics
      return {
        total: stats?.totalCards ?? 0,
        mastered: stats?.masteredCount ?? 0,
        due: reviewStore.totalCardsDue ?? 0
      }
    })
    ```
- **Knowledge Graph Data**:
  - In `useKnowledgeGraphStore`, reactive data lives in `rawData`, not `graphData`.
  - In `HomeBentoDashboard.vue`, access node and edge counts via `graphStore.rawData?.nodes?.length || 148` and `graphStore.rawData?.edges?.length || 210`.

### 2. Component Structure & Decoupling
- **Path**: `frontend/components/dashboard/HomeBentoDashboard.vue`.
- **Target Page**: `frontend/pages/index.vue` imports `HomeBentoDashboard`.
- **Obsolete Deletion**: Remove `frontend/components/today/TodayBentoDashboard.vue` to prevent stale duplicate code.

### 3. Domain Knowledge Constellation Redesign
- Replace `CyberRadarWidget.vue` with `frontend/components/dashboard/DomainConstellationCard.vue`.
- **Visual Structure**:
  - Header: Lucide `Network` or `Compass` icon, title "Domain Constellation", and high-contrast link "Open 3D Cosmos" (`/graph`).
  - Constellation Canvas: A custom, lightweight SVG showing 5 interconnected vertices representing TechDaily's 5 core engineering pillars (Distributed Systems, Backend Runtime, Database, System Design, Craft).
  - Styling: Semi-transparent vector connectors (`stroke="rgba(139, 92, 246, 0.25)"`), Electric Violet (`#8b5cf6`) and Cyber Cyan (`#22d3ee`) glowing nodes with subtle SVG radial gradients and pulse animations, retiring the arcade-style rotating sweep line.
  - Telemetry Bar: Compact 2-column cards showing total Nodes and Relations with clear typography (`font-mono text-sm font-bold`).

```
+--------------------------------------------------------------+
| [Network] Domain Constellation           Open 3D Cosmos [↗]  |
|                                                              |
|                     (Distributed Systems)                    |
|                            *                                 |
|                           / \                                |
|                          /   \                               |
|        (Craft) *-------*-------* (Backend Runtime)           |
|                 \     / \     /                              |
|                  \   /   \   /                               |
|                   \ /     \ /                                |
|         (Database) *-------* (System Design)                 |
|                                                              |
|      +-----------------------+-----------------------+       |
|      |       148 NODES       |     210 RELATIONS     |       |
|      +-----------------------+-----------------------+       |
+--------------------------------------------------------------+
```

## Risks / Trade-offs

- **Risk**: Stale cache in Vitest or component tests.
  - *Mitigation*: Update `frontend/tests/pages/index.spec.ts` and add dedicated test suites `frontend/tests/components/dashboard/HomeBentoDashboard.spec.ts` and `DomainConstellationCard.spec.ts`.
- **Risk**: Hydration mismatch between server date and client date in weekly calendar.
  - *Mitigation*: Ensure day calculations rely on deterministic array generation with safe fallback values.
