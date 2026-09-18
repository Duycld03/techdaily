# Proposal

## Why

When users refresh (F5) or directly load the Home Dashboard (`/`), the application crashes into the `error.vue` error boundary with `ERROR CODE: 500`. This crash is triggered during client hydration in `onMounted`, where `TodayBentoDashboard.vue` synchronously invokes `reviewStore.fetchForecast()`, a method that does not exist in `useReviewStore`, throwing an unhandled `TypeError`.

Additionally, the dashboard component remains lingering in `frontend/components/today/TodayBentoDashboard.vue` with legacy `/today` naming and assumptions despite commit `821bb68` having separated `/` (Home Dashboard) from `/today` (Focus Studio). Furthermore, Card E in the Bento Grid currently renders an arcade-like "Knowledge Radar" (`CyberRadarWidget.vue`) with blinking cyber neon telemetry ("TELEMETRY LIVE ONLINE") and rotating radar sweeps that aesthetically clash ("lạc quẻ") with TechDaily's clean, engineering-grade Dev-Learning Studio design language (Obsidian canvas, Electric Violet accents, hairline borders).

## What Changes

- **Crash Fix & Store Integration Hardening**:
  - Eliminate the invalid `reviewStore.fetchForecast()` call in `onMounted`. Use standard `useReviewStore` methods (`fetchDeckCards({ pageSize: 1 })` or `fetchReviewDeck()`) with optional chaining and fallback to `deckStatistics` / `totalCardsDue`.
  - Fix Knowledge Graph store property access from `graphStore.graphData` to the real reactive state `graphStore.rawData`.
- **Component Relocation & Pure Naming**:
  - Relocate `frontend/components/today/TodayBentoDashboard.vue` to `frontend/components/dashboard/HomeBentoDashboard.vue`.
  - Update `frontend/pages/index.vue` to import `HomeBentoDashboard.vue`.
  - Ensure zero residual broken imports or dangling files in `frontend/components/today/`.
- **Domain Knowledge Constellation Card Redesign**:
  - Replace the arcade-style `CyberRadarWidget.vue` with an engineering-grade **Domain Knowledge Constellation** widget (`DomainConstellationCard.vue` in `frontend/components/dashboard/`).
  - Represent knowledge coverage across the 5 core engineering pillars (Distributed Systems, Backend Runtime, Database, System Design, Craft) with clean SVG constellation vertices, subtle interconnecting paths, and real node/relation statistics.
  - Align visual tokens with the studio design system: Electric Violet (`brand`), Obsidian slate (`canvas-subtle`, `canvas-elevated`), hairline borders (`border-white/[0.08]`), and sleek navigation CTA to `/graph`.
- **Comprehensive Automated Test Coverage**:
  - Create dedicated unit tests for `HomeBentoDashboard.vue` and `DomainConstellationCard.vue` verifying clean mount under empty store states, error-free lifecycle execution, and responsive telemetry rendering.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Modernize Home Dashboard architecture, guarantee crash-free page load and refresh (F5), and standardize Domain Knowledge Constellation telemetry.

## Impact

- **Affected Areas**: `frontend/pages/index.vue`, `frontend/components/dashboard/HomeBentoDashboard.vue`, `frontend/components/dashboard/DomainConstellationCard.vue`, `frontend/tests/components/dashboard/`.
- **User Experience**: Home page F5 / full-page load functions reliably with zero 500 error screens; dashboard visual aesthetics harmonize across all cards.
- **Breaking Changes**: None. Internal component relocation only.
