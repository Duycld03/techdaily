# Design: Scalable Mindmap Architecture for Large Technical Documents

## Context
TechDaily's `/roadmap` page features a dual-view switcher (`[ 📋 Timeline View | 🌳 Mindmap View ]`) powered by client-side SVG vector rendering. While this design works seamlessly for standard curriculums (like the 30-Day Senior Fullstack Curriculum with 5 pillars), real-world technical documentation ingested via `/library` (such as the ASP.NET Core 10 Architecture Guide, Microsoft.Win32 Namespace reference, or distributed system monographs) introduces 30–100 chapters and hundreds of chunks.

Under these large-scale documents, the initial mindmap implementation suffers from:
1. **Vertical Space Explosion:** 50–60 chapters create a layout height of 5,000px–15,000px.
2. **Root Node Disappearance:** Centering Root Y globally at `(firstY + lastY) / 2` pushes the root node 3,000px–7,000px down, leaving the initial viewport empty of its primary anchor.
3. **Illegible Auto-Fit:** Clamping `fitToScreen` to 25% zoom shrinks text to 2–3px unreadable micro-text.
4. **Single-Slice Chapter Fragmentation:** Naive title delimiter splitting creates dozens of isolated 1-slice chapters when source chunks lack colons or hyphens.
5. **Lack of Discovery Tools:** No in-canvas search or active-slice focus mechanism.

---

## Goals & Non-Goals

### Goals
- **Graceful High-Density Layout:** Cap canvas height within 800px–1,600px for large documents through intelligent auto-accordion branch expansion.
- **De-fragmented Chapter Heuristics:** Consolidate standalone delimiter-less chunks into logical 3–6 slice modules, eliminating 1-slice chapter spam.
- **Dynamic Root Node Anchoring:** Anchor Root Y to the active/expanded branch window so the root node is always visually connected to the user's active reading context.
- **Active-First Initial Viewport:** Center the active chapter and today's slice at 100% scale (`scale: 1.0`) on mount and provide a 1-click `🎯 Focus Active` toolbar button.
- **Interactive In-Canvas Search:** Instant keyword filtering that auto-expands matching branches, highlights matching slices, and dims non-matching nodes.
- **Zero VPS Overhead Invariant:** Maintain 100% client-side computation with 0 backend changes.

### Non-Goals
- Modifying backend PostgreSQL schemas or API endpoints.
- Altering the linear timeline view's core data structure.
- Adopting heavy third-party canvas or WebGL dependencies (maintain lean Vue/SVG vector pipeline).

---

## Key Decisions & Architectural Changes

### 1. Two-Pass Chapter De-fragmentation Heuristics (`roadmap.vue`)
Currently, `parseSliceTitle` only groups chunks when `chunk.chapterTitle` contains a colon `:` or hyphen ` - ` with prefix length $\le 45$. Standalone titles (`Overview`, `Get started`, `Static files`, `Middleware`) each spawn a 1-slice chapter.

**Decision:** Implement a two-pass grouping heuristic:
- **Pass 1 (Explicit Prefix Grouping):** Identify common prefixes delimited by `:` or ` - `.
- **Pass 2 (Adjacent Standalone Clustering):** When consecutive chunks lack a delimiter and are under 10 minutes read time each, cluster them into thematic modules of 3–5 slices (e.g. *"Core Concepts & Architecture (Part 1)"*, *"Runtime Fundamentals"*), capped at 5 slices per module.
This reduces a 60-chapter fragmentation down to 12–16 manageable chapters.

```
[Before: Naive Split]                [After: Two-Pass Heuristic]
- Ch 1: Overview (1 slice)            ┌─ Module 1: Overview & Getting Started (3 slices)
- Ch 2: Get started (1 slice)        │  ├─ Overview
- Ch 3: Project structure (1 slice)   │  ├─ Get started
- Ch 4: Static files (1 slice)        │  └─ Project structure
- Ch 5: Middleware (1 slice)         └─ Module 2: Fundamentals & Pipeline (4 slices)
... (50+ isolated chapters)             ├─ Static files
                                        ├─ Middleware
                                        ...
```

