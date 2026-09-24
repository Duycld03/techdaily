# Technical Design: Compact AppTimePicker Dimensions

## Context

`frontend/components/common/AppTimePicker.vue` is a custom floating popover component used for selecting reminder and streak alert times in `/settings` as well as interactive UI demos.

In the current implementation, the popover container uses `h-40 flex-1` across its three columns (Hours, Minutes, Period). In flex contexts without strict `max-h` bounding, this causes vertical height inflation (>350px), displaying up to 10 hour numbers simultaneously and colliding with the bottom of the screen on 1080p desktop viewports.

## Goals / Non-Goals

**Goals:**
- Bound total popover height to $\le 220\text{px}$ (target ~210px), reducing height by ~40%.
- Constrain number column tracks to `h-28 max-h-28` ($112\text{px}$ / $7\text{rem}$), displaying $\sim 3.5$ to $4$ items at a time with smooth mouse and touch scrolling.
- Balance the AM/PM Period column to `h-28` with two symmetrical buttons ($\sim 50\text{px}$ each).
- Update `updateFloatingPosition()` collision detection threshold to `spaceBelow < 230` to smoothly flip above triggers when approaching the viewport bottom.

**Non-Goals:**
- Modifying time conversion logic (`to12h`, `to24h`), minute increments (5-minute intervals), or outer trigger styling.
- Introducing third-party timepicker dependencies.

## Decisions

### 1. Fixed Height Wells (`h-28 max-h-28`) & Removal of `flex-1`
- **Current**: Column tracks use `h-40 overflow-y-auto ... flex-1`, and their parent columns use `flex flex-col space-y-1.5`. The `flex-1` combined with flexible parent height causes vertical stretching.
- **Decision**: Replace `h-40 flex-1` with explicit `h-28 max-h-28 overflow-y-auto scrollbar-none`. At $112\text{px}$, each $28\text{px}$ button row allows 4 visible rows (or 3 full rows with half rows at top/bottom indicating scroll affordance), matching macOS/iOS compact wheel density.

### 2. Balanced AM/PM Period Column
- **Current**: Period container uses `h-40 flex flex-col justify-center gap-2 p-1.5 ... flex-1` with `max-h-16` on buttons.
- **Decision**: Standardize Period container to `h-28 flex flex-col justify-between gap-1.5 p-1 rounded-xl`. Each button gets `flex-1 flex items-center justify-center rounded-lg text-xs font-bold` (resulting in exactly $\sim 50\text{px}$ per button), maintaining perfect alignment with the adjacent number wells.

### 3. Tightened Popover Shell Spacing
- Container padding: `p-3 space-y-2.5` (reduced from `p-3.5 space-y-3`).
- Top header: `pb-1.5` (reduced from `pb-2`).
- Bottom action button: `h-7.5` ($30\text{px}$) with `text-xs font-semibold` (reduced from `h-8`).
- Result: Total popover height measures $\sim 210\text{px}$.

### 4. Smart Collision Detection Flip
- Update `const placeAbove = spaceBelow < 230 && spaceAbove > spaceBelow`.
- With total height ~210px + 6px gap = 216px, a 230px clearance threshold guarantees the popover will flip cleanly above without any bottom edge cut-off or layout jump.

## Risks / Trade-offs

- **Risk**: Fewer numbers are visible simultaneously without scrolling.
  - **Mitigation**: Scrolling is effortless via mouse wheel or swipe; 12 hours and 12 minute increments require minimal scrubbing, and the live preview badge at the top guarantees immediate feedback.
