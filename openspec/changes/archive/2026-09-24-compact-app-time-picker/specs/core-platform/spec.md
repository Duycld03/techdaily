# Spec Delta: core-platform

## ADDED Requirements

### Requirement: Compact Dimensions and Viewport Bounds for AppTimePicker Popover
The custom timepicker component (`AppTimePicker.vue`) SHALL enforce compact vertical dimensions conforming to Density 8/10 ergonomics, bounding total popover height to $\le 220\text{px}$ to prevent vertical viewport overflow and excessive column stretching.

1. **Column Height & Scroll Ergonomics**:
   - The Hours list (01 to 12) and Minutes list (00 to 55) containers SHALL enforce a strict height constraint of `h-28 max-h-28` ($112\text{px}$ / $7\text{rem}$) with `overflow-y-auto` and `scrollbar-none`.
   - The scroll tracks SHALL display $\sim 3.5$ to $4$ options simultaneously, allowing rapid wheel and touch scrubbing without dominating the screen fold.
   - The Period container (AM / PM) SHALL match the exact `h-28` ($112\text{px}$) height of adjacent number columns, containing two symmetrically sized buttons ($\sim 50\text{px}$ height each).

2. **Compact Vertical Rhythm**:
   - The popover container padding SHALL standardize to `p-3 space-y-2.5`.
   - The top preview header SHALL standardize to `pb-1.5`, displaying the clock icon, label, and live badge with zero vertical dead space.
   - The bottom action confirmation button SHALL standardize to `h-7.5` ($30\text{px}$) with `text-xs font-semibold rounded-xl`.

3. **Collision Detection & Flip Threshold**:
   - The floating coordinate positioning algorithm (`updateFloatingPosition`) SHALL evaluate viewport clearance against a compact threshold of $230\text{px}$ (`spaceBelow < 230 && spaceAbove > spaceBelow`).
   - When the trigger is positioned within $230\text{px}$ of the viewport bottom, the popover SHALL flip cleanly above the trigger with zero bottom edge overflow.

#### Scenario: User opens timepicker near bottom of Settings viewport
- **WHEN** the user clicks the timepicker trigger located in the lower region of `/settings`
- **THEN** the popover opens with compact total height $\le 220\text{px}$
- **AND** if vertical space below the trigger is less than 230px, the popover renders above the trigger
- **AND** the entire popover remains fully visible within the viewport without clipping or page scrollbars.

#### Scenario: Number scrubbing within compact column tracks
- **WHEN** the user scrolls the Hours column
- **THEN** options scroll smoothly within the bounded 112px track
- **AND** selecting any hour instantly updates the top preview badge and active highlight pill.
