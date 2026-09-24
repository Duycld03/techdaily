# Tasks: Audit & Cleanup Frontend Test Suite to Enforce Governance Pillars

## 1. Frontend - Component Tests Cleanup

- [x] 1.1 In `frontend/tests/components/LayoutArchetypes.spec.ts` and `BentoDashboardLayout.spec.ts`, purge CSS class assertions (`flex-1`, `lg:w-[32%]`, `lg:col-span-2`), preserving slot rendering and geometry prop tests
- [x] 1.2 In `frontend/tests/components/DocReaderPane.spec.ts`, `TermExplainerModal.spec.ts`, and `BasePagination.spec.ts`, remove styling utility checks (`flex`, `items-center`, `shrink-0`, `truncate`, `min-w-0`, `bg-brand-600`), retaining ARIA attributes, text content, and modal triggers
- [x] 1.3 In `frontend/tests/components/OptionCard.spec.ts` and `reviewBentoComponents.spec.ts`, remove border and color class checks (`border-brand-500`, `border-emerald-500`, `bg-brand-600`), asserting `aria-pressed`, icon presence, and click emissions

## 2. Frontend - Page Tests Cleanup

- [x] 2.1 In `frontend/tests/pages/login.spec.ts`, remove layout class assertions (`max-w-5xl`, `gap-3.5`, `hidden`, `lg:block`), focusing tests on form submission payloads, password visibility toggles, and validation error messages
- [x] 2.2 In `frontend/tests/pages/library.spec.ts`, remove card styling class assertions (`flex`, `flex-col`, `mt-auto`, `pt-3`, `bg-slate-100`), preserving book filtering, pagination, and category inference behavior
- [x] 2.3 In `frontend/tests/pages/roadmap.spec.ts` and `frontend/tests/pages/notes.spec.ts`, remove popover and line styling assertions (`overflow-visible`, `z-20`, `z-50`, `border-brand-*`), keeping track switcher selection, note conversion, and drawer inspection logic

## 3. Frontend - Layout & Graph Tests Cleanup

- [x] 3.1 In `frontend/tests/layout/AppChrome.spec.ts`, remove header and sidebar positioning class assertions (`sticky`, `z-40`, `z-50`, `md:w-64`), preserving brand title, streak badge presence, and navigation links
- [x] 3.2 In `frontend/tests/components/graph/*.spec.ts` and `RoadmapMindmapCanvas.spec.ts`, remove cursor and flex utility assertions (`cursor-grab`, `cursor-grabbing`, `shrink-0`, `whitespace-nowrap`), retaining node selection, drawer open/close, and event listeners

## 4. Verification & Validation

- [x] 4.1 Execute full frontend test suite (`npm test`) to confirm 100% pass rate with improved speed and zero flakiness
- [x] 4.2 Execute frontend production build (`npm run build`) to ensure zero regressions
- [x] 4.3 Run `openspec validate cleanup-frontend-tests-by-governance-pillars --json` to verify schema compliance
