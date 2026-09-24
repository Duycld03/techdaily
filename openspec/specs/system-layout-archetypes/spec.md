# System Layout Archetypes Specification

## Purpose
Defines the three canonical system layout archetypes (`StudioLayout`, `MasterDetailLayout`, and `BoardLayout`) that govern page-level view composition across TechDaily, ensuring responsive viewport bounding, slot-based composability, and balanced engineering cockpit density (8/10) on standard 1080p desktop displays without dead black voids or unconstrained single-column stretching.

## Requirements

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

---

### Requirement: Unboxed Direct-Canvas Catalog Browsing Standard
The system SHALL standardize catalog and content browsing surfaces (`library.vue`, `review.vue` Tab 2: Deck Management, and `insights.vue`) on the **Unboxed Direct-Canvas Layout Archetype**, prohibiting monolithic outer wrapper cards (`BoardLayout` or giant enclosing `glass-card`) that produce double-card nesting. Functional tiers (Header/Bento stats, Filter bars, Auto-flowing 3-column grids, and Pagination) MUST sit directly on the page background canvas (`bg-slate-50 dark:bg-canvas`) within a consistent container (`max-w-7xl mx-auto`).

#### Scenario: Desktop 1080p unboxed catalog grid distribution
- **WHEN** a user accesses the library or review deck management page on a desktop viewport ($W \ge 1280\text{px}$)
- **THEN** content cards MUST distribute across a 3-column responsive auto-flowing grid (`xl:grid-cols-3 gap-4`) sitting directly on the canvas without an outer enclosing card container or nested vertical scrollbars.

#### Scenario: Mobile and tablet responsive degradation
- **WHEN** a user views an unboxed catalog browsing surface on tablet ($768\text{px} \le W < 1280\text{px}$) or mobile ($W < 768\text{px}$)
- **THEN** the grid MUST automatically collapse to 2 columns on tablet and 1 column on mobile without clipping content cards.

### Requirement: Finalized Preview UI Micro-Component Parity
The system SHALL implement production components for Phase 1 catalog surfaces matching the structural, typographical, and density specifications finalized and verified in the Playground Preview UI (`playground/temp.vue`):
1. **Book Cards (`library.vue`)**: MUST feature `p-5 rounded-2xl` hairline borders, category badge, slice bookmark counter with Bookmark icon, title, author/source URL, status pill, progress bar, format badge, and compact reading action button with `GraduationCap` icon.
2. **Flashcard Inventory Cards (`FlashcardBentoCard.vue`)**: MUST feature `p-4 rounded-xl` hairline borders, urgency badge (`Due Today` / `Mastered` / `Learning`), SM-2 algorithm metrics (`EF: x.xx • Interval: xd`), bold question, 2-line answer preview (`line-clamp-2`), source citation, and compact action controls (`Details →`, Edit, Reset, Delete).
3. **Bento Stat Cards (`FlashcardHeroCard.vue`, `MasteryGaugeCard.vue`, `ReviewForecastChart.vue`)**: MUST feature unified `min-h-[190px]` vertical rhythm, gradient hero styling with high-contrast white CTA button, semi-circular SVG mastery gauge, and 7-day mini bar chart with tooltips.

#### Scenario: Rendering flashcard inventory cards in deck management
- **WHEN** the user views flashcards in Deck Management (`review.vue` Tab 2)
- **THEN** each card MUST render with the compact, hairline-bordered layout showing urgency badge, SM-2 metrics, 2-line answer summary, and source attribution without large collapsible accordion wrappers.

### Requirement: Phase 1 Bilingual i18n Completeness & Zero Hardcoded Strings
Every user-facing label, status, counter, unit, and empty state across Phase 1 catalog browsing surfaces MUST be resolved via `$t` through comprehensive translation dictionaries in `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`. Hardcoded language literals (e.g. `'In Progress'`, `'Ready'`, `'Completed'`, `'Processing'`, `'Loading flashcard library...'`, `'min'`, `'Lát cắt'`) are strictly prohibited.

#### Scenario: Switching locales on book progress status
- **WHEN** the user switches the active locale between English (`en`) and Vietnamese (`vi`)
- **THEN** book reading statuses MUST reactively display localized equivalents (e.g. "In Progress" in English vs "Đang đọc" in Vietnamese, and "Slice 8/23 (35%)" in English vs "Lát cắt 8/23 (35%)" in Vietnamese).

#### Scenario: Switching locales on flashcard urgency and time estimates
- **WHEN** the user views the review deck in Vietnamese
- **THEN** urgency badges MUST display "Đến Hạn Hôm Nay" / "Đã Thuộc" / "Đang Rèn Luyện", and the time estimate MUST display "phút" rather than "min".

### Requirement: Bilingual Action Button Density Invariant
Action buttons, tags, and badge indicators within catalog cards and filter toolbars MUST specify `whitespace-nowrap shrink-0` and hairline border styling (`border-slate-200/80 dark:border-white/[0.08]`) to prevent text wrapping and visual collisions across both English and Vietnamese locales.

#### Scenario: Viewing bilingual button labels
- **WHEN** switching between English and Vietnamese locales on any catalog surface
- **THEN** action buttons (such as "Continue Reading" / "Đọc tiếp" and "Import Document" / "Nhập tài liệu") MUST maintain single-line layout without label wrapping.

