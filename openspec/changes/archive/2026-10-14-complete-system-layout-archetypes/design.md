# Design: Complete System Layout Archetypes & Purge Test Theater

## Context
See `proposal.md` for background and motivation.

During the audit of `AGENTS.md` and `system-layout-archetypes`:
1. Catalog surfaces (`library.vue`, `insights.vue`) were found to already comply with Requirement 5 ("Unboxed Direct-Canvas Catalog Browsing Standard"), which intentionally places content directly on the page canvas (`max-w-7xl mx-auto`) to avoid double-card nesting.
2. In contrast, interactive practice in `quiz.vue` (specifically Arena mode) currently renders inside an unconstrained `max-w-6xl mx-auto` container, missing the `StudioLayout` two-column structure (68% Action Stage + 32% Telemetry Dock) mandated by `system-layout-archetypes` Requirement 9.
3. `LayoutArchetypes.spec.ts` asserts Tailwind CSS class names (`wrapper.classes().toContain('glass-card')`), directly violating Pillar 3 of `AGENTS.md`.

## Goals / Non-Goals

**Goals:**
- Implement `StudioLayout` for `quiz.vue` Arena practice mode, decoupling the active question stage (`#main`) from session telemetry (`#dock`).
- Purge all CSS class assertions from `LayoutArchetypes.spec.ts` and replace them with contract and slot rendering assertions.
- Verify 100% test pass rate in Vitest and execute automated headless browser visual screenshot capture on both Desktop and Mobile.

**Non-Goals:**
- Restructuring the quiz API, quiz generation logic, or store state in `useInterviewQuizStore.ts`.
- Encapsulating catalog browsing pages (`library.vue`, `insights.vue`) into boxed cards, preserving their validated unboxed direct-canvas presentation.

## Decisions

### 1. StudioLayout Integration in Quiz Arena Mode
In `frontend/app/pages/quiz.vue`:
- Keep the top header and tab navigation banner (`generate`, `arena`, `review`, `stats`) clean at the page top.
- When `quizStore.activeTab === 'arena'` and active questions exist:
  - Render `<StudioLayout>`:
    - `#main`: The primary Question Card, Category & Difficulty badges, Multiple-Choice Options, and Submit/Next CTA buttons.
    - `#dock`: The Quiz Session Telemetry Dock, displaying:
      - Question progress tracker (e.g., `Question 3 of 10`)
      - Current Score & Streak indicator
      - Keyboard shortcut cheatsheet (Key `1-4` for options, `Enter` to submit/advance)
- On mobile viewports ($< 1024\text{px}$), `StudioLayout` automatically collapses to a full-width column.

### 2. Purge CSS Class Assertions from Vitest
In `frontend/tests/components/LayoutArchetypes.spec.ts`:
- Remove:
  ```typescript
  expect(wrapper.classes()).toContain('glass-card')
  expect(wrapper.classes()).not.toContain('glass-card')
  expect(wrapper.classes()).toContain('space-y-4')
  ```
- Replace with assertions testing slot rendering, DOM presence of headers/footers, and prop behavior:
  ```typescript
  expect(wrapper.find('header').exists()).toBe(true)
  expect(wrapper.find('footer').exists()).toBe(true)
  ```

### 3. Dual-Gate Verification Invariant
Implementation completion strictly requires:
- **Gate 1:** `npm test` passes 100% with zero regressions.
- **Gate 2:** Headless Chromium automated screenshot capture for Desktop (1440x900) and Mobile (390x844) on `/quiz`.

## Risks / Trade-offs

- **Risk**: Visual regression in Arena mode when answering options are rendered inside `StudioLayout`.
  - *Mitigation*: The `max-w-2xl` width bound inside `StudioLayout` matches the optimal reading/choice line length and prevents horizontal option stretching on ultra-wide screens.
- **Risk**: Mobile responsive layout collisions.
  - *Mitigation*: The dock elements naturally flow below the main stage on viewports $< 1024\text{px}$.
