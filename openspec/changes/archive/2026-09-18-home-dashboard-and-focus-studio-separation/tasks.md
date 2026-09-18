# Tasks

## 1. Frontend - Core Dashboard Components Refactoring

- [x] 1.1 Update `frontend/components/today/TodayBentoDashboard.vue` to remove the out-of-context "Ask AI Explainer" button from the Welcome Banner.
- [x] 1.2 Apply zero-scroll desktop constraints (`lg:h-[calc(100vh-3.5rem)] lg:overflow-hidden flex flex-col justify-between p-4 sm:p-5`) in `TodayBentoDashboard.vue`, tightening container gaps and card padding so all 5 cards fit above the fold.
- [x] 1.3 Refactor `frontend/components/today/ConcentricMetricCard.vue` to remove `h-full` and unbounded `justify-between` stretching, scaling SVG rings to $R_1=46\text{px}, R_2=34\text{px}$ in a compact 120x120 viewBox.
- [x] 1.4 Wire action events in `TodayBentoDashboard.vue` so that clicking "Continue Reading" or "Solve Challenge" invokes `navigateTo('/today')`.

## 2. Frontend - Route-Level Architecture & Focus Studio Refactoring

- [x] 2.1 Refactor `frontend/pages/index.vue` to host the primary Home Command Center Dashboard (`TodayBentoDashboard.vue`) with authentication guard and store hydration instead of redirecting to `/today`.
- [x] 2.2 Refactor `frontend/pages/today.vue` to act exclusively as the Focus Studio, removing the segmented view-mode switcher and embedded `TodayBentoDashboard.vue`, rendering the slice pacer bar and dual-pane reader/challenge directly.
- [x] 2.3 Deprecate or align `frontend/composables/useTodayViewMode.ts` to prevent state drift now that dashboard and focus studio are separate routes.

## 3. Frontend - App Shell & Global Navigation Alignment

- [x] 3.1 Update `frontend/components/layout/AppSidebar.vue` navigation items to register `Dashboard` (`/`, `LayoutGrid` icon) and `Today's Focus` (`/today`, `Target` icon), updating active route detection.
- [x] 3.2 Update `frontend/components/app/AppCommandPalette.vue` search items to distinguish the root Dashboard (`/`) from Today's Focus Studio (`/today`).
- [x] 3.3 Verify and update locale dictionaries in `frontend/locales/en.json` and `frontend/locales/vi.json` to ensure clean bilingual display without missing keys or wrapping bugs.

## 4. Verification & Automated Tests

- [x] 4.1 Update existing unit tests in `frontend/tests/components/today/ConcentricMetricCard.spec.ts` and `frontend/tests/components/app/AppCommandPalette.spec.ts`.
- [x] 4.2 Update or add test coverage for `frontend/pages/index.vue` and `frontend/tests/composables/useTodayViewMode.spec.ts`.
- [x] 4.3 Execute full frontend unit test suite (`npm --prefix frontend test`) and backend test suite (`dotnet test backend/TechDaily.sln`) to verify zero regressions.
