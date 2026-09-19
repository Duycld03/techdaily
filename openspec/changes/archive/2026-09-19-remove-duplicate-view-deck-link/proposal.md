# Proposal: Remove Redundant Header View Deck Link from Concentric Metric Card

## Why

In the dashboard's "Ghi Nhớ & Mục Tiêu" (Memory & Goal) metric card (`frontend/components/today/ConcentricMetricCard.vue`), a navigation link to `/review` with text `dashboard.view_deck` ("Xem Bộ Thẻ ↗" / "View Deck ↗") is rendered in the top-right header. Concurrently, the bottom action row also renders a button linking to `/review` which displays `dashboard.view_deck` ("Xem Bộ Thẻ ↗") whenever zero cards are due for review (`dueCards === 0`).

Rendering two identical "Xem Bộ Thẻ ↗" links within the same compact card creates visual noise, redundant touch targets, and visual clutter. Removing the redundant top-right header link establishes a clean, uncluttered card header and consolidates all review deck navigation into the dedicated footer action button.

## What Changes

- **Declutter Card Header in `ConcentricMetricCard.vue`**:
  - Remove the top-right header `<NuxtLink to="/review">` element displaying `dashboard.view_deck`.
  - The card header now exclusively showcases the `BrainCircuit` icon badge, title (`dashboard.metrics_title`), and subtitle (`dashboard.metrics_subtitle`).
- **Consolidate Deck Navigation in Footer Action Banner**:
  - The single, authoritative review deck call-to-action remains in the bottom banner:
    - Displays `dashboard.review_now` ("Ôn Luyện Ngay (Space) ↗") with primary brand styling when `dueCards > 0`.
    - Displays `dashboard.view_deck` ("Xem Bộ Thẻ ↗") when `dueCards === 0`.
- **Component Test Updates**:
  - Update unit tests in `frontend/tests/components/today/ConcentricMetricCard.spec.ts` to assert that only one deck navigation link exists in the card.
- **Zero Breaking Changes**: No backend changes, no API modifications, and no database changes.

## Capabilities

### New Capabilities
<!-- None: uses existing capabilities -->

### Modified Capabilities
- `today`: Update requirements and scenarios for the concentric metric card to specify that review deck navigation is consolidated exclusively in the footer action row without duplicate header links.
