# Design

## Context

See `proposal.md` for background motivation.
Currently in `frontend/components/dashboard/HomeBentoDashboard.vue`, the itinerary strip pills inside Card B use:
```vue
<div class="px-2.5 py-1.5 rounded-xl bg-slate-100/80 dark:bg-canvas-elevated/60 border border-slate-200/60 dark:border-white/[0.04] flex items-center gap-2 text-xs text-slate-600 dark:text-slate-300 min-w-0">
```
In `frontend/pages/index.vue`, the lifecycle hook checks:
```ts
onMounted(async () => {
  if (!focusStore.data) {
    await focusStore.fetchTodayFocus({ locale: locale.value })
  }
})
```
When a user navigates between `/` and other routes (e.g. `/today`, `/notes`, `/read`), or uses browser history buttons (bfcache / popstate), `focusStore.data` remains in memory, preventing revalidation. Furthermore, if theme classes experience a momentary race or delay during history restoration, `bg-slate-100/80` composites over dark backgrounds to form a `#c4c7ca` light-gray box, blending directly with `dark:text-slate-300` (`#cbd5e1`) and rendering the text invisible.

## Goals / Non-Goals

**Goals:**
- **Guaranteed Contrast Invariance:** Enforce bulletproof contrast tokens on Card B itinerary pills (`dark:bg-white/[0.04] dark:border-white/[0.06] dark:text-slate-200` in dark mode, and `bg-slate-100 text-slate-700 border-slate-200/80` in light mode), completely eliminating washed-out light-gray boxes across all browser states, hydration phases, and bfcache restorations.
- **Automatic History Revalidation:** Revalidate today's focus data when the dashboard route is activated or restored from browser history (`popstate` / `pageshow`), ensuring drill submission state and points badges remain synchronized with actual server progress.
- **Zero Layout Shifts:** Background revalidation must update existing DOM nodes in place without flashing full-page spinner overlays or resetting scroll positions.

**Non-Goals:**
- No backend API or database changes (existing `GET /api/v1/curriculum/today/focus` already returns complete, authoritative state).
- No modification to other dashboard cards or layout archetype wrappers.

## Decisions

### 1. Robust Theme Tokens for Itinerary Pills
- **Decision:** Replace `bg-slate-100/80 dark:bg-canvas-elevated/60 text-slate-600 dark:text-slate-300` with `bg-slate-100 dark:bg-white/[0.04] text-slate-700 dark:text-slate-200 border-slate-200/80 dark:border-white/[0.06]`.
- **Rationale:** `dark:bg-white/[0.04]` applies a subtle 4% white alpha layer over the dark card background without relying on named color opacity modifiers. Combined with `dark:text-slate-200`, it guarantees a high contrast ratio ($> 8:1$) that remains legible even if ambient lighting or display properties fluctuate.
- **Alternatives Considered:** Using opaque `#18181b` for the pill background was considered, but `dark:bg-white/[0.04]` matches the Showcase Design System glass aesthetics used throughout other elevation cards.

### 2. Dual-Mode Page Revalidation (Initial vs Background)
- **Decision:** In `frontend/pages/index.vue`, structure the data fetch lifecycle as:
  1. If `!focusStore.data`, await initial fetch while showing the loading spinner.
  2. If `focusStore.data` is already present, trigger `focusStore.fetchTodayFocus({ locale: locale.value })` in the background without setting full-page loading indicators.
  3. Listen to window `pageshow` and `popstate` events using `@vueuse/core` (`useEventListener`) to detect bfcache restoration and trigger background revalidation.
- **Rationale:** Prevents annoying full-page loading flashes when navigating back to `/` while ensuring that changes made on other routes (e.g. submitting a drill or reading slices) immediately sync to Card B and the streak tracker.

### 3. Immediate Reactive Drill Sync in HomeBentoDashboard
- **Decision:** In `HomeBentoDashboard.vue`, ensure computed properties (`isDrillSubmitted`, `isDrillPassed`, `isDrillFailed`, `drillScore`) reactively derive from `focusStore.data?.drill` without intermediate caching.
- **Rationale:** When `focusStore.data` updates from background revalidation, the computed properties automatically trigger Vue 3 reactivity updates, flipping the badge from pending `+10 Points` to completed `✓ Completed: 10/10` or `✗ Needs Review: 0/10` seamlessly.

## Risks / Trade-offs

- **Risk:** Rapid forward/back clicking could trigger concurrent API requests.
- **Mitigation:** The HTTP client already deduplicates or cancels redundant requests, and `fetchTodayFocus` in `useDailyFocusStore` handles state assignment atomically.
