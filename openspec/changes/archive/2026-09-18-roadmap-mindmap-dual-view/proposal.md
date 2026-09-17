# Proposal: Roadmap & Mindmap Dual-View Switcher

## Why

### Executive Summary
Software engineers mastering complex technical domains (such as Distributed Systems, Storage Internals, or Cloud Architecture) operate within two distinct, complementary cognitive modes:
1. **Chronological / Milestone Execution (Linear Progression):** "What slice or challenge should I complete today? What is my daily streak, drill score, and immediate next milestone?"
2. **Structural / Mental-Model Mastery (Hierarchical Overview):** "How do these chapters and concepts interlock? What is the overarching architecture of this book or curriculum? Where does an LSM-tree or WAL fit within the broader database storage engine?"

Currently, the `/roadmap` page (`frontend/pages/roadmap.vue`) provides exclusively a linear, top-to-bottom milestone timeline. While effective for tracking sequential daily progress, it obscures the structural hierarchy and conceptual tree of multi-chapter books and curriculum tracks. Forcing users to choose only one mental model or mashing them into a cluttered hybrid interface degrades the learning experience.

We propose introducing a sleek, responsive **Dual-View Switcher** on `/roadmap`:
`[ 📋 Timeline View | 🌳 Mindmap View ]`

This gives engineers the best of both worlds: a structured chronological timeline for execution, and an interactive, client-side mindmap for spatial orientation and conceptual mapping—toggleable with a single click.

---

### Conceptual Tension: Linear Roadmap vs. Hierarchical Mindmap

| Dimension | Timeline View (Linear) | Mindmap View (Hierarchical) |
|---|---|---|
| **Primary Mental Model** | Chronological milestone progression | Conceptual structural tree & spatial topology |
| **Core Question Answered** | *"What should I learn today and what comes next?"* | *"How do all parts of this domain connect?"* |
| **Visual Structure** | Vertical accordion list with progress badges | Interactive branch-and-leaf tree with radial/horizontal coordinates |
| **Optimal Use Case** | Daily routine, completing daily drills, streak tracking | Big-picture review, architectural orientation, knowledge mapping |
| **Interaction Pattern** | Sequential scroll, chapter search, milestone drill action | Pan, zoom, branch collapse/expand, leaf jump |

---

### Business & Learning Value
1. **Accelerated Mental Model Construction:** Visualizing technical concepts as an interconnected tree helps senior engineers form durable cognitive schemas and improves knowledge retention for architecture design interviews.
2. **Reduced Cognitive Overload:** Collapsible chapter branches allow users to focus on specific architectural modules without scrolling through dozens of slice cards.
3. **Seamless Micro-to-Macro Transition:** Engineers can switch between zooming in on today's drill on `/today` and zooming out to explore the full curriculum topology on `/roadmap`.
4. **Delight & Developer Ergonomics:** Smooth canvas interactions (drag-to-pan, pinch-to-zoom, fit-to-screen) deliver a modern, tactile developer experience aligned with tools like Obsidian and Miro.

---

### Performance & VPS Zero-Overhead Invariant
The entire Mindmap visualization executes **100% on the client** (in-browser SVG/Canvas 2D tree layout). It reuses the already-loaded reactive Pinia state (`libraryStore.selectedBook`, `focusStore.data`, and `roadmapStore.roadmapData`).
- **0 Extra Backend APIs:** No new endpoints or database tables.
- **0 Extra VPS CPU/Memory Overhead:** Layout computations and rendering run locally on the client machine.
- **Instant Toggling:** Switching between Timeline and Mindmap views is instantaneous and requires zero network requests.

---

## What Changes

1. **Roadmap Dual-View Switcher (`RoadmapViewSwitcher.vue`):**
   - A prominent, accessible segmented pill control positioned directly beneath the roadmap header banner:
     `[ 📋 Timeline View | 🌳 Mindmap View ]`.
   - Preserves user preference in `localStorage` (`techdaily_roadmap_view_mode`) so returning users remain in their preferred view.
   - Enforces `whitespace-nowrap shrink-0` and responsive sizing to satisfy AGENTS.md invariant 37 across English and Vietnamese.

