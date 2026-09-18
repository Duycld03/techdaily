# Tasks

## 1. Dynamic Pacer Badges & i18n

- [x] 1.1 Add `active_slice_badge` to `frontend/i18n/locales/en.json` ("Slice {current} / {total}") and `vi.json` ("Lát cắt {current} / {total}").
- [x] 1.2 Update `frontend/components/dashboard/HomeBentoDashboard.vue` to compute dynamic `sliceBadgeText` using `pacer.currentChunkOrder` and `pacer.totalChunks`, eliminating the hardcoded `/ 30`.
- [x] 1.3 Update `frontend/components/layout/AppHeader.vue` to display dynamic slice counts from `focusStore.data.pacer` without hardcoded `/ 30`.

## 2. Constellation Card Visual Polish

- [x] 2.1 In `frontend/components/dashboard/DomainConstellationCard.vue`, replace `animate-ping origin-center` with a stationary pulsing aura (`animate-pulse`) to eliminate off-center bubble scaling.
- [x] 2.2 In `frontend/components/dashboard/DomainConstellationCard.vue`, remove `group-hover:translate-x-0.5` from the `<NuxtLink>` title to prevent text jumping on card hover.

## 3. Differentiated Dashboard Navigation

- [x] 3.1 Update `handleStartReading()` in `frontend/components/dashboard/HomeBentoDashboard.vue` to navigate directly to `/read/${pacer.bookId}?slice=${pacer.currentChunkOrder}` when active book exists, with fallback to `/today`.
- [x] 3.2 Ensure `handleStartScenario()` in `frontend/components/dashboard/HomeBentoDashboard.vue` navigates to `/today`.

## 4. Automated Testing & Verification

- [x] 4.1 Update `frontend/tests/components/dashboard/HomeBentoDashboard.spec.ts` to assert dynamic slice badge rendering and reader navigation routing.
- [x] 4.2 Update `frontend/tests/components/dashboard/DomainConstellationCard.spec.ts` to verify stable hover styling and pulse aura rendering.
- [x] 4.3 Run all frontend tests (`npm --prefix frontend test`) to ensure zero regressions across all test files.
- [x] 4.4 Validate OpenSpec specifications and changes (`openspec validate --changes` and `openspec validate --specs`).
