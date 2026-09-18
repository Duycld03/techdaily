# Proposal: Fix Roadmap Track Switcher Dropdown Clipping

## Why
On `/roadmap`, when users click the Track Switcher dropdown button in the header banner to select an active reading track, the popover menu is hard-clipped at the bottom edge of the banner card. Critical navigation options—including the lower half of in-progress documents, the 30-Day Curriculum option, and the "+ Browse Library" link—are rendered invisible and inaccessible.

This clipping occurs because the outer header banner card (`frontend/pages/roadmap.vue`) has CSS `overflow-hidden` applied directly to its container to constrain a decorative blurred circle (`bg-brand-500/10 rounded-full blur-3xl`). Any absolutely positioned child that extends beyond the card's boundary is consequently cut off by the browser's box-model overflow clipping.

Isolating the decorative background blur into a dedicated inset container and elevating the popover's z-index stacking context ensures the track menu renders fully and cleanly over all subsequent page elements.

## What Changes
1. **Isolate Decorative Background Overflow (Advisor Guidance):** In `frontend/pages/roadmap.vue`, replace `overflow-hidden` on the outer banner card element with explicit `overflow-visible z-20`, and encapsulate the decorative background glow within a dedicated inset container (`<div class="absolute inset-0 rounded-3xl overflow-hidden pointer-events-none">`).
2. **Elevate Stacking Context for Track Menu:** Ensure the track menu dropdown anchor and popover retain elevated z-index layering (`relative z-30` / `z-50`) so the popover floats cleanly over the below-the-fold view switcher tabs, milestone timeline, and mindmap canvas.
3. **Responsive Viewport Containment:** Ensure the popover enforces responsive max-height and internal scrolling (`max-h-[calc(100vh-14rem)] overflow-y-auto`) so it never extends beyond the browser viewport on compact or mobile screens.

## Capabilities

### Modified Capabilities
- `roadmap`: Extends the `Active Track Synchronization & Switcher` requirement with a scenario ensuring unclipped dropdown popover rendering and full visibility of all track options and library navigation bridges.

## Impact
- **Frontend Codebase:** `frontend/pages/roadmap.vue` and `frontend/tests/pages/roadmap.spec.ts`.
- **Zero VPS Overhead Invariant:** 100% layout and CSS styling refinement; 0 backend modifications, 0 database queries.
