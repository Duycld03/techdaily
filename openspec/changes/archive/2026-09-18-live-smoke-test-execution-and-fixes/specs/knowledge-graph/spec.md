## MODIFIED Requirements

### Requirement: Application Navigation & Mobile Responsive Placement
The knowledge graph SHALL be accessible as a first-class navigation item under the **"Knowledge"** navigation section (`nav.group_knowledge`) across both desktop and mobile layouts.

In `frontend/components/layout/AppSidebar.vue`:
- A link with label `nav.graph`, route `/graph`, and icon `Network` from `lucide-vue-next` SHALL be rendered in the Knowledge navigation group alongside Insights, Library, and Notes.
- The link SHALL be highlighted with the active sidebar style when the current route is `/graph`.

In `frontend/components/layout/AppHeader.vue`:
- The mobile slide-out navigation drawer SHALL include the `/graph` link with icon `Network` under the Knowledge section.
- Selecting the link on mobile SHALL close the mobile navigation drawer and navigate to `/graph`.

On mobile viewports ($< 768\text{px}$):
- The graph canvas SHALL occupy 100% of the available screen height below the top header.
- The control bar SHALL collapse into a compact floating filter pill button that expands into a mobile filter bottom sheet on tap.
- The node detail drawer SHALL present as a swipeable bottom sheet rather than a wide side drawer, ensuring comfortable thumb reachability.

#### Scenario: Desktop sidebar navigation
- **WHEN** an authenticated user clicks "Knowledge Graph" in the desktop sidebar
- **THEN** the browser navigates to `/graph`
- **AND** the sidebar marks the "Knowledge Graph" link as active with the primary accent background.

#### Scenario: Mobile header navigation
- **WHEN** a user on a mobile device opens the header hamburger menu and taps "Knowledge Graph"
- **THEN** the mobile menu drawer closes
- **AND** the browser navigates to `/graph`
- **AND** the full-height mobile graph canvas initializes.

#### Scenario: Responsive multi-viewport and bilingual smoke verification
- **WHEN** an automated E2E test runs across Desktop ($1920\times 1080$), Tablet ($768\times 1024$), and Mobile ($375\times 812$) viewports
- **THEN** the knowledge graph canvas initializes successfully in both 2D and 3D engine modes without console errors
- **AND** all HUD buttons, visual legend cards, and category filter pills render without horizontal clipping across both English and Vietnamese locales.

#### Scenario: Live smoke test execution and defect resolution
- **WHEN** the live responsive E2E smoke test is executed against the application runtime
- **THEN** 100% of assertion checks pass across Desktop, Tablet, and Mobile viewports
- **AND** visual snapshots confirm zero label collisions, proper HUD elevation, and unclipped legend cards.