---

### 2. Windowed Root Node Anchoring (`roadmapTreeLayout.ts`)
Instead of calculating `rootY` across the entire bounding box of all chapters:
$$\text{rootY} = \frac{\text{firstChapterCenterY} + \text{lastChapterCenterY}}{2} - \frac{\text{rootHeight}}{2}$$

**Decision:** Anchor `rootY` to the vertical centroid of the **expanded and active chapters**:
```ts
// If active/expanded chapters exist, anchor Root Y to their vertical center
const focusChapters = positionedChapters.filter(c => isExpanded(c) || c.data.isActive)
const anchorY = focusChapters.length > 0
  ? focusChapters.reduce((acc, c) => acc + c.y + c.height / 2, 0) / focusChapters.length
  : (firstChapterCenterY + lastChapterCenterY) / 2

// Clamp rootY within layout boundaries to prevent negative or extreme coordinates
const rootY = Math.max(cfg.paddingTop, Math.min(anchorY - cfg.rootHeight / 2, maxY - cfg.rootHeight - cfg.paddingBottom))
```
This ensures the Root Node stays vertically adjacent to the active chapter, and outgoing bezier curves flow horizontally rather than shooting vertically across thousands of offscreen pixels.

---

### 3. Auto-Accordion Strategy for Large Documents
When a document has $\le 12$ chapters (e.g. 30-Day Curriculum with 5 pillars), multiple chapters can be expanded simultaneously.
When a document has $> 12$ chapters:
- **Auto-Accordion Mode:** Clicking to expand Chapter $N$ automatically collapses previously opened chapters.
- **Manual Override:** The `Expand All` button remains available for users who explicitly want the entire global map, but default single-click interaction keeps the total layout height bounded to $\le 1,200\text{px}$.

---

### 4. Smart Viewport Initial Centering & 1-Click "Focus Active" (🎯)
Currently, `onMounted` calls `fitToScreen()`, compressing large trees down to the minimum zoom of 25%.

**Decision:** Replace the default mount behavior:
1. Locate the active node (the slice with `isActiveToday === true`, or the active chapter node).
2. Calculate the pan required to place that node at the horizontal and vertical center of the container at `scale = 1.0`:
   $$\text{pan.x} = \frac{\text{containerWidth}}{2} - \text{activeNode.x} \times 1.0$$
   $$\text{pan.y} = \frac{\text{containerHeight}}{2} - \text{activeNode.y} \times 1.0$$
3. Add a dedicated `🎯 Focus Active` button to the floating toolbar, allowing users to return to today's active slice with a single click.

---

### 5. In-Canvas Mindmap Search & Node Highlighting
Add a compact, rounded search input to the mindmap floating toolbar or top-left corner:
- As the user types, matches are checked against slice titles, summaries, and chapter titles.
- All chapters containing matching slices are **automatically expanded**.
- Matching slice nodes are decorated with a glowing amber/sky ring.
- Non-matching nodes and edges are styled with `opacity-25 transition-opacity`.
- Clearing the search resets the view smoothly.

---

## Risks & Trade-offs

| Risk | Impact | Mitigation |
|---|---|---|
| **Title Clustering Inaccuracies:** Auto-clustering standalone chunks might group slightly disparate topics. | Low | Retain slice titles verbatim inside the chapter leaves; only the generated chapter header uses the clustered title. |
| **Performance with Rapid Search Typing:** Frequent layout recalculation when typing long search queries. | Low | Debounce search input by 150ms before triggering chapter expansion and tree layout recalculation. |
| **Mobile Screen Space:** Adding a search input and focus button to the floating toolbar could crowd mobile viewports. | Medium | On small viewports (`< 640px`), collapse the search bar into an expandable icon trigger and abbreviate toolbar labels to icons only (`whitespace-nowrap shrink-0`). |
