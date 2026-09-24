# Spec Delta: system-layout-archetypes

## ADDED Requirements

### Requirement: Master-Detail Layout Archetype Standards
The web frontend SHALL formalize the `MasterDetailLayout` archetype (`MasterDetailLayout.vue`) for multi-category configuration and settings surfaces (e.g., `settings.vue`, account management):

1. **Sub-Navigation Rail (`#nav`)**:
   - On desktop viewports ($\ge 768\text{px}$), the sub-navigation rail SHALL render as a dedicated left sidebar (`md:w-64 shrink-0`) containing category navigation items with icons, title, and active pill styling (`bg-brand-500/10 text-brand-600 dark:text-brand-400 font-bold`).
   - The rail SHALL be visually separated from the content panel by a subtle hairline border (`border-slate-200 dark:border-white/[0.08]`).
2. **Expansive Content Panel (`#content`)**:
   - The main content panel SHALL occupy the remaining viewport width (`flex-1 min-w-0`), rendering structured form sections, input fields, and action buttons without cramped widths or dead horizontal margins.
   - Form fields SHALL utilize responsive 1-column or 2-column grid layouts with consistent vertical rhythm (`space-y-6`).
3. **Mobile Responsive Degradation**:
   - On mobile viewports ($< 768\text{px}$), the sub-navigation rail SHALL stack horizontally as top segmented category pills or a scrollable category tab bar, allowing full-width presentation of settings controls below.

#### Scenario: User navigates settings tabs on desktop
- **GIVEN** an authenticated user is on the `/settings` page on a desktop viewport ($\ge 768\text{px}$)
- **WHEN** user clicks on a category in the left navigation rail (e.g., "Notifications" or "Security")
- **THEN** the active tab updates smoothly without full page reloads
- **AND** the content panel displays the selected section's configuration controls with expansive horizontal width.

#### Scenario: Mobile viewport responsive adaptation
- **WHEN** user views `/settings` on a mobile screen (< 768px)
- **THEN** the category rail presents as a compact horizontal tab bar at the top
- **AND** the settings form fields span 100% of the screen width with comfortable touch padding.

### Requirement: Global Navigation Chrome & Header Elevation Standards
The global application shell components (`AppHeader.vue`, `AppSidebar.vue`) SHALL adhere to unified studio density and elevation specifications:

1. **Header Chrome (`AppHeader.vue`)**:
   - SHALL maintain a fixed compact height (`h-14 sm:h-15`) with glassmorphic backdrop blur (`bg-white/95 dark:bg-canvas/80 backdrop-blur-md`).
   - SHALL provide compact telemetry badges: Streak counter with flame badge, Quick Search trigger (`⌘K`), Theme switch, Locale selector, and user account dropdown.
   - Mobile navigation drawer SHALL slide in smoothly with safe area inset padding and backdrop dimming, closing automatically on route navigation or `Escape` key press.
2. **Sidebar Chrome (`AppSidebar.vue`)**:
   - SHALL render on desktop ($\ge 768\text{px}$) with standard width `w-64`, grouped section headers (`nav.group_learn`, `nav.group_knowledge`, `nav.group_system`), and active route indicators with high-contrast text and violet glow pills.
   - Navigation links SHALL enforce `whitespace-nowrap shrink-0` across English and Vietnamese locales.

#### Scenario: Global header rendering across viewports
- **WHEN** a user navigates across pages on desktop or mobile
- **THEN** `AppHeader.vue` remains pinned at the top with hairline border and glassmorphic backdrop
- **AND** interactive controls (Search, Theme, Language, Profile) remain accessible without layout jitter.
