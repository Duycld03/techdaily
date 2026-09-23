# Spec Delta: core-platform

## ADDED Requirements

### Requirement: Executive Cockpit Bento Dashboard Layout Integration
The primary root route `/` (`HomeBentoDashboard.vue`) SHALL implement the `BentoDashboardLayout` archetype (`BentoDashboardLayout.vue`), decoupling layout shell geometry from individual card content.
1. **Header Slot (`#header`)**:
   - Houses the Welcome & Orientation Banner, displaying the personalized engineer greeting, role target, active reading slice badge, and streak status.
2. **Action Stage Slot (`#action-stage`)**:
   - Houses Card A (Today's Reading Slice) and Card B (Daily Scenario Challenge) with compact typography, tight CTA placement, and zero horizontal dead voids.
3. **Telemetry Dock Slot (`#telemetry-dock`)**:
   - Houses Card C (7-day Consistency Heatmap and SM-2 Due Count) and Card D (Domain Knowledge Constellation Card), equalizing total vertical height with the action stage.

#### Scenario: Navigating Home Dashboard on 1080p Desktop
- **WHEN** an engineer loads the root page `/` on a 1920x1080 desktop browser
- **THEN** the entire Bento Grid renders with cohesive spacing and equalized column heights, eliminating empty internal margins within Card A and Card B.

---

### Requirement: Settings Master-Detail Desktop Layout Standard
The Settings interface (`frontend/pages/settings.vue`) SHALL implement the `MasterDetailLayout` archetype (`MasterDetailLayout.vue`), replacing the narrow single-column layout with a standard desktop master-detail architecture.
1. **Category Navigation Rail (`#nav`)**:
   - Pinned on the left (`w-full md:w-64 shrink-0`) featuring section icons, category titles (General & Preferences, Push Notifications, Security), and active state pills.
2. **Settings Content Panel (`#content`)**:
   - Expands to fill available width (`flex-1 min-w-0`), organizing form controls into responsive 2-column grids (`grid sm:grid-cols-2 gap-4`) using `AppSelect.vue` for all selection inputs.
   - Eliminates $> 800\text{px}$ dead margins on desktop displays.

#### Scenario: Configuring Settings on Desktop
- **WHEN** an engineer accesses `/settings` on a desktop viewport ($\ge 1280\text{px}$)
- **THEN** the left rail displays configuration categories, the right panel displays the active settings form in a 2-column grid, and no empty side voids surround the interface.

---

### Requirement: Developer & Agent UI Design Governance Protocol
The project repository SHALL mandate strict UI design governance rules codified in `AGENTS.md` (Rules 19, 20, and 21):
1. **Rule 19 (Mandatory System Layout Archetypes)**: All page Single File Components (`*.vue`) MUST inherit from one of the four standardized layout archetypes (`StudioLayout`, `MasterDetailLayout`, `BoardLayout`, or `BentoDashboardLayout`). Arbitrary unconstrained wrapper divs causing empty black voids on 1080p screens are strictly prohibited.
2. **Rule 20 (Strict Prohibition of Native HTML Select)**: Raw `<select><option>` elements are strictly prohibited across all components and views. All selection controls MUST use `AppSelect.vue`.
3. **Rule 21 (Sandbox Playground & Screenshot Preview Protocol)**: Major UI redesigns or new layouts MUST be drafted in `frontend/pages/playground/`, visually verified via headless 1080p screenshots, and approved by the user before cutover into production routes.

#### Scenario: Agent Implements a New View
- **WHEN** an AI agent or developer is instructed to create or refactor a frontend view
- **THEN** the agent selects an established layout archetype, verifies dropdowns use `AppSelect.vue`, and prototypes in `frontend/pages/playground/` with visual screenshot proof before touching production routes.
