# Design: Remove Redundant Header Deck Link in Concentric Metric Card

## Context

In `frontend/components/today/ConcentricMetricCard.vue`, the "Ghi Nhớ & Mục Tiêu" (Memory & Goal) Bento card provides developers with real-time feedback on daily study minutes and SM-2 spaced repetition retention health.

Currently, the card header contains a `<NuxtLink to="/review">` rendering `dashboard.view_deck` ("Xem Bộ Thẻ ↗" / "View Deck ↗"). Simultaneously, the card's bottom action banner renders another `<NuxtLink to="/review">` button displaying `dashboard.view_deck` whenever zero cards are due for review. This creates duplicate navigation elements on the same card.

## Goals / Non-Goals

**Goals:**
- Eliminate the top-right `<NuxtLink>` from `ConcentricMetricCard.vue` header.
- Maintain the footer action button as the single, authoritative CTA leading to the review deck (`/review`).
- Keep the header visually balanced with the `BrainCircuit` icon badge, card title, and subtitle.
- Verify component tests in `frontend/tests/components/today/ConcentricMetricCard.spec.ts`.

**Non-Goals:**
- Modifying SVG ring rendering, dashoffset computations, or retention math.
- Changing `dueCards` badge logic or `/review` routing.

## Decisions

### Decision 1: Remove Header `<NuxtLink>` Element
In `frontend/components/today/ConcentricMetricCard.vue`, remove lines 69-75 containing:
```html
<NuxtLink
  to="/review"
  class="text-xs font-medium text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white flex items-center gap-1 transition-colors"
>
  <span>{{ $t('dashboard.view_deck') }}</span>
  <ArrowUpRight class="w-3.5 h-3.5" />
</NuxtLink>
```
*Rationale*: Concentrates review deck navigation into the dedicated footer button, matching Bento design standards across TechDaily where primary actions reside in the card footer.

### Decision 2: Retain Responsive Footer Action Banner
The bottom banner contains:
- Left: Flame icon + count of cards due (`dueCards + dashboard.cards_due`).
- Right: Action button with dynamic copy (`dueCards > 0 ? dashboard.review_now : dashboard.view_deck`).
This ensures seamless, uncompromised navigation to `/review` whether cards are due or not.

## Risks / Trade-offs

- **Zero functional risk**: All navigation to `/review` is fully preserved via the footer button.
