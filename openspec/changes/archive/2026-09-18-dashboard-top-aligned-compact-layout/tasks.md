# Tasks

## 1. Frontend - Dashboard Layout Alignment

- [x] 1.1 Update outer container in `TodayBentoDashboard.vue` to use `justify-start gap-3.5 sm:gap-4` instead of `justify-between`.
- [x] 1.2 Update the 3-column grid and column flexboxes in `TodayBentoDashboard.vue` to use `items-start` and `justify-start gap-3.5 sm:gap-4`.
- [x] 1.3 Adjust Card A (Active Reading Slice) and Card B (Scenario Challenge) in `TodayBentoDashboard.vue` to remove `flex-1` stretching and maintain natural compact heights.
- [x] 1.4 Verify right column cards (Concentric Metrics, 7-Day Consistency, Knowledge Radar) stack snugly with uniform gap spacing matching the left column.

## 2. Verification & Visual Tests

- [x] 2.1 Run frontend unit tests (`npm --prefix frontend test`) to verify zero regressions across all 50 test files.
- [x] 2.2 Validate OpenSpec specifications (`openspec validate --changes` and `openspec validate --specs`).
