# Tasks: Fix Roadmap Track Switcher Dropdown Clipping

## 1. Layout & Overflow Refactoring

- [x] 1.1 In `frontend/pages/roadmap.vue`, replace `overflow-hidden` on the outer banner card container with explicit `overflow-visible z-20` and wrap the decorative blurred background glow in a dedicated inset container (`<div class="absolute inset-0 rounded-3xl overflow-hidden pointer-events-none">`).
- [x] 1.2 In `frontend/pages/roadmap.vue`, ensure the track menu anchor is positioned with `relative z-30` and the popover menu retains `z-50` so it cleanly floats above the below-the-fold view switcher tabs and mindmap canvas.
- [x] 1.3 In `frontend/pages/roadmap.vue`, add responsive viewport height constraints (`max-h-[calc(100vh-14rem)] overflow-y-auto`) to the track popover menu container to prevent overflow on compact and mobile screens.

## 2. Automated Tests & Verification

- [x] 2.1 In `frontend/tests/pages/roadmap.spec.ts`, add unit test assertions verifying that the track switcher popover renders all in-progress books, the 30-Day Curriculum option, and the browse library link without parent container overflow clipping.
- [x] 2.2 Run `npm --prefix frontend test -- tests/pages/roadmap.spec.ts` to verify all roadmap page tests pass.
- [x] 2.3 Run full test suites `npm --prefix frontend test` and `dotnet test backend/tests/TechDaily.Tests` to verify zero regressions.
- [x] 2.4 Run `openspec validate roadmap-track-dropdown-clipping-fix --strict` to confirm 100% specification compliance.
