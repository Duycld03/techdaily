# Tasks

## 1. Insights & Library Tab Switchers Fix

- [x] 1.1 Update `frontend/pages/insights.vue` View Mode Switcher buttons to include `border border-transparent` in base/inactive state and replace `transition-all` with `transition-colors`.
- [x] 1.2 Update `frontend/pages/library.vue` Import Document Modal 3-tab switcher (`markdown`, `pdf`, `url`) to include `border border-transparent` in base/inactive state and replace `transition-all` with `transition-colors`.

## 2. Sidebar Navigation & Locale Selector Fix

- [x] 2.1 Update `frontend/components/layout/AppSidebar.vue` navigation links to include `border-l-2 border-transparent` in base/inactive state and replace `transition-all` with `transition-colors`.
- [x] 2.2 Update `frontend/components/common/LocaleSelector.vue` buttons to replace `transition-all` with `transition-colors` to prevent focus/blur shadow and border flicker.

## 3. Pacer & Auxiliary Tab Switchers Fix

- [x] 3.1 Update `frontend/pages/today.vue` pacer slice buttons to include `border-l-2 border-transparent` in base/inactive state and replace `transition-all` with `transition-colors`.
- [x] 3.2 Update `frontend/pages/quiz.vue`, `frontend/pages/review.vue`, and `frontend/pages/profile.vue` tab switchers to use constant border geometry and `transition-colors`.

## 4. Automated Verification

- [x] 4.1 Run frontend test suite (`npm --prefix frontend test`) to ensure zero regressions across all 53 test files.
- [x] 4.2 Validate OpenSpec changes and specifications (`openspec validate --changes` and `openspec validate --specs`).
