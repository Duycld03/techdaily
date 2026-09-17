# Design: Roadmap & Mindmap Dual-View Switcher

## Context

See `proposal.md` for background, user experience rationale, and conceptual tension between linear milestone execution and hierarchical mental-model mastery.

On TechDaily, `/roadmap` (`frontend/pages/roadmap.vue`) provides software engineers with a macro visualization of their learning trajectory. While the linear milestone timeline is effective for day-to-day execution, it lacks a structural representation showing how chapters, modules, and individual slices form a cohesive technical knowledge graph.

This design introduces a **Dual-View Switcher** on `/roadmap` that toggles between:
1. **Timeline View:** The existing linear chapter milestone accordions, daily slices, search filters, and progress metrics.
2. **Mindmap View:** An interactive, client-side hierarchical tree visualization rendering the root track, chapter branches, and slice leaves with full zoom, pan, collapse/expand, and 1-click action bridges.

The entire mindmap capability operates 100% on the client using SVG vector rendering and existing Pinia store data, adhering strictly to the **Zero VPS Overhead** invariant.

---

## Goals / Non-Goals

### Goals
- **Dual-View Switcher Component (`RoadmapViewSwitcher.vue`):** Provide a segmented control allowing immediate toggling between `'timeline'` and `'mindmap'` views, with user preference persisted in `localStorage`.
- **Hierarchical Mindmap Canvas (`RoadmapMindmapCanvas.vue`):** Render an interactive, vector-based tree layout displaying Root Track -> Chapter Branches -> Slice Leaves.
- **Interactive Controls:** Equip the mindmap with Zoom In, Zoom Out, Fit to Screen, Pan, and Batch Expand/Collapse All Branches.
- **Node Status Visuals:** Visually differentiate slices by status: Completed (Emerald Green), Active Today (Amber/Gold with pulse animation), and Upcoming (Neutral Slate).
- **1-Click Action Bridges:** Enable direct navigation from slice leaves to `/today` (for active drills) or `/read/[bookId]?slice={chunkOrder}` (for full-text reading).
- **Zero VPS Overhead Invariant:** 0 backend changes, 0 new API endpoints, and 0 database queries.
- **Theme Reactivity & Invariant 37 Compliance:** Complete reactivity to Dark/Light mode, and strict enforcement of `whitespace-nowrap shrink-0` across English and Vietnamese.

### Non-Goals
- Modifying backend PostgreSQL schemas, EF Core models, or C# minimal APIs.
- Storing node canvas coordinates or branch expand/collapse states in the backend database.
- Enabling drag-and-drop node reordering or manual tree restructuring.
- Modifying the reader (`/read/[bookId]`) or daily focus (`/today`) core logic.

---

