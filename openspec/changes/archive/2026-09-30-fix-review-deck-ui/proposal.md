# Proposal

## Why

The `/review` Deck Management tab has three defects that break core browsing and make retention analytics unreadable:

1. Opening the dashboard (`/`) first pollutes the shared review store so the deck later renders **one card per page across nine pages** instead of a normal page size.
2. Because the page is keyed by `route.fullPath`, every pagination click remounts `review.vue` and silently throws the user back to the **"Ôn tập hôm nay" (Review Session)** tab.
3. The **"Ghi Nhớ Theo Nguồn" (Source-Channel Retention)** card renders two rows with an identical label ("Ghi chú đọc sách"), so the user cannot tell the recall channels apart, and on wide desktop viewports the two-card analytics row stretches into large empty gaps.

## What Changes

- **Fix deck page-size pollution**: the dashboard count probe (`HomeBentoDashboard.vue` calling `fetchDeckCards({ pageSize: 1 })`) MUST NOT overwrite the shared `deckPageSize` used by the Deck Management list. The Deck Management list SHALL always paginate at its own fixed page size regardless of prior lightweight fetches.
- **Persist the active tab across pagination**: switching to Deck Management SHALL record the tab in the URL, and paginating SHALL preserve it, so a page change never bounces the user back to the Review Session tab (even though the page remounts on `route.fullPath` change).
- **Fix Source-Channel Retention labels**: remove the duplicate `review.source_highlight` i18n key and ensure each recall source (`Highlight`, `QuizMistake`, `DocumentChunk`) maps to its own distinct label so the two rows are visually distinguishable. Source type values from the analytics API MUST resolve to the correct per-source label rather than falling back to a single generic one.
- **Add an Ease-Factor Distribution card**: introduce a third analytics card that visualizes the already-computed `strugglingCount` / `developingCount` / `comfortableCount` from `GET /api/v1/review/analytics`, and change the analytics row to a three-column layout on large viewports so the cards no longer stretch.

No API surface, DTO, or database schema changes are required; all fixes are frontend behavior plus reuse of existing analytics fields.

## Capabilities

### New Capabilities
<!-- None. This change only modifies existing review behavior. -->

### Modified Capabilities
- `review`: pagination page-size isolation, active-tab persistence across pagination remounts, distinct Source-Channel Retention labels, and a new Ease-Factor Distribution analytics card in a three-column analytics row.
