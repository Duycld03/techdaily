# Design

## Context

See `proposal.md` for motivation.

Currently, `AppHeader.vue` is configured with `sticky top-0 z-40`. However, multiple view components render controls with overlapping or higher z-indexes:
- `roadmap.vue`: Track selector header sets `z-40`, with child popovers at `z-50`.
- `today.vue`: Studio control bar uses `z-30`, with book popover and floating selection menu at `z-50`.
- `graph.vue`: GraphDetailDrawer and overlays sit at `z-50`.
- `library.vue`, `notes.vue`: Search toolbars and filter popovers use `z-30` or `z-50`.

Furthermore, horizontal header real-estate is constrained: on viewports $< 640\text{px}$, the left cluster (Menu, Logo, Mobile Search) and right cluster (Streak, Locale, Theme, Profile, SignOut) total over $330\text{px}$ of content width, causing clipping and overlap on $320\text{px} - 375\text{px}$ devices. On $640\text{px} - 1024\text{px}$ tablets, the centered ⌘K bar (`max-w-md`) lacks flexible shrinking (`min-w-0`), leading to collision with neighboring elements.

## Goals / Non-Goals

**Goals:**
- Unify platform z-index stacking hierarchy: `AppHeader` at `z-50`, in-page sticky toolbars and local popovers capped at $\le \text{z-40}$, and modal backdrops / `AppCommandPalette` at $\ge \text{z-60}$ (`AppToastContainer` at `z-[9999]`).
- Ensure the header search trigger is always visible, clickable, and never occluded by page content across all routes.
- Prevent horizontal layout collisions in `AppHeader.vue` across all breakpoints ($320\text{px}$, $375\text{px}$, $640\text{px}$, $768\text{px}$, $1024\text{px}$).
- Guarantee fluid, elastic scaling for the centered search trigger on tablet viewports.

**Non-Goals:**
- Altering the internal filtering or keyboard navigation logic of `AppCommandPalette.vue`.
- Changing reader mode (`/read/:bookId`) which intentionally disables `AppHeader` for distraction-free reading.

## Decisions

1. **Z-Index Layering Hierarchy Standard**:
   - Level 1: In-page sticky headers, local floating bars, and dropdown popovers: `z-20` to `z-35`.
   - Level 2: Platform Application Header (`AppHeader.vue`): `z-50`.
   - Level 3: Full-screen mobile navigation drawer, modal dialogs, and `AppCommandPalette.vue`: `z-60` (teleported to body).
   - Level 4: Global toast notifications (`AppToastContainer.vue`): `z-[9999]`.

2. **Mobile Header Action Prioritization**:
   - On screens $< 480\text{px}$, header action buttons will be condensed with tighter spacing (`gap-1 sm:gap-2`) and secondary controls (such as Theme and Locale) will gracefully adjust so that Menu, Logo, Search, Streak, and Profile avatar have guaranteed clearance without wrap.
   - The mobile search trigger button will have a guaranteed touch target ($\ge 40\text{px}$) and dedicated position.

3. **Elastic ⌘K Search Trigger Container**:
   - Add `min-w-0` to the centered search bar container: `hidden sm:flex items-center flex-1 min-w-0 max-w-md mx-2 sm:mx-4 lg:mx-8`.
   - Apply `truncate` to the placeholder text and hide the keyboard shortcut `<kbd>` on viewports $< 768\text{px}` (`hidden md:inline-flex`) to prevent width blowouts.

## Risks / Trade-offs

- **Risk**: In-page popovers near the top edge could be clipped if rendered under the sticky header.
- **Mitigation**: All in-page popovers open downwards (`top-full mt-2`) and compute maximum heights using `max-h-[calc(100dvh-12rem)]` or scroll containment so they remain accessible below the 56px/60px header.