2. **Hierarchical Mindmap Interactive View (`RoadmapMindmapCanvas.vue`):**
   - **Root Node:** Active Document Book or 30-Day Curriculum title with an overall completion badge and metadata.
   - **Chapter Branches:** Expandable/collapsible intermediate nodes displaying chapter titles, slice progress counts, and completion color states.
   - **Slice Leaves:** Terminal nodes representing individual reading slices or daily challenges, color-coded by status:
     - **Completed (Emerald Green):** Slices already read or drills passed.
     - **Active Today (Amber/Gold with Pulse Animation):** Today's designated reading slice.
     - **Upcoming (Slate):** Future slices in the learning track.
   - **Interactive Canvas Controls:** Floating toolbar with Zoom In, Zoom Out, Fit to Screen, and Batch Expand/Collapse All Branches.
   - **Smooth Navigation Bridges:** Clicking any slice leaf opens a preview popover with 1-click jumps to `/today` (for active drills) or `/read/[bookId]?slice={chunkOrder}` (for full-text reading).
   - **Theme Reactivity:** Automatic dark/light theme switching reactive to Nuxt's `useColorMode()`.

3. **Timeline View Preservation:**
   - The existing linear chapter accordions, slice cards, search bar, and drill action bridges remain completely intact when `viewMode === 'timeline'`.

---

## Capabilities

### Modified Capabilities
- `roadmap`: Introduces requirements for `Roadmap Dual-View Switcher` and `Hierarchical Mindmap Interactive View`, enabling software engineers to seamlessly toggle between chronological milestone progression and an interactive, client-side hierarchical tree mindmap with collapsible branches, canvas navigation controls, and 1-click action bridges.

---

## Impact

- **Frontend Components:**
  - `frontend/components/roadmap/RoadmapViewSwitcher.vue` (new): Segmented toggle for switching between Timeline and Mindmap views.
  - `frontend/components/roadmap/RoadmapMindmapCanvas.vue` (new): Interactive SVG/Canvas tree layout renderer with pan, zoom, and collapsible branches.
  - `frontend/pages/roadmap.vue` (modified): Houses the switcher and conditionally mounts either the Timeline milestone list or the Mindmap canvas.
- **Frontend State & Composable:**
  - View preference persisted in `localStorage` under `techdaily_roadmap_view_mode`.
- **Frontend Localization:**
  - `frontend/i18n/locales/en.json` & `frontend/i18n/locales/vi.json`: New translation keys for switcher labels, canvas controls, and node status tooltips.
- **Frontend Test Suites:**
  - `frontend/tests/components/roadmap/RoadmapViewSwitcher.spec.ts` (new).
  - `frontend/tests/components/roadmap/RoadmapMindmapCanvas.spec.ts` (new).
  - `frontend/tests/pages/roadmap.spec.ts` (updated).
- **Backend & APIs:**
  - Zero backend changes. Zero database migrations. Zero server compute impact.

---

## Scope & Non-Goals

### In Scope
- Designing and implementing `RoadmapViewSwitcher.vue`.
- Designing and implementing `RoadmapMindmapCanvas.vue` with client-side tree layout calculations.
- Integrating both components into `frontend/pages/roadmap.vue`.
- Supporting both Active Book pacer tracks and the 30-Day Senior Curriculum track in the mindmap.
- 1-click action bridges from mindmap nodes to `/today` and `/read/[bookId]`.
- Zoom, pan, reset/fit-to-screen, and batch expand/collapse controls.
- View mode persistence via `localStorage`.
- Full localization in `en.json` and `vi.json`.
- Strict enforcement of AGENTS.md invariant 37 (`whitespace-nowrap shrink-0`).
- Automated Vitest unit and integration test suites.

### Non-Goals
- Server-side graph generation or layout computation.
- Persisting canvas coordinates or branch expand/collapse states into PostgreSQL.
- Drag-and-drop structural reorganization of chapters or slices (curriculum structure is authoritative).
- Modifying reader (`/read/[bookId]`) or daily focus (`/today`) views.
