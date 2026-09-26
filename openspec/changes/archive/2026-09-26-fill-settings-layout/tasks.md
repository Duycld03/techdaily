# Tasks

## 1. Frontend layout fill (`frontend/pages/settings.vue`)

- [x] 1.1 Change the page wrapper (currently `min-h-[calc(100dvh-3.5rem)] sm:min-h-[calc(100dvh-3.75rem)]` on a plain block) to a full-height flex column (`h-full flex flex-col`, keeping padding + `bg`), so it fills the app shell's `<main>` region. Verify: the wrapper's computed height equals the `<main>` content height (no reliance on `min-h` calc).
- [x] 1.2 Replace the `max-w-5xl mx-auto` container with a full-width fluid stretch container (`w-full flex-1 flex flex-col min-h-0`), so `MasterDetailLayout`'s existing `flex-1` stretches the card to fill both width and height. Verify: on 1080p the card spans the full content region with no `> 400px` side margin and no empty vertical void below it.
- [x] 1.3 Preserve the inner content-panel width caps and top-left alignment (Decision A: keep `max-w-3xl`/`max-w-2xl` on the tab content wrappers; do NOT stretch form fields edge-to-edge). Verify: content stays anchored top-left and the remaining empty space sits inside the card, not outside it.
- [x] 1.4 Confirm `frontend/components/layout/MasterDetailLayout.vue` needs no change (its `flex-1`/`min-h-0`/`overflow-y-auto` already provide the stretch + single scroll region). Verify: only `#content` scrolls; there is no double scrollbar.

## 2. Verification (Dual-Gate per AGENTS.md Pillars 3 & 5)

- [x] 2.1 Gate 1 — Behavioral data contract: run `npm test` in `frontend/` and verify all Vitest suites pass 100% (including `tests/pages/settings.spec.ts`); the layout change must not alter form payloads, validation, auth routing, or error states.
- [x] 2.2 Gate 2 — Visual integrity: drive headless Chromium, authenticate, open `/settings`, and capture screenshots at Desktop 1080p (`1920x1080`) and Mobile (`390x844`) in both `en` and `vi`. Verify and present: card fills the region (no bottom/side void) on desktop, content anchored top-left, no double scrollbar, and mobile stacks with no regression or clipping.

## 3. Full-bleed refinement (user feedback)

- [x] 3.1 Add an additive `flush` prop to `MasterDetailLayout.vue` that drops the `glass-card` chrome (rounded/border/shadow/blur) and paints the base canvas, defaulting to `false` so the showcase and other consumers keep the card look. Verify: the flush root computes `border-radius: 0` and the default variant is unchanged (existing `LayoutArchetypes`/`AppChrome` component tests still pass).
- [x] 3.2 In `settings.vue`, remove the outer wrapper padding (`p-*`) and pass `flush` so the surface fills all four edges. Verify: no rounded corners or outer gap on desktop, and the surface stays visually separated from the global `AppSidebar` via elevation contrast + the sidebar's `border-r`.
- [x] 3.3 Replace the duplicated gear icon on the General sub-nav tab with `SlidersHorizontal` so it is distinct from the page header's gear. Verify (mobile 390px): the header gear and the General tab icon are no longer the same icon.
- [x] 3.4 Re-run both gates after the refinement. Verify: `npm test` still 485/485 green, and headless screenshots (Desktop 1080p + Mobile 390px, dark) confirm full-bleed fill, sidebar separation, and no duplicate icon.