### Requirement: StudioLayout Interactive Practice Geometry Invariant
Interactive practice surfaces (`quiz.vue` Arena mode and `review.vue` Tab 1 Flashcard Session) SHALL implement the `StudioLayout` archetype (`StudioLayout.vue`), enforcing balanced engineering density (8/10) on standard 1080p desktop viewports ($W \ge 1280\text{px}$):
1. **Action Stage (`#main`)**: Occupies 65% to 70% width (`lg:w-[68%]`), providing a centered, comfortable stage bounded vertically to prevent bottom fold collisions.
2. **Telemetry Dock (`#dock`)**: Occupies 30% to 35% width (`lg:w-[32%]`), housing live session telemetry (countdown timers, progress meters, SM-2 readouts, keyboard shortcuts) without competing with the primary interaction.
3. **Zero Layout Shifts**: Answering state transitions (question selection, flipping card, answer grading) MUST NOT cause height bouncing, font weight shifts, or page scroll jumping.
4. **Mobile Responsive Degradation**: On mobile screens ($< 1024\text{px}$), the main action stage SHALL occupy 100% width, with telemetry dock elements stacking sequentially below or into a collapsible bottom drawer.

#### Scenario: Desktop 1080p StudioLayout practice distribution
- **WHEN** an engineer begins an interactive quiz or flashcard session on desktop ($W \ge 1280\text{px}$)
- **THEN** the question/flashcard action stage renders in `#main` (68% width), the telemetry dock renders in `#dock` (32% width), and neither column causes window scroll jumping or viewport overflow.

#### Scenario: Mobile viewport responsiveness
- **WHEN** an engineer uses interactive practice on a mobile screen ($< 1024\text{px}$)
- **THEN** the primary interaction fills 100% screen width and telemetry docks collapse cleanly below without clipping option choices or action buttons.

### Requirement: Phase 2 Interactive Practice Playground Sandbox Verification
Prior to modifying production routes (`frontend/pages/quiz.vue`, `frontend/pages/review.vue`), the system SHALL construct an isolated interactive prototype at `frontend/pages/playground/temp.vue` rendering:
1. **Quiz Arena Studio View**: Question prompt, Shiki-highlighted code block, 4 `OptionCard.vue` states (`default`, `selected`, `correct`, `incorrect`), and companion telemetry dock (live countdown timer, streak multiplier, question progress map).
2. **Flashcard 3D Practice Studio View**: 3D flip card player with front/back transitions, SM-2 grading button bar (`[1] Blackout`, `[2] Hard`, `[3] Good`, `[4] Easy`), and companion dock (session progress, SM-2 metrics card, hotkeys guide).
3. **Headless Verification Gate**: The system SHALL capture 1080p screenshots in both Light Mode and Dark Obsidian Mode and require user review and explicit approval before applying changes to production components.

#### Scenario: Prototyping interactive practice in playground
- **WHEN** the agent develops the Phase 2 prototype
- **THEN** the prototype is accessible at `http://localhost:3000/playground/temp` with mock datasets without impacting production endpoints or existing Vitest test suites.

### Requirement: Canvas Layout Archetype
The web frontend SHALL define the `CanvasLayout` archetype for full-bleed exploratory and interactive spatial surfaces (such as the Knowledge Graph Explorer and Roadmap Mindmap Canvas):

1. **Geometry & Viewport Bounding**:
   - The canvas container SHALL utilize dynamic viewport units (`h-[calc(100dvh-4rem)]` or responsive height bounds) to prevent viewport clipping and double scrollbars when browser bars auto-hide on mobile devices.
   - The primary canvas rendering layer SHALL occupy 100% width and height (`w-full h-full relative overflow-hidden select-none`).
2. **Floating HUD Layer Architecture**:
   - Floating control decks (search inputs, zoom/fit controls, filter pills, mode switches) SHALL be anchored using standard z-index layers:
     - `z-10`: Background canvas watermarks and locator minimaps.
     - `z-20`: Floating HUD control bars and legend decks, utilizing `pointer-events-none` on container wrappers and `pointer-events-auto` on interactive button pods.
     - `z-30`: Loading and error state overlays with backdrop blur.
     - `z-40`: Responsive slide-over detail inspection drawers.
     - `z-50`: Top-level modal dialogs and dropdown menus.
3. **Responsive Inspection Docking**:
   - On desktop viewports (>= 768px), node and milestone telemetry SHALL render as a slide-over panel anchored to the right edge (`w-80 md:w-96`), preserving unobstructed view of the central canvas nodes.
   - On mobile viewports (< 768px), telemetry SHALL render as a bottom-sheet drawer with safe-area padding and drag-dismiss/touch-dismiss gestures.

#### Scenario: User navigates interactive canvas on desktop
- **WHEN** user interacts with a canvas surface adhering to the `CanvasLayout` archetype
- **THEN** floating HUD control bars remain accessible without overlapping central canvas nodes
- **AND** panning or zooming within the canvas does not trigger parent page scrolling.

#### Scenario: Mobile viewport drawer inspection
- **WHEN** user selects a node or card on a mobile screen (< 768px)
- **THEN** the inspect panel renders as an elevated bottom-sheet drawer
- **AND** floating legend or minimap widgets yield visual priority to the drawer without collisions.
