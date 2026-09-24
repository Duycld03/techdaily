# Proposal: Audit & Cleanup Frontend Test Suite to Enforce Governance Pillars

## Why

The frontend test suite currently encompasses 65 test files and over 460 tests. However, an audit reveals dozens of tests engaged in "Test Theater": asserting Tailwind CSS styling classes (e.g. `expect(el.classes()).toContain('flex')`, `expect(el.classes()).toContain('max-w-5xl')`, `expect(el.classes()).toContain('shrink-0')`). Because Vitest executes inside Happy-DOM (a Node.js DOM simulator without a CSS layout engine), these tests pass 100% while providing zero protection against actual visual breakage (text truncation, element collisions, or viewport overflows). Furthermore, they create brittle coupling to incidental styling changes, slow down execution, and inflate token consumption during maintenance. Cleaning up these tests aligns the codebase directly with **Pillar 3** of `AGENTS.md`.

## What Changes

- **Purge Brittle CSS/Tailwind Class Assertions**:
  - Remove all assertions checking layout and styling utility classes (e.g., `flex`, `flex-col`, `items-center`, `max-w-*`, `w-*`, `z-*`, `gap-*`, `pt-*`, `mt-auto`, `hidden`, `shrink-0`) across `frontend/tests/components/`, `frontend/tests/pages/`, and `frontend/tests/layout/`.
  - Delete tautological tests whose sole assertion is whether an element contains styling classes.
- **Defend Real Behavioral & Data Contracts**:
  - Preserve and strengthen assertions covering form data serialization, event emissions (`update:modelValue`, `change`, `submit`), validation state messages, disabled attributes, Pinia store state transitions, and route middleware navigation guards.
- **Speed Up & Stabilize Test Execution**:
  - Reduce execution time across the Vitest suite and eliminate memory bloat caused by unnecessary DOM class scans.

## Capabilities

### New Capabilities
*None.*

### Modified Capabilities
- `core-platform`: Audit and enforce `Frontend Testing Boundaries & Visual Inspection Standard` across the frontend test suite.

## Impact

- **Test Files**: `frontend/tests/components/*.spec.ts`, `frontend/tests/pages/*.spec.ts`, `frontend/tests/layout/*.spec.ts`.
- **Runtime Code**: Zero changes to production code (`components/`, `pages/`, `stores/`).
- **Breaking Changes**: Zero breaking changes. All passing behavioral tests remain green while brittle styling assertions are eliminated.
