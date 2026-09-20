# Proposal

## Why

On mobile and narrow tablet viewports ($320\text{px} - 1024\text{px}$), the application header search bar and quick-search command palette trigger become obscured, clipped, or inaccessible across multiple pages. The root causes are twofold:
1. Conflicting z-index stacking layers where in-page sticky bars, popovers, floating menus, and drawers (`z-40`, `z-50`) overlap the sticky header (`z-40`).
2. Horizontal layout crowding within `AppHeader.vue` where the centered ⌘K search bar and mobile action clusters (Streak, Locale, Theme, Profile, SignOut) collide on screens between $320\text{px}$ and $840\text{px}$.

Standardizing header stacking precedence and responsive action layout ensures that search and navigation remain reliably visible, unclipped, and touch-accessible across all platform pages.

## What Changes

- **Header Stacking Context Elevation**: Elevate `AppHeader.vue` from `z-40` to `z-50` (or ensure page overlays and drawer backdrops do not occlude the persistent header unless explicitly modal, while modals and command palette remain above at `z-60` / `z-[9999]`).
- **Responsive Search Trigger Geometry**: Refactor `AppHeader.vue` search trigger to adapt cleanly:
  - On compact mobile screens ($< 640\text{px}$): Provide a dedicated search button that does not compress into brand or action icons, avoiding collision with the streak and profile items.
  - On mid-width viewports ($640\text{px} - 1024\text{px}$): Allow the centered ⌘K search bar to flex without overflowing or squishing adjacent action buttons.
- **Header Action Strip Decluttering on Narrow Screens**: Condense header actions on narrow mobile viewports ($< 400\text{px}$) by prioritizing core controls (Menu, Logo, Search, Profile) and moving secondary toggles into the mobile navigation drawer if screen width is constrained.
- **Page-Level Layering Audit & Remediation**: Audit sticky toolbars and dropdown popovers across all primary views (`today.vue`, `roadmap.vue`, `graph.vue`, `library.vue`, `notes.vue`, `review.vue`, `insights.vue`) to ensure in-page sticky headers sit strictly below `AppHeader` in the visual hierarchy.

## Capabilities

### New Capabilities
<!-- None -->

### Modified Capabilities
- `core-platform`: Standardize header stacking context, responsive search bar layout, and mobile action containment across all pages.
