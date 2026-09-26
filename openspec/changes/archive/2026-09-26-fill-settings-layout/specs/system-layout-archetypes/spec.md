# Spec Delta

## MODIFIED Requirements

### Requirement: Master-Detail Layout Archetype
The web frontend SHALL provide a reusable layout component `MasterDetailLayout` (`frontend/components/layout/MasterDetailLayout.vue`) designed for multi-section settings, user profile management, and account configuration.
1. **Rail & Panel Architecture**:
   - The layout SHALL provide a persistent sub-navigation rail (`w-full md:w-64 shrink-0`) on the left, containing section tabs, category badges, and active state pill indicators.
   - The layout SHALL provide an expansive content panel (`flex-1 min-w-0`) on the right, organizing form fields and settings into responsive multi-column grids rather than long single-column stacks.
2. **Elimination of Side & Bottom Voids**:
   - The layout container SHALL utilize `max-w-7xl` or full-width fluid bounds, eliminating `> 400px` horizontal dead margins on 1080p desktop displays.
   - The page hosting a `MasterDetailLayout` SHALL bound the layout to the full available viewport region as a flex column with a resolved height (the page wrapper is a full-height flex column inside the app shell's scrollable `<main>` region), so the layout card's `flex-1` stretches to fill the region and NO empty vertical void appears below the card on 1080p desktop displays.
   - The layout container SHALL NOT be constrained by a narrower fixed cap (such as `max-w-5xl`) that reintroduces horizontal side voids.
3. **Content Anchoring**:
   - Content within the `#content` panel SHALL be anchored to the top-left of the panel; additional sections SHALL append downward within the panel's own scroll region (`overflow-y-auto`) rather than resizing or centering the card.
   - Form fields MAY retain a readable maximum width and SHALL remain left-aligned; any remaining empty space SHALL sit inside the filled card rather than as dead canvas outside it.
4. **Slots**:
   - The component SHALL expose named slots: `#header` (title, description, and status badges), `#nav` (category rail navigation), and `#content` (active settings or configuration surface).
5. **Full-Bleed (Flush) Variant**:
   - The component SHALL support an optional full-bleed mode (a `flush` prop) that drops the card chrome (rounded corners, border, shadow, backdrop blur) and paints the base canvas surface, so the layout fills its container to all four edges when the host page removes its outer padding. The default (non-flush) mode SHALL retain the `glass-card` chrome for other consumers.
   - When rendered full-bleed directly adjacent to the global application sidebar, the layout SHALL remain visually distinct from that sidebar via surface-elevation contrast and/or a hairline divider, so the two navigation columns do not read as a single merged menu.
   - Sub-navigation category icons SHALL be visually distinct from the page/header icon; no icon SHALL be duplicated between the page header and the active category tab.

#### Scenario: Settings Navigation via Master-Detail Layout
- **WHEN** an engineer visits a settings surface using `MasterDetailLayout`
- **THEN** the category rail is pinned on the left with distinct section icons, the active panel displays on the right, and no awkward horizontal empty margins surround the interface.

#### Scenario: No bottom or side void on 1080p desktop
- **WHEN** a user views a `MasterDetailLayout` settings surface (e.g. `/settings`) on a 1920x1080 viewport with content shorter than the viewport
- **THEN** the layout card fills the full available height and width of the content region, its content stays anchored top-left, and no empty vertical void appears below the card and no `> 400px` horizontal margin appears beside it.

#### Scenario: Full-bleed settings surface adjacent to the global sidebar
- **WHEN** the settings surface renders in full-bleed (flush) mode on desktop, directly beside the global application sidebar
- **THEN** the surface fills all four edges with no rounded corners or outer padding, and it stays visually separated from the global sidebar (via elevation contrast plus the sidebar's divider) so the two navigation columns remain distinguishable
- **AND** the active category tab icon is not a duplicate of the page header icon
