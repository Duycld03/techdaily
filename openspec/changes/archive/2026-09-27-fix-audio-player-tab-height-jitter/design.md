# Design

## Context

In `ReaderAudioPlayer.vue`, the player bar is a horizontal flex container (`flex flex-wrap items-center gap-2 sm:gap-3 ... px-3 py-2`).
The interactive controls inside include:
1. "Listen" action button: `px-3 py-1.5 text-sm font-semibold` without border, rendering at exactly 32px height ($12\text{px} \text{ padding} + 20\text{px} \text{ line-height} = 32\text{px}$).
2. Engine switch segmented pill: `p-0.5 border` with inner button `py-1 text-xs`, rendering at 30px height.
3. Voice selector (`AppSelect.vue` with `size="sm"`): `px-3 py-1.5 text-xs sm:text-sm font-semibold border`, which renders at 34px ($12\text{px} \text{ padding} + 20\text{px} \text{ line-height} + 2\text{px} \text{ border} = 34\text{px}$).
4. Speed button: `px-2.5 py-1 text-xs font-semibold`, rendering at 24px height.

Because `AppSelect` is conditionally mounted with `v-if="engineMode === 'cloud'"`, entering Cloud mode raises the maximum child height from 32px to 34px, expanding the container from 50px to 52px. Switching back to Device mode unmounts `AppSelect`, causing the container to collapse back to 50px. This 2px layout jitter pushes the entire slice text content down and up on every tab switch.

## Goals / Non-Goals

**Goals:**
- Clamp `AppSelect.vue` `size="sm"` trigger button to an exact 32px height (`h-8`), matching the 32px rendered height of the "Listen" button.
- Enforce height stability on the `ReaderAudioPlayer.vue` player container (`min-h-[50px]`) so that toggling between Cloud and Device modes results in zero cumulative layout shift (0px height difference).
- Maintain Vietnamese diacritic legibility and WCAG accessibility standards.

**Non-Goals:**
- Altering the `size="md"` styling used across standard form inputs and modal dialogs elsewhere in the application.
- Redesigning the segmented engine toggle pill or speed selector.

## Decisions

### Decision 1: Explicit `h-8` (`32px`) height for `AppSelect` `size="sm"`
Instead of relying solely on `py-1.5` padding (which varies by 2px due to border box calculation), `sizeClasses` for `sm` will include `h-8` (`2rem` / 32px) and `flex items-center`. This guarantees that the button box-sizing is fixed to exactly 32px including borders and padding across all browsers.

### Decision 2: Height normalization on the "Listen" action button
Add `h-8` explicitly to the "Listen" action button in `ReaderAudioPlayer.vue` (`h-8 px-3 text-sm font-semibold inline-flex items-center`) to ensure both buttons share an identical 32px box model.

### Decision 3: Container `min-h-[50px]` stability
Apply `min-h-[50px]` to the parent player container in `ReaderAudioPlayer.vue` (`min-h-[50px] px-3 py-2`). With 32px controls and 18px container padding/border, the total rendered height remains invariant at 50px whether the voice select is present or omitted.

## Risks / Trade-offs

- **Vietnamese Diacritics**: In `h-8` (32px), `text-xs sm:text-sm` uses a 20px line-height. With 12px vertical headroom (6px top/bottom), all uppercase and accented Vietnamese characters (e.g., `Đ`, `Ệ`, `Ư`, `Ờ`) render cleanly without vertical truncation.
- **Option Listbox Tap Targets**: Only the combobox trigger button is sized to `h-8`; the listbox option items remain at $\ge 40\text{px}$–$44\text{px}$ ensuring touch compliance.
