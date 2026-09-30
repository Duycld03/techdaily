# Tasks

## 1. Frontend - Reader Navigation & Auth Hardening

- [x] 1.1 Replace `window.location.assign` with `window.location.replace` in `frontend/pages/read/[bookId].vue` so cross-origin isolation reloads do not poison browser history.
- [x] 1.2 Harden `frontend/composables/useApiClient.ts` token retrieval to check `useCookie('techdaily_token')`, `localStorage`, and `sessionStorage`, and prevent premature session purging on initial hydration.
- [x] 1.3 Ensure `frontend/stores/useAuthStore.ts` sets `techdaily_token` cookies with explicit `SameSite=Lax` and `Path=/` across both persistent and session modes.
- [x] 1.4 Implement contextual back navigation in `frontend/components/reader/ReaderHeaderBar.vue` that returns to the previous originating route (`router.back()` or `/`) with fallback to `/library`.

## 2. Frontend - Dashboard Action Targets Disambiguation

- [x] 2.1 Update Tier 2 Sub-card 1 (Daily Micro-Drill) in `frontend/components/dashboard/HomeBentoDashboard.vue` to navigate to `/today?tab=challenge`.
- [x] 2.2 Update `frontend/pages/today.vue` to recognize `tab=challenge` query parameter, activating the challenge tab on mobile and focusing the challenge pane on desktop.
- [x] 2.3 Update Tier 2 Sub-card 2 (Senior Dilemma) in `frontend/components/dashboard/HomeBentoDashboard.vue` to navigate to `/quiz` (Architecture Interview Arena).

## 3. Frontend - Bento Dashboard Flush Grid Alignment

- [x] 3.1 Update `frontend/components/layout/BentoDashboardLayout.vue` to establish a synchronized 2-row CSS subgrid (`lg:grid-rows-2` and `lg:grid-rows-subgrid`) across the Action Stage and Telemetry Dock.
- [x] 3.2 Refactor the Practice Streak & Consistency Card in `frontend/components/dashboard/HomeBentoDashboard.vue` with `h-full flex flex-col justify-between`, anchoring the daily goal consistency progress bar flush with the neighboring Hero Reading Card's action footer.

## 4. Frontend - Dual-Gate Verification & Visual Inspection

- [x] 4.1 Run frontend unit tests (`npm test` in `frontend/`) verifying router navigation, auth store persistence, and Bento dashboard computed properties.
- [x] 4.2 Execute dual-gate automated visual verification via headless Chromium on Desktop (1440x900) and Mobile (390x844), capturing and visually inspecting screenshots of the dashboard and reader back navigation.
