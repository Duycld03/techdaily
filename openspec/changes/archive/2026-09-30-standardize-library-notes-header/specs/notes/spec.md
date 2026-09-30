# Spec Delta

## MODIFIED Requirements

### Requirement: Technical Notes Board Layout Integration
The technical notes archive interface (`frontend/pages/notes.vue`) SHALL implement the `BoardLayout` archetype (`BoardLayout.vue`) in **flat open-canvas mode** (`:flat="true"`), rendering directly onto the obsidian background canvas (`bg-slate-50 dark:bg-canvas`) without an enclosing outer `.glass-card` container ("card tổng"), structured in a standardized 2-tier header and filter architecture:

1. **Flat Open-Canvas Shell**:
   - The root layout SHALL NOT enclose the page sections within a parent `.glass-card` container or force an artificial overflow scroll box.
   - The page header, search and filter bars, notes content grid, and pagination controls SHALL flow naturally within `max-w-7xl mx-auto space-y-4`, preventing double-card nesting and eliminating artificial black margins.

2. **Standardized Header Slot (`#header`)**:
   - The top header bar SHALL follow the platform 2-tier catalog layout standard:
     - **Left**: Icon badge (`w-10 h-10 rounded-2xl bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20 flex items-center justify-center shrink-0`) enclosing `<Highlighter class="w-5 h-5 sm:w-6 sm:h-6" :stroke-width="1.5" />`, primary title (`text-xl sm:text-2xl font-black tracking-tight text-slate-900 dark:text-white`), and subtitle (`text-xs sm:text-sm text-slate-500 dark:text-slate-400 font-medium`).
     - **Right**: Full-text search input (`h-10 rounded-xl`) with shortcut hint (`⌘K`), filling the top-right header area and eliminating empty voids on desktop viewports.

3. **Dedicated Filters Slot (`#filters`)**:
   - Houses the horizontal scrollable tag filter chips (`Tất cả (N)`, `#tag1`, `#tag2`...) spanning the full container width (`w-full`), completely separated from the search input to eliminate layout crowding and prevent tag wrapping collisions.

4. **Content Grid Slot (`#content`)**:
   - Renders saved technical highlights in a responsive auto-flowing grid: 1 column on mobile, 2 columns on tablets/small laptops (`md:grid-cols-2`), and 3 columns on standard desktop viewports (`xl:grid-cols-3 gap-4`).
   - Each individual note item SHALL render as its own self-contained `.glass-card`.

5. **Pagination Slot (`#pagination`)**:
   - Houses the paginated navigation controls (`BasePagination.vue`).

#### Scenario: Browsing Technical Highlights on Desktop
- **WHEN** an engineer views their saved highlights on a 1920x1080 display
- **THEN** highlights render as compact, structured cards distributed evenly across a 2-to-3 column grid spanning the available container width, with search and tag filters pinned at the top.

#### Scenario: Browsing Technical Highlights on Open Canvas
- **WHEN** an engineer views their saved highlights on `/notes`
- **THEN** the page renders directly on the obsidian canvas without an enclosing outer `.glass-card` shell
- **AND** the individual note cards sit as first-class elevation cards on the canvas
- **AND** zero double-card nesting occurs.

#### Scenario: Standardized 2-Tier Header and Full-Width Tag Filters on Notes
- **WHEN** an engineer navigates to `/notes` on desktop or mobile
- **THEN** Row 1 renders the icon container (`w-10 h-10 rounded-2xl`), bold heading (`text-xl sm:text-2xl font-black`), and search input (`h-10 rounded-xl`) aligned in the header slot
- **AND** Row 2 dedicates the full container width to the tag filter chips without being squeezed by the search input.
