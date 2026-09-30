# Proposal

## Why

Navigating from the Home Bento Dashboard to the Reader and pressing browser back causes an infinite reload loop and wipes the user's authentication session due to `window.location.assign` history poisoning across the Cross-Origin-Opener-Policy (`COOP: same-origin`) process boundary. Concurrently, both Tier 2 action buttons on the dashboard redundantly navigate to `/today`, and asymmetric card content heights in row 1 create an unsightly hollow notch in the dashboard bento grid. Resolving these issues restores seamless navigation flow, clarifies learning paths, and guarantees flush visual enclosure.

## What Changes

- **Reader Navigation & Auth Resilience**:
  - Replace `window.location.assign` with `window.location.replace` in `frontend/pages/read/[bookId].vue` so cross-origin isolation reloads do not poison browser history with aborted intermediate states.
  - Guard `useAuthStore` and `useApiClient` against accidental session purging during COOP context transitions, ensuring persistent cookie/localStorage token resolution during SSR hydration.
  - Add contextual back navigation in `ReaderHeaderBar.vue` that intelligently returns to the previous route (Dashboard or Library) rather than a rigid link to `/library`.
- **Differentiated Practice Action Targets**:
  - Update Tier 2 Sub-card 1 (Daily Micro-Drill) to navigate to `/today?tab=challenge` with the challenge dock focused.
  - Update Tier 2 Sub-card 2 (Senior Dilemma) to navigate to `/quiz` (Architecture Interview Arena) with level pre-selection.
- **Bento Dashboard Row Height Alignment**:
  - Refactor `BentoDashboardLayout.vue` and `HomeBentoDashboard.vue` to adopt synchronized 2-row CSS subgrid (`lg:grid-rows-2` and `lg:grid-rows-subgrid`).
  - Restructure the "Ghi Nhớ & Mục Tiêu" (Streak & Consistency) card with `h-full flex flex-col justify-between` so its daily goal bar anchors flush with the neighboring Hero Reading card's action footer.

## Capabilities

### New Capabilities
<!-- No new top-level capabilities introduced -->

### Modified Capabilities
- `reader`: Enforce history-replacing cross-origin isolation reload, resilient auth persistence across COOP boundaries, and originating back navigation.
- `today`: Differentiate daily focus navigation contracts between reading slices, daily micro-drills, and senior scenario arenas.
- `system-layout-archetypes`: Enforce 2x2 flush bento grid row height synchronization across action stage and telemetry dock columns.

## Impact

- **Frontend Routes & Components**:
  - `frontend/pages/read/[bookId].vue`
  - `frontend/components/reader/ReaderHeaderBar.vue`
  - `frontend/components/dashboard/HomeBentoDashboard.vue`
  - `frontend/components/layout/BentoDashboardLayout.vue`
  - `frontend/composables/useApiClient.ts`
  - `frontend/stores/useAuthStore.ts`
- **Backend & APIs**: No database schema or backend endpoint modifications required.
- **Dependencies**: No new npm or NuGet packages.
