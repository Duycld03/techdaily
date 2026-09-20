# Tasks: Modernize Mobile Navigation Drawer

## 1. Frontend: Shared Navigation Architecture

- [x] 1.1 Create `frontend/composables/useNavigationMenu.ts` with centralized `navGroups` (including Dashboard `/` with `LayoutGrid` icon, Practice, Knowledge, and Account groups) and typed `NavGroup` interface.
- [x] 1.2 Implement route active determination helper `isLinkActive(linkPath: string, currentPath: string): boolean` with exact `/` matching and proper prefix matching for `/today`, `/library`, `/read`, and `/graph`.

## 2. Frontend: Mobile Navigation Drawer UI Modernization

- [x] 2.1 Refactor `frontend/components/layout/AppHeader.vue` to consume `useNavigationMenu()` for navigation groups and active link logic, removing duplicate local arrays.
- [x] 2.2 Update mobile drawer container and overlay styling with Dev-Learning Studio tokens (`dark:bg-canvas-subtle/95`, `border-slate-200/80 dark:border-white/[0.08]`, `backdrop-blur-md`).
- [x] 2.3 Modernize mobile drawer header branding to use the canonical TechDaily badge (`bg-gradient-to-tr from-brand-600 via-brand-500 to-indigo-400`), stylized `TechDaily` brand mark, and accessible close button hover states.
- [x] 2.4 Apply zero-shift active link styling (`border-l-2 border-brand-500` with `bg-brand-500/10 dark:bg-white/[0.06]`) and `:stroke-width="1.5"` iconography matching `AppSidebar.vue`.
- [x] 2.5 Refactor mobile drawer footer with `.glass-card` user profile card, user initial avatar, and localized logout button (`$t('nav.logout')`).
- [x] 2.6 Add `data-testid="mobile-nav-drawer"` and `data-testid="mobile-nav-title"` for robust test targeting.

## 3. Frontend: AppSidebar Integration & Deduplication

- [x] 3.1 Refactor `frontend/components/layout/AppSidebar.vue` to reuse `useNavigationMenu()` instead of local duplicate arrays and route matching methods.
- [x] 3.2 Verify desktop sidebar renders identically with zero visual regression.

## 4. Verification & Testing

- [x] 4.1 Update `frontend/e2e/test-navigation-locales.mjs` to target the updated mobile drawer title and assert Dashboard (`/`) and Today (`/today`) active links on mobile viewports.
- [x] 4.2 Run frontend unit tests (`npm test` in `frontend/`) and type checking.
- [x] 4.3 Run Playwright / E2E navigation verification in mobile emulation mode (375x667 and 390x844 viewports) across English (`en-US`) and Vietnamese (`vi-VN`) locales.
