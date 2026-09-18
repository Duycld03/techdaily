# Design: Dashboard Top-Aligned Compact Layout

## Context
In `frontend/components/today/TodayBentoDashboard.vue`, the dashboard container and column flexboxes currently use Tailwind `justify-between` alongside `flex-1` on individual cards. On standard laptop viewports with browser toolbars (Image #2), the height naturally matches the content, giving a cohesive appearance. However, on taller screens or full-screen desktop viewports (Image #1), `justify-between` distributes all extra vertical height as large empty voids between cards, pushing the bottom cards to the bottom edge of the screen and tearing the layout apart visually.

## Goals / Non-Goals

**Goals:**
- Replace `justify-between` with `justify-start` and consistent gap spacing (`gap-3.5 sm:gap-4`) across the outer container and both column flexboxes.
- Allow all cards to hug the top of the viewport directly below the Welcome Banner, forming a dense, unified command center cluster.
- Remove unbounded `flex-1` stretching on Card A (Active Reading Slice) and Card B (Scenario Challenge), giving them balanced, natural proportions without empty internal voids.
- Ensure the right column (Concentric Metrics, 7-Day Consistency, Knowledge Radar) stacks snugly with the same gap spacing.

**Non-Goals:**
- Modifying backend APIs, database models, or state stores.
- Changing route definitions or navigation logic.
- Adding new metric cards or altering existing card contents.

## Decisions

### 1. Top-Aligned Flex Hierarchy (`justify-start`)
- **Decision:** Switch from `justify-between` to `justify-start` across the container and column flexboxes:
  - Outer container: `flex flex-col justify-start gap-3.5 sm:gap-4`
  - Grid: `grid grid-cols-1 lg:grid-cols-3 gap-3.5 sm:gap-4 items-start`
  - Left column: `lg:col-span-2 flex flex-col justify-start gap-3.5 sm:gap-4`
  - Right column: `flex flex-col justify-start gap-3.5 sm:gap-4`
- **Rationale:** Top-alignment ensures visual elements anchor to the top of the reading plane. Any surplus viewport height on tall monitors sits cleanly below the dashboard rather than creating artificial chasms between related cards.

### 2. Sizing and Height Distribution
- **Card A & B (Left Column):**
  - Drop `flex-1` in favor of natural content height with `min-h-0` and balanced padding (`p-4 sm:p-5`).
  - Keep footer CTA buttons anchored with `mt-3 pt-3 border-t border-slate-200/80 dark:border-white/[0.08]`.
- **Card C, D & E (Right Column):**
  - Card C (`ConcentricMetricCard`): Sized at ~210px.
  - Card D (7-Day Consistency Matrix): Natural height ~130px.
  - Card E (Knowledge Radar): Natural height ~130px.
  - The total right-column stack height matches the left-column stack (~490px - 510px).

## Risks / Trade-offs

- **Surplus Space on Ultra-Wide/4K Displays:**
  - Extra space will appear below the dashboard cards. This is standard for modern developer tools (e.g. Linear, Raycast, GitHub dashboard) and significantly preferred over stretching cards artificially.
