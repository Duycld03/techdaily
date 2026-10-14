# Proposal: Complete System Layout Archetypes & Purge Test Theater

## Why
A comprehensive design system and test audit against `AGENTS.md` and `system-layout-archetypes` identified two critical gaps:
1. `system-layout-archetypes` Requirement 9 specifies that interactive practice surfaces—specifically `quiz.vue` in Arena mode—must implement the `StudioLayout` archetype (`StudioLayout.vue`) to eliminate the "card in a void" problem on 1080p desktop displays ($W \ge 1280\text{px}$). Currently, `quiz.vue` uses an ad-hoc `max-w-6xl` container without the two-column stage and telemetry dock architecture.
2. `frontend/tests/components/LayoutArchetypes.spec.ts` contains forbidden CSS/Tailwind class assertions (`expect(wrapper.classes()).toContain('glass-card')`), directly violating `AGENTS.md` Pillar 3 ("Strict Prohibition of CSS/Tailwind Class Assertions in Vitest").

## What Changes
- Refactor `quiz.vue` in Arena mode to utilize the `StudioLayout` archetype (`StudioLayout.vue`), providing a primary practice stage (`#main`) and telemetry dock (`#dock`) for timer, streak, and hotkeys on desktop viewports while preserving single-column responsive flow on mobile.
- Clean up `frontend/tests/components/LayoutArchetypes.spec.ts` by replacing fragile CSS class assertions (`classes().toContain('glass-card')`, `classes().toContain('space-y-4')`) with data contract, prop validation, and structural slot tests.
- Re-verify layout integrity via the mandatory Dual-Gate Verification protocol (`npm test` and headless browser visual inspection on desktop 1080p and mobile 390px).

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `system-layout-archetypes`: Ensures `quiz.vue` Arena practice surface strictly conforms to the `StudioLayout` archetype geometry.
- `core-platform`: Enforces Pillar 3 testing boundaries by eliminating CSS class assertions from Vitest suites.

## Impact
- **Frontend UI**: `quiz.vue` Arena mode gains a dedicated telemetry dock on desktop (1080p) matching `review.vue` and `today.vue`, while mobile remains 100% full-width. Catalog surfaces (`library.vue`, `insights.vue`) remain direct-canvas without double-card nesting.
- **Frontend Tests**: Eliminates flaky class assertions from Vitest while maintaining 100% test pass rate.
- **Breaking Changes**: None.
