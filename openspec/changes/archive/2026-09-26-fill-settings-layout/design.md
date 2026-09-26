# Design

## Context

See `proposal.md` — Why. Current state (verified in the running app + source):

- `app.vue` renders every page inside `<main class="flex-1 overflow-y-auto">`, itself inside `min-h-dvh flex flex-col` under a fixed-height `AppHeader`. So `<main>` already has a **resolved** height (viewport − header) and is the scroll container.
- `frontend/pages/settings.vue`: the page wrapper (`L364`) is a plain block using `min-h-[calc(100dvh-3.5rem)]`; its child (`L365`) is `max-w-5xl mx-auto`.
- `MasterDetailLayout.vue` root is `glass-card flex min-h-0 flex-col` with `flex-1` applied when no `maxHeight` prop is passed (settings passes none).

Root cause: `flex-1` on the card is inert because its parent chain (`max-w-5xl` block → `min-h` block) is not a flex column with a resolved height. Result on 1080p: card caps at 1024px wide (side voids) and grows only to content height (bottom void).

## Goals / Non-Goals

**Goals:**
- Card fills the full available content region (height + width); no bottom or side void on 1080p desktop.
- Content anchored top-left; extra sections append downward in the existing scroll region.
- No regression on mobile (`< 768px`), where the rail already stacks above the panel.

**Non-Goals:**
- Restructuring `MasterDetailLayout.vue` internals beyond an additive `flush` variant prop (its `flex-1` stretch mechanism is unchanged).
- Changing settings data, form fields, i18n keys, or behavior.
- Applying the fill fix to other archetypes/pages in this change.

## Decisions

- **Fill via a full-height flex column, not fixed pixel heights.** Make the settings page wrapper `h-full flex flex-col` and its container `w-full flex-1 flex flex-col min-h-0`, letting the card's existing `flex-1` stretch. `h-full` resolves against `<main>`'s already-definite height, so no `calc(100dvh − header)` guесswork is needed.
  - *Alternatives:* (a) keep `min-h-[calc(...)]` + add `flex flex-col` — works but duplicates the header-height constant that `<main>` already encodes; rejected for redundancy. (b) set an explicit `maxHeight` prop on `MasterDetailLayout` — reintroduces a hardcoded height; rejected.
- **Drop `max-w-5xl mx-auto`; go full-width fluid.** The spec's Master-Detail archetype mandates `max-w-7xl`/full-width and `< 400px` margins; `max-w-5xl` (1024px) was the violation. Full-width fluid matches the "fill all empty space" intent.
- **Decision A: keep a readable content max-width inside the panel, left-aligned (chosen over Decision B: stretch fields edge-to-edge).** Verified via headless preview: stretching 2-column form fields to ~1900px harms readability; anchoring content top-left with the empty area inside the filled card satisfies the request and reads better. Recorded here so implementation does not remove the inner content caps.
- **Full-bleed "flush" surface (user refinement — supersedes the glass-card look).** Add an additive `flush` prop to `MasterDetailLayout`: when set it drops the `glass-card` chrome (rounded corners, border, shadow, blur) and paints the base canvas (`bg-slate-50 dark:bg-canvas`), so the settings surface fills all four edges once the outer page padding is removed. The content-width caps and top-left anchoring from Decision A are retained inside the panel. Other consumers (e.g. the layout showcase) keep the default card look because the prop defaults to `false`.
  - *Separation from the global sidebar:* the flush surface paints the base canvas (`rgb(9,9,11)` dark) while `AppSidebar` is elevated (`canvas-subtle`, `rgb(18,18,21)`) and carries its own `border-r`; the elevation contrast plus that border delineate the two navigation columns without reintroducing padding.
  - *Icon de-duplication:* the General sub-nav tab reused the page header's gear (`Settings`) icon, reading as a duplicated gear on mobile; it now uses `SlidersHorizontal` (preferences) so the header and tab icons are visually distinct.

## Risks / Trade-offs

- [Sparse content leaves visible empty space inside the now-full-height card] → Expected and accepted: the void moves from raw canvas outside the card to inside a styled surface, which is the requested behavior ("append downward"). No mitigation needed.
- [Double scrollbar if the wrapper both fills and the panel scrolls] → Avoided: wrapper is fixed to the region height (`h-full`, no own overflow) and only `#content` (`overflow-y-auto`) scrolls; verified in preview.
- [Mobile regression] → Low; the mobile stack (`flex-col`, nav row on top) is unaffected by the parent becoming a flex column. Covered by the Mobile 390px visual gate.
- [Full-bleed surface sits directly against the global `AppSidebar`, risking two indistinguishable nav columns] → Mitigated by background-elevation contrast (base canvas vs `canvas-subtle` sidebar) plus the sidebar's `border-r`; verified visually in dark mode.

## Migration Plan

Pure CSS-utility edit in one page; no data or API migration. Rollback = revert the utility-class change on the two lines in `settings.vue`.
