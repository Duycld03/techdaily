# Spec Delta

## MODIFIED Requirements

### Requirement: Application Header Stacking Precedence and Responsive Search Trigger Accessibility
The Application Header (`AppHeader.vue`) SHALL maintain an elevated stacking context (`z-index: 50`) above all in-page sticky action bars, popover menus, floating selection toolbars, and content drawers across all platform pages (`today.vue`, `roadmap.vue`, `graph.vue`, `library.vue`, `notes.vue`, `review.vue`, `insights.vue`, `profile.vue`, `settings.vue`).

The header search bar and quick-jump command palette trigger SHALL remain visible, unclipped, and touch-accessible across all screen sizes from $320\text{px}$ mobile phones to desktop displays, without overlap from adjacent brand items, streak counters, or locale and theme controls.

#### Scenario: Persistent Header Stacking Above In-Page Content and Sticky Controls
- **WHEN** user navigates any page with sticky toolbars, popovers, or floating menus (e.g. `today.vue` book switcher, `roadmap.vue` track menu, `graph.vue` floating control bar, or `library.vue` search bar)
- **THEN** `AppHeader` SHALL render strictly above these in-page components during scrolling and menu toggling
- **AND** modal dialogs, drawers, and `AppCommandPalette` SHALL render at a higher layer (`z-index: 60` or above) to maintain accessible modal focus.

#### Scenario: Mobile Search Trigger Accessibility on Narrow Displays (< 640px)
- **WHEN** user views any platform page on a mobile viewport ($< 640\text{px}$, including $320\text{px}$ to $390\text{px}$)
- **THEN** the search trigger button SHALL be cleanly rendered with touch target dimensions $\ge 40\text{px} \times 40\text{px}$
- **AND** header action items (streak badge, locale switcher, theme toggle, and profile menu) SHALL NOT collide, wrap, or force horizontal overflow off the screen.

#### Scenario: Centered ⌘K Search Bar Elastic Scaling on Tablet Viewports (640px - 1024px)
- **WHEN** user views the platform on a medium or tablet viewport ($640\text{px} - 1024\text{px}$)
- **THEN** the centered ⌘K search bar SHALL elastically scale with `min-w-0` and truncate placeholder text without squishing or overlapping the brand title on the left or the action buttons on the right.
