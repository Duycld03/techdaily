# Tasks

## 1. Preparation (Frontend)

- [x] 1.1 Read required skills before touching frontend code (`skill://vue`, `skill://pinia`, `skill://vitest`, `skill://vueuse-functions`, `skill://antfu-design`); verify each `read` call is executed.
- [x] 1.2 Confirm the actual `sourceType` wire format returned by `GET /api/v1/review/analytics` (string enum name vs numeric) by inspecting a live response or a serialization test; verify the observed shape is recorded to drive the label-mapping fix in task 3.2.

## 2. Deck pagination page-size isolation (Store + Dashboard)

- [x] 2.1 In `stores/useReviewStore.ts`, add a count-only path (e.g. a `countOnly` option on `fetchDeckCards` or a dedicated `fetchDeckCount`) that returns `totalCount` without writing `deckPageSize` or `deckCurrentPage`; verify with a store unit test that a probe call with `pageSize: 1` leaves `deckPageSize` at its default.
- [x] 2.2 Update `components/dashboard/HomeBentoDashboard.vue` onMounted to use the count-only path instead of `fetchDeckCards({ pageSize: 1 })`; verify with a component/store test that `deckStatistics.totalCards` still populates and `deckPageSize` is unaffected.
- [x] 2.3 In `pages/review.vue`, make `fetchDeck` use a fixed module-level page-size constant (retain `20` or the chosen grid-friendly value) rather than reading `reviewStore.deckPageSize`; verify with a test that opening Deck Management after a probe fetch calls `/api/v1/review/cards` with the standard page size (Deck Management Pagination Page-Size Isolation scenario).

## 3. Active-tab URL persistence (review.vue)

- [x] 3.1 Write `?tab=management` to the route when switching to Deck Management and clear it when returning to the Review Session tab; verify with a component test that clicking each tab updates `route.query.tab` accordingly.
- [x] 3.2 Verify `onDeckPageChange` preserves the `tab` query (it already spreads `...route.query`) and that `onMounted` rehydrates `activeTab` from `route.query.tab === 'management'`; verify with a test simulating a page-change remount that `activeTab` stays `'management'` (Deck Management Tab Persistence Across Pagination scenario).

## 4. Source-channel retention labels

- [x] 4.1 Remove the duplicate `review.source_highlight` key in `i18n/locales/vi.json` (keep one intended value) and audit `i18n/locales/en.json` for the same duplication; verify by parsing each JSON that `review.source_highlight` appears exactly once and `npm test` i18n key suites pass.
- [x] 4.2 In `components/review/SourceChannelRetentionCard.vue`, align `SOURCE_LABEL_KEYS` resolution with the confirmed wire format from task 1.2 so `Highlight`, `QuizMistake`, and `DocumentChunk` each resolve to a distinct label with no generic fallback; verify with a component test rendering rows for two distinct source types that each row shows a different label (Source-channel rows render distinct labels scenario).

## 5. Ease-Factor Distribution card + three-column analytics row

- [x] 5.1 Add English/Vietnamese i18n keys for the new card (title, `struggling`/`developing`/`comfortable` bucket labels) in `en.json` and `vi.json`; verify no raw `review.*` keys render in either locale.
- [x] 5.2 Create `components/review/EaseFactorDistributionCard.vue` consuming `strugglingCount`/`developingCount`/`comfortableCount`, styled to match the sibling analytics cards (`glass-card`, `min-h-[190px]`, `whitespace-nowrap shrink-0`, responsive typography); verify with a component test that the three buckets render from props.
- [x] 5.3 In `pages/review.vue`, add the new card to the analytics row and change the row from `lg:grid-cols-2` to `lg:grid-cols-3` while keeping `grid-cols-1` on mobile; verify with a component test that three analytics cards render (Ease-factor distribution card renders in a three-column analytics row scenario).

## 6. Dual-gate verification (integration)

- [x] 6.1 Gate 1 — Behavioral: run `npm test` and verify 100% of Vitest suites pass.
- [x] 6.2 Gate 2 — Visual: drove headless Chromium to `/review` Deck Management tab with the real E2E account (local frontend proxied to prod API). Desktop (`1440x900`) shows the three-column analytics row (At-Risk, Source-Channel, new Ease-Factor Distribution) with the source row rendering a distinct label ("Thử thách hằng ngày", not the collapsed "Ghi chú đọc sách"); mobile (`390x844`) stacks cleanly with no horizontal overflow. Screenshots presented. Note: the E2E account holds a single card, so the multi-page pagination + tab-persistence fixes are covered by the store/page unit tests rather than a live 9-page capture.