## Architecture & Component Breakdown

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      ROADMAP DUAL-VIEW ARCHITECTURE                         │
│                                                                             │
│  frontend/pages/roadmap.vue                                                 │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ Unified Header Banner (Track Title, Active Badge, Global Progress)    │  │
│  └───────────────────────────────────┬───────────────────────────────────┘  │
│                                      │                                      │
│                                      ▼                                      │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │ RoadmapViewSwitcher.vue                                               │  │
│  │ [ 📋 Timeline View (active) | 🌳 Mindmap View ]                        │  │
│  │ LocalStorage Sync: 'techdaily_roadmap_view_mode'                      │  │
│  └───────────────────┬───────────────────────────────────┬───────────────┘  │
│                      │                                   │                  │
│         viewMode === 'timeline'             viewMode === 'mindmap'          │
│                      │                                   │                  │
│                      ▼                                   ▼                  │
│  ┌─────────────────────────────────┐   ┌─────────────────────────────────┐  │
│  │ Linear Timeline Milestone View  │   │ RoadmapMindmapCanvas.vue        │  │
│  │ • Chapter Accordions            │   │ ┌─────────────────────────────┐ │  │
│  │ • Chapter Search Filter         │   │ │ Floating Control Toolbar    │ │  │
│  │ • Sequential Slice Cards        │   │ │ [+][-][Fit][Expand][Collapse│ │  │
│  │ • Batch Accordion Controls      │   │ └──────────────┬──────────────┘ │  │
│  │ • Direct Drill Jump Bridges     │   │                │                │  │
│  └─────────────────────────────────┘   │ ┌──────────────▼──────────────┐ │  │
│                                        │ │ Interactive SVG Viewport    │ │  │
│                                        │ │  • Pan & Zoom Transform     │ │  │
│                                        │ │  • Cubic Bezier Connectors  │ │  │
│                                        │ │  • Root Node (Book/Track)   │ │  │
│                                        │ │  • Chapter Branch Nodes     │ │  │
│                                        │ │  • Slice Leaf Nodes         │ │  │
│                                        │ └──────────────┬──────────────┘ │  │
│                                        │                │                │  │
│                                        │ ┌──────────────▼──────────────┐ │  │
│                                        │ │ Slice Node Action Bridge    │ │  │
│                                        │ │ [Today Drill] [Read Slice]  │ │  │
│                                        │ └─────────────────────────────┘ │  │
│                                        └─────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Technical Decisions & Implementation Specs

### 1. Rendering Engine: Declarative SVG Tree vs. HTML5 Canvas vs. Cytoscape.js

| Criterion | SVG-Based Declarative Tree | HTML5 Canvas 2D | Cytoscape.js |
|---|---|---|---|
| **Rendering Crispness** | Infinite vector clarity on Retina displays | Pixelated unless manual DPI scaling applied | Depends on canvas/webgl renderer |
| **DOM Event Handling** | Native `@click`, `@mouseenter`, tooltips | Manual coordinate hit-testing required | Custom event listener abstraction |
| **Bundle Size Overhead** | **0 KB** (Native Vue template) | **0 KB** (Native browser API) | ~350 KB bundle overhead |
| **Dark Mode Reactivity** | Native Tailwind `dark:` classes & transitions | Manual redraw loop on theme toggle | Manual stylesheet re-injection |
| **Performance (<500 Nodes)** | Extremely smooth (60 FPS CSS transforms) | Very fast | Good, but heavier memory footprint |

**Decision:** Implement the mindmap using **declarative SVG vector rendering** wrapped with CSS hardware-accelerated transforms (`transform: translate(x, y) scale(s)`). Technical books on TechDaily contain 5–40 chapters and 20–250 slices. SVG easily handles this node count while delivering crisp typography, seamless Tailwind styling, and zero bundle bloat.

---

### 2. Client-Side Hierarchical Tree Layout Algorithm

The mindmap organizes nodes in a **Horizontal Tidy Tree** layout with three distinct hierarchical tiers:
1. **Tier 0: Root Track Node** (Active Book or Senior Curriculum)
2. **Tier 1: Chapter Branch Nodes** (Expandable/Collapsible)
3. **Tier 2: Slice Leaf Nodes** (Terminal reading or challenge units)

#### Coordinate Calculation:
- **Horizontal Depth Spacing ($X$):**
  - Root Node: $X_{\text{root}} = 80$
  - Chapter Nodes: $X_{\text{chapter}} = 440$
  - Slice Leaf Nodes: $X_{\text{slice}} = 860$
- **Vertical Placement ($Y$):**
  - Leaf nodes within an expanded chapter are spaced vertically by $H_{\text{leaf}} = 54\text{px}$ with a vertical gap of $12\text{px}$.
  - A collapsed chapter occupies an effective branch height of $H_{\text{chapter}} = 64\text{px}$.
  - An expanded chapter occupies an effective branch height of:
    $$H_{\text{branch}} = \max(H_{\text{chapter}}, N_{\text{slices}} \times (H_{\text{leaf}} + \text{gap}))$$
  - Chapter branch centers $Y_{\text{chapter}}$ are stacked sequentially with branch margins ($24\text{px}$).
  - The Root Node is centered vertically at:
    $$Y_{\text{root}} = \frac{Y_{\text{first\_chapter}} + Y_{\text{last\_chapter}}}{2}$$

#### Connector Edges (Cubic Bezier Curves):
Connector paths between parent node $(x_1, y_1)$ and child node $(x_2, y_2)$ use smooth horizontal cubic Bezier curves:
```ts
function computeBezierPath(x1: number, y1: number, x2: number, y2: number): string {
  const dx = (x2 - x1) * 0.5
  return `M ${x1} ${y1} C ${x1 + dx} ${y1}, ${x2 - dx} ${y2}, ${x2} ${y2}`
}
```

---

### 3. View Switcher & State Management

#### View Mode Persistence:
```ts
export type RoadmapViewMode = 'timeline' | 'mindmap'

const STORAGE_KEY = 'techdaily_roadmap_view_mode'

export function useRoadmapViewMode() {
  const viewMode = ref<RoadmapViewMode>('timeline')

  onMounted(() => {
    if (import.meta.client) {
      const stored = localStorage.getItem(STORAGE_KEY)
      if (stored === 'timeline' || stored === 'mindmap') {
        viewMode.value = stored
      }
    }
  })

  function setViewMode(mode: RoadmapViewMode) {
    viewMode.value = mode
    if (import.meta.client) {
      localStorage.setItem(STORAGE_KEY, mode)
    }
  }

  return { viewMode, setViewMode }
}
```

#### Segmented Switcher UI (`RoadmapViewSwitcher.vue`):
```html
<div class="inline-flex items-center p-1.5 bg-slate-100 dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl shadow-inner">
  <button
    type="button"
    role="tab"
    :aria-selected="modelValue === 'timeline'"
    @click="$emit('update:modelValue', 'timeline')"
    :class="[
      'inline-flex items-center gap-2 px-4 py-2 rounded-xl text-xs sm:text-sm font-bold transition-all whitespace-nowrap shrink-0',
      modelValue === 'timeline'
        ? 'bg-white dark:bg-slate-800 text-brand-600 dark:text-brand-400 shadow-sm'
        : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
    ]"
  >
    <ListOrdered class="w-4 h-4 shrink-0" />
    <span>{{ $t('roadmap.timeline_view') }}</span>
  </button>

  <button
    type="button"
    role="tab"
    :aria-selected="modelValue === 'mindmap'"
    @click="$emit('update:modelValue', 'mindmap')"
    :class="[
      'inline-flex items-center gap-2 px-4 py-2 rounded-xl text-xs sm:text-sm font-bold transition-all whitespace-nowrap shrink-0',
      modelValue === 'mindmap'
        ? 'bg-white dark:bg-slate-800 text-brand-600 dark:text-brand-400 shadow-sm'
        : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
    ]"
  >
    <GitFork class="w-4 h-4 shrink-0" />
    <span>{{ $t('roadmap.mindmap_view') }}</span>
  </button>
</div>
```

---

### 4. Interactive Viewport & Canvas Controls

#### Transform State:
- `scale = ref(1.0)`: Clamped between `0.25` and `2.0`.
- `pan = ref({ x: 40, y: 40 })`: 2D offset in pixels.
- `isPanning = ref(false)`: Activated via mousedown on canvas background or touch move.

#### Viewport Toolbar Actions:
- **Zoom In:** `scale.value = Math.min(2.0, scale.value + 0.15)`
- **Zoom Out:** `scale.value = Math.max(0.25, scale.value - 0.15)`
- **Fit to Screen:** Calculates the bounding box of all visible nodes and sets:
  ```ts
  const scaleX = (containerWidth - 100) / treeBoundingBox.width
  const scaleY = (containerHeight - 100) / treeBoundingBox.height
  scale.value = Math.min(Math.max(Math.min(scaleX, scaleY), 0.35), 1.1)
  pan.value = {
    x: 50 - treeBoundingBox.minX * scale.value,
    y: (containerHeight - treeBoundingBox.height * scale.value) / 2 - treeBoundingBox.minY * scale.value
  }
  ```
- **Expand All:** Adds all chapter IDs to `expandedChapterIds.value`.
- **Collapse All:** Clears `expandedChapterIds.value` (or leaves only the active chapter expanded).

---

### 5. Node Styling by Completion Status

```ts
export type NodeStatus = 'completed' | 'active_today' | 'upcoming'
```

| Node Status | Border & Background Styling | Indicator Icon / Animation |
|---|---|---|
| **Completed** | `border-emerald-500/40 bg-emerald-50/80 dark:bg-emerald-950/40 text-emerald-900 dark:text-emerald-100` | CheckCircle2 (emerald-500) |
| **Active Today** | `border-amber-500 bg-amber-50/90 dark:bg-amber-950/50 text-amber-950 dark:text-amber-100 ring-2 ring-amber-500/40` | Flame (amber-500) + Ping Animation |
| **Upcoming** | `border-slate-200 dark:border-slate-800 bg-white/80 dark:bg-slate-900/80 text-slate-700 dark:text-slate-300` | Clock or Lock (slate-400) |

---

### 6. 1-Click Action Bridge & Quick Preview

Clicking a Slice leaf node triggers a contextual action popover or direct route navigation:
- **Active Today Slice:** Directly routes to `/today?bookId={bookId}&chunkOrder={chunkOrder}` (or `/today?day={dayOrder}`).
- **Completed Slice:** Opens quick modal with slice summary, read duration, and dual action buttons:
  - `[ Xem lại Thử Thách AI ⚡ ]` -> `/today?bookId={bookId}&chunkOrder={chunkOrder}`
  - `[ Đọc lại Bài Học 📖 ]` -> `/read/{bookId}?slice={chunkOrder}`
- **Upcoming Slice:** Opens quick modal preview with button:
  - `[ Xem trước Nội Dung 📖 ]` -> `/read/{bookId}?slice={chunkOrder}`

---

### 7. Bilingual & Layout Invariants (AGENTS.md Invariant 37)

All buttons, labels, and badges must strictly enforce `whitespace-nowrap shrink-0` to avoid text collision across English and Vietnamese.

#### Translation Keys:
```json
{
  "roadmap": {
    "timeline_view": "Timeline View",
    "mindmap_view": "Mindmap View",
    "mindmap": {
      "zoom_in": "Zoom In",
      "zoom_out": "Zoom Out",
      "fit_screen": "Fit to Screen",
      "expand_all": "Expand All",
      "collapse_all": "Collapse All",
      "active_badge": "Active Today",
      "completed_badge": "Completed",
      "upcoming_badge": "Upcoming",
      "read_slice": "Read Slice",
      "review_drill": "Review Drill",
      "start_drill": "Start Today's Drill",
      "chapters_count": "{count} Chapters",
      "slices_count": "{count} Slices"
    }
  }
}
```

---

## Risks / Trade-offs

| Risk | Impact | Mitigation Strategy |
|---|---|---|
| **Large Document Scaling:** Books with 50+ chapters or 300+ slices could create visual noise or large SVG bounding boxes. | Low/Medium | By default, only the chapter containing today's active slice is expanded. All other chapters start collapsed, keeping initial tree depth compact. "Fit to Screen" dynamically calculates boundaries. |
| **Touch Interaction Collisions:** On mobile touchscreens, dragging the canvas could conflict with vertical page scrolling. | Medium | Constrain canvas drag to single-finger touches inside the canvas container with `touch-action: none`. Provide clear zoom buttons in the floating toolbar so mobile users do not depend solely on pinch gestures. |
| **Visual Flickering on View Mode Switch:** Switching between timeline and mindmap could cause layout shift. | Low | Retain the unified header banner in place; only switch the sub-container beneath the header. Wrap the transition with Nuxt/Vue `<Transition name="fade" mode="out-in">`. |
| **Theme Reactivity:** Changing dark/light mode could require canvas redrawing. | Low | SVG elements use standard Tailwind reactive classes (`dark:border-slate-800`, `dark:bg-slate-900`), ensuring instantaneous, zero-cost theme transitions without manual canvas re-renders. |
