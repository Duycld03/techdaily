# Design: Audit & Cleanup Frontend Tests to Eliminate Test Theater

## Context

TechDaily's frontend test suite currently runs 469 tests across 65 test files using Vitest and Happy-DOM. An audit revealed dozens of tests dedicated to checking Tailwind CSS class strings (e.g. `classes().toContain('max-w-5xl')`, `classes().toContain('lg:grid-cols-12')`, `classes().toContain('shrink-0')`, `classes().toContain('whitespace-nowrap')`).

Happy-DOM does not include a CSS layout engine. It does not parse stylesheet rules, compute box model coordinates, render fonts, or detect visual collisions. Consequently:
1. Asserting that an element contains `shrink-0` or `lg:w-[32%]` does NOT verify that the element does not shrink or that the column occupies 32% width on screen.
2. Styling refactors (such as tuning padding or adjusting grid gaps) cause tests to fail even when application behavior and user UX are completely intact.
3. Tests run slower, consume more memory, and mislead developers into believing visual layout is tested when it is not.

## Goals / Non-Goals

**Goals:**
- Systematically audit all test files in `frontend/tests/components/`, `frontend/tests/pages/`, and `frontend/tests/layout/`.
- Purge all CSS class assertions (`expect(el.classes()).toContain(...)` and `expect(el.classes()).not.toContain(...)`) checking visual styling.
- Delete tautological tests that exist solely to assert styling classes.
- Where an assertion was intended to check active/inactive state, replace it with accessible attribute assertions (e.g. `attributes('aria-current')`, `attributes('aria-pressed')`, `attributes('disabled')`) or behavioral DOM state.
- Ensure the pruned test suite executes 100% green with reduced execution time and zero flakiness.

**Non-Goals:**
- Removing behavioral unit tests covering form serialization, input binding, validation error rendering, Pinia store actions, or route navigation guards.
- Modifying production Vue SFCs or application code.

## Decisions

### Decision 1: Systematic Audit Order by Directory
Tests will be audited and cleaned in three distinct batches:
1. **Component Primitives (`frontend/tests/components/*.spec.ts`)**:
   - Clean `BentoDashboardLayout.spec.ts`, `LayoutArchetypes.spec.ts`, `OptionCard.spec.ts`, `DocReaderPane.spec.ts`, `TermExplainerModal.spec.ts`, `BasePagination.spec.ts`, `reviewBentoComponents.spec.ts`.
2. **Page Views (`frontend/tests/pages/*.spec.ts`)**:
   - Clean `login.spec.ts`, `library.spec.ts`, `roadmap.spec.ts`, `notes.spec.ts`, `review.spec.ts`, `quiz.spec.ts`, `settings.spec.ts`.
3. **Layout & Graph Suites (`frontend/tests/layout/*.spec.ts` & `frontend/tests/components/graph/*.spec.ts`)**:
   - Clean `AppChrome.spec.ts`, `GraphControlBar.spec.ts`, `GraphDetailDrawer.spec.ts`, `GraphLegend.spec.ts`, `RoadmapMindmapCanvas.spec.ts`, `RoadmapViewSwitcher.spec.ts`.

### Decision 2: State Assertion Migration Pattern
When a test asserts active/selected state, replace class checks with accessible attribute checks:
- **Old (Test Theater)**:
  ```ts
  expect(btn.classes()).toContain('bg-brand-600')
  expect(btn.classes()).toContain('font-bold')
  ```
- **New (Accessible State)**:
  ```ts
  expect(btn.attributes('aria-current')).toBe('page') // or attributes('aria-pressed') / active prop
  ```
When an element's sole test was checking its layout classes (e.g. `expect(col.classes()).toContain('lg:col-span-2')`), remove the assertion or delete the test block entirely if it asserts no observable user behavior.

### Decision 3: Acceptance of Reduced Test Count as a Quality Improvement
Pruning tautological class tests will decrease the total test count (e.g., from 469 to ~410-430). In accordance with the project's engineering philosophy ("Tests earn their place only where a plausible bug would fail them"), eliminating weightless tests reduces build overhead and token consumption without sacrificing behavioral coverage.

## Risks / Trade-offs

- **Risk**: Accidentally deleting an assertion that verifies a critical conditional state toggle (e.g. an element dynamically rendered via `v-if`).
  - **Mitigation**: Verify that the conditional presence (`expect(el.exists()).toBe(...)`) or ARIA attribute is preserved. Run `npm test` after each batch to guarantee 100% passing tests.
