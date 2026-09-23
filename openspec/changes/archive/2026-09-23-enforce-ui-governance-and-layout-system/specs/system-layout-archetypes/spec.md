# Spec Delta: system-layout-archetypes

## ADDED Requirements

### Requirement: Bento Dashboard Layout Archetype
The web frontend SHALL provide a reusable layout component `BentoDashboardLayout` (`frontend/components/layout/BentoDashboardLayout.vue`) specifically designed for multi-widget executive cockpits and command center overviews.
1. **Asymmetric Grid Geometry**:
   - The layout container SHALL span the available desktop viewport (`max-w-7xl mx-auto`) with responsive padding (`px-3.5 sm:px-6 lg:px-8 py-3.5 sm:py-4`).
   - The desktop grid SHALL organize into a 3-column asymmetric layout:
     - **Action Stage (`#left` or `#action-stage`)**: Occupies two columns on large screens (`lg:col-span-2`) to house primary high-priority interactive cards with synchronized vertical spacing.
     - **Telemetry & Cosmos Dock (`#right` or `#telemetry-dock`)**: Occupies one column on large screens (`lg:col-span-1`) for metrics, retention heatmaps, and knowledge graph previews.
2. **Concentric Elevation and Spacing**:
   - The layout container SHALL eliminate internal dead margins between tiles, maintaining uniform gap spacing (`gap-3.5 sm:gap-4`) and equalizing vertical column boundaries.
3. **Slots**:
   - The component SHALL expose named slots: `#header` (orientation banner and greeting), `#action-stage` (primary action tiles), `#telemetry-dock` (metrics, consistency, and graph widgets), and `#footer` (optional bottom status).

#### Scenario: Rendering Bento Dashboard on Desktop Viewport
- **WHEN** an authenticated user opens an executive dashboard using `BentoDashboardLayout` on a 1920x1080 display
- **THEN** the layout renders a top orientation banner across full width, two columns of primary action tiles on the left, and one column of telemetry widgets on the right, with zero awkward horizontal dead space inside card bodies.

#### Scenario: Responsive Mobile Stacking
- **WHEN** a user accesses a `BentoDashboardLayout` on a mobile viewport ($< 1024\text{px}$)
- **THEN** all Bento tiles automatically collapse into a single vertical column arranged by priority, preserving full touch targets and zero horizontal overflow.

---

### Requirement: Isolated Playground Sandbox Prototyping Invariant
The web frontend SHALL maintain an isolated development playground directory at `frontend/pages/playground/` for rapid component prototyping, layout experiments, and visual review without polluting production application routes.
1. **Isolation from Production State**:
   - Sandbox prototype pages under `frontend/pages/playground/` SHALL use mock datasets and self-contained state rather than coupling to global production stores.
   - Playground prototypes SHALL NOT be linked in production navigation menus (`AppSidebar.vue`, `AppHeader.vue`).
2. **Visual Verification Prerequisite**:
   - Any new major layout archetype or high-impact page redesign SHALL be drafted and verified in a playground route with headless 1080p screenshot evidence prior to production cutover.

#### Scenario: Prototyping a New Layout in Playground
- **WHEN** an engineer or agent prototypes a new UI layout at `frontend/pages/playground/dashboard-v2.vue`
- **THEN** the route is accessible locally for visual and screenshot inspection without modifying `frontend/pages/index.vue` or affecting production test suites.
