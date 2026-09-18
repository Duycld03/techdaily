# Tasks

## 1. Domain Knowledge Constellation Widget

- [x] 1.1 Create `frontend/components/dashboard/DomainConstellationCard.vue` featuring a clean SVG constellation connecting 5 core engineering pillars (Distributed Systems, Backend Runtime, Database, System Design, Craft), hairline connectors, telemetry counters, and high-contrast link to `/graph`.
- [x] 1.2 Create unit tests in `frontend/tests/components/dashboard/DomainConstellationCard.spec.ts` verifying node/relation counters, pillar vertex rendering, and navigation anchor properties.

## 2. Home Bento Dashboard Relocation & Store Hardening

- [x] 2.1 Create `frontend/components/dashboard/HomeBentoDashboard.vue` migrating markup and logic from `frontend/components/today/TodayBentoDashboard.vue`.
- [x] 2.2 Fix Spaced Repetition store integration in `HomeBentoDashboard.vue`: eliminate invalid `reviewStore.fetchForecast()` call, replace with safe `reviewStore.fetchDeckCards({ pageSize: 1 })`, and bind `deckStatistics` / `totalCardsDue` with defensive defaults.
- [x] 2.3 Fix Knowledge Graph store access in `HomeBentoDashboard.vue`: replace invalid `graphStore.graphData` lookup with `graphStore.rawData`.
- [x] 2.4 Embed `DomainConstellationCard.vue` into Card E slot of `HomeBentoDashboard.vue`.
- [x] 2.5 Update `frontend/pages/index.vue` to import and render `HomeBentoDashboard.vue`.
- [x] 2.6 Safely remove deprecated `frontend/components/today/TodayBentoDashboard.vue` and `frontend/components/today/CyberRadarWidget.vue` along with obsolete test files.

## 3. Automated Testing & Verification

- [x] 3.1 Update `frontend/tests/pages/index.spec.ts` to reference `HomeBentoDashboard`.
- [x] 3.2 Create unit test suite `frontend/tests/components/dashboard/HomeBentoDashboard.spec.ts` verifying crash-free mount under empty or loading store states.
- [x] 3.3 Run all frontend tests (`npm --prefix frontend test`) to verify zero regressions across all test files.
- [x] 3.4 Validate OpenSpec specifications and changes (`openspec validate --changes` and `openspec validate --specs`).
