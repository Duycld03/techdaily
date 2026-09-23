# Spec Delta: system-layout-archetypes

## Purpose
Defines the three canonical system layout archetypes (`StudioLayout`, `MasterDetailLayout`, and `BoardLayout`) that govern page-level view composition across TechDaily, ensuring responsive viewport bounding, slot-based composability, and balanced engineering cockpit density (8/10) on standard 1080p desktop displays without dead black voids or unconstrained single-column stretching.

## ADDED Requirements

### Requirement: Studio Layout Archetype
The web frontend SHALL provide a reusable layout component `StudioLayout` (`frontend/components/layout/StudioLayout.vue`) designed for focused learning, drills, and active practice sessions.
1. **Geometry & Viewport Bounding**:
   - The layout container SHALL be bounded to viewport height (`min-h-[calc(100vh-3.5rem)] sm:min-h-[calc(100vh-3.75rem)]`) with zero nested double-scrollbars.
   - The desktop layout SHALL split horizontally into two distinct columns:
     - **Main Action Stage (`#main`)**: Occupies 65% to 70% width (`w-full lg:w-[68%]`) providing a comfortable, centered stage for the primary interactive card, reader slice, or drill challenge.
     - **Telemetry & Context Dock (`#dock`)**: Occupies 30% to 35% width (`hidden lg:flex lg:w-[32%]`) as a dedicated rail for session progress, keyboard shortcut cheatsheets, SM-2 scheduling metrics, and source document context quotes.
2. **Slots & Fallbacks**:
   - The component SHALL expose named slots: `#header` (optional breadcrumb or session controls), `#main` (required primary interaction), `#dock` (required telemetry and auxiliary widgets), and `#footer` (optional sticky bottom actions).
   - On mobile viewports ($< 1024\text{px}$), the main action stage SHALL expand to full width (`w-full`), while dock items SHALL stack below or render as a collapsible bottom drawer.

#### Scenario: Rendering Studio Layout on Desktop 1080p
- **WHEN** an engineer opens a practice session using `StudioLayout` on a 1920x1080 viewport with available width $\ge 1280\text{px}$
- **THEN** the main action stage renders on the left column (68% width), the telemetry dock renders on the right column (32% width), and neither column causes horizontal or vertical page overflow.

#### Scenario: Responsive Fallback on Mobile Screens
- **WHEN** a user accesses a `StudioLayout` page on a mobile screen ($< 1024\text{px}$)
- **THEN** the main interaction stage occupies 100% of the screen width, and the dock elements stack sequentially below the primary stage.

---

### Requirement: Master-Detail Layout Archetype
The web frontend SHALL provide a reusable layout component `MasterDetailLayout` (`frontend/components/layout/MasterDetailLayout.vue`) designed for multi-section settings, user profile management, and account configuration.
1. **Rail & Panel Architecture**:
   - The layout SHALL provide a persistent sub-navigation rail (`w-full md:w-64 shrink-0`) on the left, containing section tabs, category badges, and active state pill indicators.
   - The layout SHALL provide an expansive content panel (`flex-1 min-w-0`) on the right, organizing form fields and settings into responsive multi-column grids rather than long single-column stacks.
2. **Elimination of Side Voids**:
   - The layout container SHALL utilize `max-w-7xl` or full-width fluid bounds, eliminating $> 400\text{px}$ dead margins on 1080p desktop displays.
3. **Slots**:
   - The component SHALL expose named slots: `#header` (title, description, and status badges), `#nav` (category rail navigation), and `#content` (active settings or configuration surface).

#### Scenario: Settings Navigation via Master-Detail Layout
- **WHEN** an engineer visits a settings surface using `MasterDetailLayout`
- **THEN** the category rail is pinned on the left with distinct section icons, the active panel displays on the right, and no awkward horizontal empty margins surround the interface.

---

### Requirement: Board Layout Archetype
The web frontend SHALL provide a reusable layout component `BoardLayout` (`frontend/components/layout/BoardLayout.vue`) designed for content-dense browsing, notes archives, and document libraries.
1. **Header & Filtering Bar (`#filters`)**:
   - The layout SHALL include a sticky top toolbar supporting full-text search, keyboard shortcut badges (⌘K), horizontal tag filter chips (#Tag), view mode toggles (Grid / List), and primary action triggers.
2. **Auto-Flowing Responsive Grid (`#content`)**:
   - The main surface SHALL arrange items into an auto-flowing grid: 1 column on mobile, 2 columns on tablets/small laptops (`md:grid-cols-2`), and 3 columns on desktop displays (`xl:grid-cols-3 gap-4`).
   - The board grid SHALL evenly distribute cards across the available width without leaving empty vertical voids at the bottom when cards are populated.
3. **Slots**:
   - The component SHALL expose named slots: `#header`, `#filters`, `#content`, and `#pagination`.

#### Scenario: Browsing Saved Notes on Desktop
- **WHEN** a user views a collection of highlights or cards in `BoardLayout` on a desktop viewport ($\ge 1280\text{px}$)
- **THEN** cards are arranged in an auto-flowing 2-or-3 column grid spanning the available container width, with search and tag filters pinned at the top.
