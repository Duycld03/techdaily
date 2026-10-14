# System Layout Archetypes Delta

## MODIFIED Requirements

### Requirement: Global Navigation Chrome & Header Elevation Standards
On learner routes, the global application shell components (`AppHeader.vue`, `AppSidebar.vue`) SHALL adhere to unified studio density and elevation specifications:

1. **Header Chrome (`AppHeader.vue`)**:
   - SHALL maintain a fixed compact height (`h-14 sm:h-15`) with glassmorphic backdrop blur (`bg-white/95 dark:bg-canvas/80 backdrop-blur-md`).
   - SHALL provide compact telemetry badges: Streak counter with flame badge, Quick Search trigger (`⌘K`), Theme switch, Locale selector, and user account dropdown.
   - Mobile navigation drawer SHALL slide in smoothly with safe area inset padding and backdrop dimming, closing automatically on route navigation or `Escape` key press.
2. **Sidebar Chrome (`AppSidebar.vue`)**:
   - SHALL render on desktop ($\ge 768\text{px}$) with standard width `w-64`, grouped section headers (`nav.group_learn`, `nav.group_knowledge`, `nav.group_system`), and active route indicators with high-contrast text and violet glow pills.
   - Navigation links SHALL enforce `whitespace-nowrap shrink-0` across English and Vietnamese locales.

#### Scenario: Global header rendering across viewports
- **WHEN** a user navigates across learner pages on desktop or mobile
- **THEN** `AppHeader.vue` remains pinned at the top with hairline border and glassmorphic backdrop
- **AND** interactive controls (Search, Theme, Language, Profile) remain accessible without layout jitter.

#### Scenario: Administrator navigation is isolated
- **WHEN** an authorized administrator visits `/admin` or `/admin/catalog`
- **THEN** the dedicated administrator shell replaces learner navigation and telemetry
- **AND** shared design tokens and standardized page archetypes remain in use.


## ADDED Requirements

### Requirement: Administrator shell design continuity
The administrator shell SHALL reuse DeepPace semantic colors, canvas and glass surfaces, typography, iconography and accessible shared components. It SHALL host the existing StudioLayout for login, BentoDashboardLayout for overview and MasterDetailLayout for catalog. Native selects, browser dialogs and an independent admin theme SHALL NOT be introduced.

#### Scenario: Administrator changes locale and theme
- **WHEN** an administrator changes English/Vietnamese locale or light/dark theme
- **THEN** login, dashboard, navigation and catalog follow the existing DeepPace theme and translated labels
- **AND** shared learner components retain their existing behavior.
