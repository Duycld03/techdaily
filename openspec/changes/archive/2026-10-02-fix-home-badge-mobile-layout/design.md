# Design

## Context

See `proposal.md` for motivation.

The home dashboard orientation banner in `frontend/components/dashboard/HomeBentoDashboard.vue` contains an orientation header with an "Executive Cockpit" badge and associated metadata (`targetRole` and `sliceBadgeText`):

```html
<div class="flex items-center gap-2">
  <span class="rounded-full bg-brand-500/15 px-2.5 py-0.5 text-xs font-semibold text-brand-600 dark:text-brand-300">
    Executive Cockpit
  </span>
  <span class="text-xs text-slate-500 dark:text-slate-400">
    {{ targetRole }} • {{ sliceBadgeText }}
  </span>
</div>
```

On mobile viewports ($< 640\text{px}$, especially $360\text{px} - 390\text{px}$), the flex container compresses the children when the available width is constrained. Because the badge lacks `whitespace-nowrap shrink-0`, the browser wraps "Executive Cockpit" into two stacked lines ("Executive" on line 1, "Cockpit" on line 2). Due to `items-center` and `rounded-full`, the pill distorts vertically with asymmetric trailing whitespace.

The identical pattern appears in the design system showcase demo in `frontend/components/showcase/LayoutArchetypesShowcase.vue` (line 460).

## Goals / Non-Goals

**Goals:**
- Guarantee the "Executive Cockpit" status badge in `HomeBentoDashboard.vue` renders strictly on a single horizontal line across all viewport widths ($320\text{px} - 1440\text{px}$).
- Ensure the badge and adjacent metadata (`{{ targetRole }} • {{ sliceBadgeText }}`) respond gracefully to narrow mobile viewports by enabling flexible wrapping (`flex-wrap`) and responsive gaps (`gap-1.5 sm:gap-2`).
- Synchronize `LayoutArchetypesShowcase.vue` to maintain consistency between design system documentation and production code.
- Comply fully with `AGENTS.md` Pillar 2 (Responsive Typography Standard & Bilingual Layout Invariant) and Pillar 3 (Dual-Gate Verification: Vitest data contract tests + automated Chromium visual inspection at Mobile 390px and Desktop 1440px).

**Non-Goals:**
- Altering the 2:1 asymmetric Bento Grid column ratios or card distributions in `BentoDashboardLayout.vue`.
- Changing backend Minimal API endpoints, DTOs, or database schema.
- Refactoring the primary CTA button or greeting typography in the orientation banner.

## Decisions

### Decision 1: Enforce `whitespace-nowrap shrink-0` on Badge Element

Add `whitespace-nowrap shrink-0` to the badge class list:
```html
<span class="rounded-full bg-brand-500/15 px-2.5 py-0.5 text-xs font-semibold text-brand-600 dark:text-brand-300 whitespace-nowrap shrink-0">
  Executive Cockpit
</span>
```

**Rationale:**
- Directly aligns with all other status badges in `HomeBentoDashboard.vue` (e.g. lines 336, 343, 350, 385), which already declare `whitespace-nowrap shrink-0`.
- Complies with `AGENTS.md` Section 2: *"All action buttons, badges, and card footers MUST use whitespace-nowrap shrink-0 and responsive gap to prevent text wrapping and visual collisions across both locales."*
- Prohibits flex shrink compression from breaking words into multiple lines inside a rounded pill badge.

### Decision 2: Enable `flex-wrap` and Responsive Gaps on Metadata Row

Update the parent container from `flex items-center gap-2` to:
```html
<div class="flex flex-wrap items-center gap-1.5 sm:gap-2 min-w-0">
```

**Rationale:**
- On ultra-narrow screens (e.g. 360px) or with long localized target roles, `flex-wrap` allows the metadata (`Senior Fullstack • Lát cắt 1 / 12`) to drop cleanly to the next line beneath the pill badge rather than forcing text truncation or pushing the container out of bounds.
- `min-w-0` prevents flex item overflow in nested flexbox contexts.

### Decision 3: Harmonize Design System Showcase

Apply the same `whitespace-nowrap shrink-0` classes to the `LayoutArchetypesShowcase.vue:460` demo badge.

**Rationale:**
- Prevents drift between the canonical showcase and production dashboard implementations.

## Risks / Trade-offs

- **Risk:** On extremely narrow mobile screens ($320\text{px} - 360\text{px}$), `flex-wrap` causes the metadata line to wrap below the badge, increasing the orientation banner height by approximately 18px.
  - **Mitigation:** The banner container already uses `flex flex-col sm:flex-row justify-between gap-3`, which natively accommodates vertical expansion on mobile. An 18px height increase for a secondary line is standard mobile UI behavior and strictly superior to a deformed, multi-line pill badge.
