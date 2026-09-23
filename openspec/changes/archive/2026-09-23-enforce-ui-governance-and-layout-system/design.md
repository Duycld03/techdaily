# Design: UI Governance Architecture and System Layout Adoption

## Context

TechDaily's layout foundations (`StudioLayout`, `MasterDetailLayout`, and `BoardLayout`) are established and proven on `/review` and `/showcase`. However, without explicit governance in `AGENTS.md`, AI agents and developers may continue writing unconstrained `max-w-*` wrappers or unstyled `<select>` elements on other pages. Furthermore, the root Home Dashboard (`/`) requires a dedicated asymmetric multi-widget layout archetype (`BentoDashboardLayout`) to prevent card bloating and empty spaces, while `/settings` and `/notes` still suffer from single-column desktop stretching.

This design introduces the 4-tier governance framework and completes the migration of all remaining major application surfaces onto standardized layout archetypes.

## Goals / Non-Goals

**Goals:**
- Codify Rules 19, 20, and 21 in `AGENTS.md` (Mandatory Layouts, Prohibition of Native Select, Playground & Screenshot Preview Protocol).
- Create `BentoDashboardLayout.vue` (`frontend/components/layout/BentoDashboardLayout.vue`) supporting `#header`, `#action-stage`, `#telemetry-dock`, and `#footer` slots.
- Scaffold `frontend/pages/playground/` sandbox environment with README and interactive sample.
- Refactor `/` (`HomeBentoDashboard.vue`) to use `BentoDashboardLayout`, tightening internal padding and balancing tile boundaries.
- Refactor `/settings` (`frontend/pages/settings.vue`) to use `MasterDetailLayout`, establishing a 256px category rail and 2-column form grid.
- Refactor `/notes` (`frontend/pages/notes.vue`) to use `BoardLayout`, transforming the 1-column list into an auto-flowing 2-to-3 column highlight card grid.
- Equalize vertical column heights in `/profile` Tier 3.

**Non-Goals:**
- Changing backend business logic, database migrations, or API contracts.
- Altering core SM-2 algorithms, spaced repetition scheduling, or daily focus pacer logic.
- Adding third-party CSS or UI component libraries (maintaining zero-runtime-overhead Tailwind CSS).

## Decisions

### 1. The 4-Tier Governance Model
```
+-----------------------------------------------------------------------------------+
| TIER 1: AGENTS.md Constitution (Rules 19, 20, 21)                                 |
| - Agents must inherit 1 of 4 Layout Archetypes. No random max-w-* divs.           |
| - Native <select> is strictly forbidden. Must use AppSelect.vue.                  |
| - Major UI work must be prototyped in playground and screenshot-approved first.   |
+-----------------------------------------------------------------------------------+
| TIER 2: Playground Sandbox (`frontend/pages/playground/`)                         |
| - Isolated route for mock-data UI prototyping before touching production routes.  |
+-----------------------------------------------------------------------------------+
| TIER 3: Layout Archetype Primitives (`frontend/components/layout/`)               |
| - StudioLayout (68/32 split single-task learning)                                 |
| - BentoDashboardLayout (3-column asymmetric multi-widget command center)          |
| - MasterDetailLayout (256px sub-nav rail + wide form surface)                     |
| - BoardLayout (Top search/filter bar + auto-flowing 3-column card grid)           |
+-----------------------------------------------------------------------------------+
| TIER 4: Quality Gate & Headless Visual Proof                                      |
| - Headless 1080p Chromium screenshot verification before change closure.         |
+-----------------------------------------------------------------------------------+
```

### 2. BentoDashboardLayout Geometry
- **Outer Shell**: `max-w-7xl mx-auto px-3.5 sm:px-6 lg:px-8 py-3.5 sm:py-4 min-h-[calc(100dvh-3.5rem)] flex flex-col gap-3.5 sm:gap-4`.
- **Asymmetric Grid**: `grid grid-cols-1 lg:grid-cols-3 gap-3.5 sm:gap-4 items-start`.
  - `#action-stage` (`lg:col-span-2`): Houses Card A (Reading Slice) and Card B (Scenario Drill).
  - `#telemetry-dock` (`lg:col-span-1`): Houses Card C (Heatmap & Due Count) and Card D (Knowledge Constellation).
- **Height Synchronization**: Left and right columns align their bottom bounds, preventing bottom-heavy or top-heavy visual imbalance.

### 3. Settings Master-Detail Architecture
- **Navigation Rail (`#nav`)**: 3 distinct tabs:
  1. `General & Study Preferences`: Target role, daily study pace, timezone, theme.
  2. `Web Push Notifications`: Push toggle, quiet hours, test notification trigger.
  3. `Security & Password`: PBKDF2 password change inputs.
- **Form Surface (`#content`)**: Two-column responsive grid (`grid sm:grid-cols-2 gap-4`) for compact density, utilizing `AppSelect` for all dropdown controls.

### 4. Notes Board Architecture
- **Filters Toolbar (`#filters`)**: Search input with ⌘K + tag chips (#All, #Database, #Vue, #Kafka).
- **Content Grid (`#content`)**: `grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4` auto-flowing highlight cards.

## Risks / Trade-offs

- **Risk: Breaking Existing Test Selectors**:
  - *Mitigation*: Retain all existing `data-testid` attributes (such as `data-testid="track-switcher-btn"`, `data-testid="save-profile-btn"`) during layout wrapping.
- **Risk: Mobile Route Stacking**:
  - *Mitigation*: All four layout archetypes use `lg:grid-cols-3` or `md:flex-row` with natural `flex-col` / `grid-cols-1` mobile fallbacks.
