# Design

## Context

See proposal.md - Why. Three concrete root causes were confirmed by reading the code:

1. **Page-size pollution** — `components/dashboard/HomeBentoDashboard.vue` (onMounted) calls `reviewStore.fetchDeckCards({ pageSize: 1 })` purely to read `totalCount`. `fetchDeckCards` (`stores/useReviewStore.ts`) writes the response's `pageSize` back into the shared `deckPageSize` ref. When `/review` later mounts, `fetchDeck()` reads `pageSize: reviewStore.deckPageSize` (now `1`), so the deck renders one card per page and `totalPages = ceil(total / 1)`.
2. **Tab reset on pagination** — `app.vue` renders `<NuxtPage :page-key="route => route.fullPath" />`. `onDeckPageChange` in `pages/review.vue` calls `router.replace({ query: { ...route.query, page } })`, which changes `fullPath`, changes the page key, and **fully remounts** `review.vue`. `activeTab` is a local `ref('session')`, so it resets to the Review Session tab. `onMounted` only restores management when `route.query.tab === 'management'`, but the tab button never writes `tab` into the URL, so it never restores.
3. **Collapsed source labels** — `frontend/i18n/locales/vi.json` defines `review.source_highlight` twice (`"Ghi chú"` then `"Ghi chú đọc sách"`; last wins). `SourceChannelRetentionCard.vue` maps `SOURCE_LABEL_KEYS[s.sourceType] ?? 'review.source_highlight'`; when `sourceType` does not match the string keys, every row falls back to `source_highlight`, so two distinct source types render the same label.

The backend `GET /api/v1/review/analytics` already returns `strugglingCount` / `developingCount` / `comfortableCount` (`GetReviewAnalyticsHandler`), currently unused by the frontend.

## Goals / Non-Goals

**Goals:**
- Deck Management list paginates at a fixed page size independent of any other fetch.
- Active tab survives pagination-driven remounts via URL state.
- Each source-channel row shows a distinct, correct label.
- A third Ease-Factor Distribution card fills the analytics row into three columns on large viewports.

**Non-Goals:**
- No backend, DTO, or database changes. Difficulty distribution data already exists.
- No change to the review-session player, grading, SM-2, or edit/reset/delete flows.
- No redesign of pagination controls (`BasePagination.vue`) beyond fixing inputs.

## Decisions

### 1. Isolate the deck page size from probe fetches
`fetchDeckCards` must stop mutating shared paging state on a probe call. Preferred approach: introduce a lightweight count path that does not write `deckPageSize`/`deckCurrentPage` (e.g. an explicit `countOnly` option, or a dedicated `fetchDeckCount`), and have the dashboard use it. The Deck Management list uses a module-level constant page size (retain the existing `20`, or the grid-friendly value chosen during apply) rather than reading a value another view can overwrite.
- Alternative rejected: resetting `deckPageSize` on review mount — fragile, still races other callers.

### 2. Encode the active tab in the URL
Write `?tab=management` when the user switches to Deck Management (and clear it on the session tab). Because `onDeckPageChange` already spreads `...route.query`, the tab persists across page changes; the existing `onMounted` restore (`route.query.tab === 'management'`) then rehydrates the tab after remount. This works with the current `page-key = route.fullPath` without touching `app.vue`.
- Alternative rejected: removing the `page-key` or switching to `<keep-alive>` — broader blast radius across all pages.

### 3. Fix source-channel labels
Remove the duplicate `review.source_highlight` key in `vi.json` (and audit `en.json`), keeping one intended value per source. Ensure `SourceChannelRetentionCard.vue` resolves the API `sourceType` to the correct key for all three types. During apply, verify the actual JSON shape of `sourceType` in the analytics payload (string enum name vs numeric) and align the mapping so no recognized type hits the generic fallback.

### 4. Ease-Factor Distribution card + three-column row
Add a presentational card consuming `analytics.strugglingCount/developingCount/comfortableCount`, mirroring the existing analytics card styling (`glass-card`, `min-h-[190px]`, bilingual labels, `whitespace-nowrap shrink-0`). Change the analytics row in `review.vue` from `lg:grid-cols-2` to `lg:grid-cols-3`; keep `grid-cols-1` on mobile.

## Risks / Trade-offs

- **Page-size constant vs grid columns**: if the list uses a 3-column grid, a page size not divisible by 3 leaves a ragged final row — cosmetic only; choose a value like 12 or keep 20 per existing scenarios.
- **Enum serialization uncertainty**: the exact `sourceType` wire format must be confirmed at apply time; the label fix depends on it. Low risk, single-file mapping.
- **URL tab param**: adding `?tab=management` changes shareable URLs; the existing spec already references `?tab=deck`-style params, so this aligns rather than conflicts. Ensure the session tab clears the param to avoid stale deep links.
