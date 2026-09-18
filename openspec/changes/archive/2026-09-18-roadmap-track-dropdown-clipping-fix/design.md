# Design: Fix Roadmap Track Switcher Dropdown Clipping

## Context
On the `/roadmap` page (`frontend/pages/roadmap.vue`), the header banner card currently combines two conflicting CSS properties:
```html
<div
  class="p-4 sm:p-8 rounded-3xl ... relative overflow-hidden transition-all duration-300"
>
  <div class="absolute -right-10 -bottom-10 w-64 h-64 bg-brand-500/10 rounded-full blur-3xl pointer-events-none"></div>

  <div class="relative z-10 flex flex-col md:flex-row md:items-center justify-between gap-5 sm:gap-6">
    ...
    <div ref="trackMenuRef" class="relative">
      <button ...>...</button>
      <div v-if="isTrackMenuOpen" class="absolute left-0 top-full mt-2 w-72 sm:w-84 ... z-50 ...">
        ...
      </div>
    </div>
  </div>
</div>
```
Because the outer banner card has `overflow-hidden`, any child element with `position: absolute` that extends beyond the card's bottom border is clipped by CSS overflow rules. The track switcher popover contains in-progress documents, the 30-Day Curriculum option, and a "+ Browse Library" action link; when opened, the lower half of the popover is sliced off right at the card's boundary.

---

## Goals & Non-Goals

### Goals
- **Eliminate Dropdown Clipping:** Ensure the Track Switcher popover renders fully without being truncated by any parent card boundaries.
- **Preserve Decorative Aesthetics:** Maintain the rounded ambient blur effect (`bg-brand-500/10 blur-3xl`) within the card's rounded corners.
- **Robust Stacking Hierarchy:** Guarantee the dropdown floats cleanly above all subsequent roadmap controls (`RoadmapViewSwitcher`, timeline milestones, and `RoadmapMindmapCanvas`).
- **Responsive Viewport Safety:** Constrain the popover's maximum height on mobile/compact viewports with internal scrolling.

### Non-Goals
- Modifying track switcher business logic, Pinia store actions, or backend APIs.
- Altering the popover's internal typography or bilingual translations.

---

## Decisions & Implementation Architecture

### 1. Isolated Background Overflow Containment
The outer banner container only had `overflow-hidden` to prevent the `-right-10 -bottom-10` blurred decorative glow from bleeding outside its rounded corners.

**Decision (Advisor-Aligned Architecture):** Replace `overflow-hidden` on the outer banner card with explicit `overflow-visible z-20`, and encapsulate the decorative glow inside an isolated `absolute inset-0 rounded-3xl overflow-hidden pointer-events-none` container:
```html
<!-- Outer Banner Card (Explicitly overflow-visible z-20 so popovers float over below-the-fold canvas) -->
<div
  class="p-4 sm:p-8 rounded-3xl bg-gradient-to-br from-indigo-50/80 via-white to-brand-50/50 dark:from-slate-900 dark:via-slate-900 dark:to-brand-950 border border-slate-200/90 dark:border-slate-800 text-slate-900 dark:text-white shadow-md dark:shadow-xl relative overflow-visible z-20 transition-all duration-300"
>
  <!-- Isolated decorative background with overflow containment -->
  <div class="absolute inset-0 rounded-3xl overflow-hidden pointer-events-none">
    <div class="absolute -right-10 -bottom-10 w-64 h-64 bg-brand-500/10 rounded-full blur-3xl"></div>
  </div>

  <!-- Content Row with relative positioning -->
  <div class="relative z-10 flex flex-col md:flex-row md:items-center justify-between gap-5 sm:gap-6">
    ...
```
This preserves 100% of the rounded ambient glow visual effect while completely freeing the popover from box-model clipping.

### 2. Stacking Context Elevation
To prevent layering collisions between the absolute popover and subsequent elements on the page (such as the segmented `RoadmapViewSwitcher` control or the SVG mindmap canvas):
- Content row: `relative z-20`
- Track menu anchor: `relative z-30`
- Dropdown popover: `absolute left-0 top-full mt-2 ... z-50`

### 3. Max-Height & Viewport Safety
Ensure the popover enforces responsive max-height and internal scrolling:
```html
<div
  v-if="isTrackMenuOpen"
  data-testid="track-menu-popover"
  class="absolute left-0 top-full mt-2 w-72 sm:w-84 max-w-[calc(100vw-2rem)] max-h-[calc(100vh-14rem)] overflow-y-auto rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-2xl p-2 z-50 animate-in fade-in zoom-in-95 duration-150 space-y-1"
>
```

---

## Risks & Trade-offs

| Risk | Impact | Mitigation |
|---|---|---|
| **Bleeding Background Glow:** Removing `overflow-hidden` could let the blur circle bleed into neighboring elements. | Low | The `inset-0 rounded-3xl overflow-hidden` wrapper guarantees the glow circle is strictly clipped to the card's exact border radius. |
| **Z-index Collisions:** Popover overlapping interactive buttons below. | Low | `z-50` with click-outside dismissal (`onDocumentClick`) ensures clean interaction and immediate dismissal when clicking elsewhere. |
