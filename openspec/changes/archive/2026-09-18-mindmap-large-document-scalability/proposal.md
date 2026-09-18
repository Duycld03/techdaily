# Proposal: Scalable Mindmap Architecture for Large Technical Documents

## Why
When engineers study massive technical documentation on TechDaily (such as ASP.NET Core 10, .NET API references, or multi-topic system guides containing 30–100 chapters and hundreds of slices), the client-side Mindmap (`/roadmap`) breaks down:
1. **Root Node Drift:** Root Y is computed as the midpoint between the first and last chapter, displacing the root node thousands of pixels down and completely off-screen.
2. **Micro-Text Clamping:** `fitToScreen` compresses large bounding boxes to the 25% minimum scale on mount, rendering text illegible (2–3px).
3. **Single-Slice Fragmentation:** Chunk title parsing without delimiters produces 50+ single-slice chapters.
4. **DOM/SVG Overload:** Expanding all chapters mounts hundreds of `<foreignObject>` tags simultaneously.
5. **No Mindmap Search or Focus:** Users must manually pan across massive canvases to find today's active slice.

This change transforms the Mindmap into a scalable, high-density visualization engine.

## What Changes
1. **Intelligent Chapter De-fragmentation & Grouping:** Heuristic chunk clustering in `frontend/pages/roadmap.vue` and `frontend/utils/roadmapTreeLayout.ts` that consolidates standalone, single-slice chunks into cohesive modules (3–6 slices per chapter) when titles lack delimiters.
2. **Windowed / Active-Weighted Root Node Anchoring:** Dynamic calculation of `rootY` anchored to the active chapter and visible window rather than a global midpoint across hundreds of collapsed nodes.
3. **Auto-Accordion Mode for Large Documents:** When total chapters exceed 12, auto-collapse previous branches upon opening a new chapter to cap total canvas height within comfortable viewport bounds (800–1,500px).
4. **1-Click "Focus Active / Today" (🎯) & Smart Initial Viewport:** Center the active chapter and today's slice at 100% scale (`scale: 1.0`) on mount instead of shrinking the entire document to 25%, with a 1-click toolbar button to re-center at any time.
5. **In-Canvas Interactive Search & Branch Filtering:** Live search input in the Mindmap toolbar that auto-expands matching chapter branches, highlights matching slices, and dims non-matching nodes.
6. **Zoom Scaling Range & Performance Guardrails:** Broaden zoom bounds (0.15x–2.0x), optimize bezier curve rendering, and prevent unbounded layout blowout.

## Capabilities

### Modified Capabilities
- `roadmap`: Extends the `Hierarchical Mindmap Interactive View` capability with requirements for large-document chapter grouping, windowed root anchoring, auto-accordion mode, active slice viewport focusing, and in-canvas keyword search filtering.

## Impact
- **Frontend Codebase:**
  - `frontend/components/roadmap/RoadmapMindmapCanvas.vue`: Search bar, focus active button, auto-accordion toggle, refined initial pan/zoom.
  - `frontend/utils/roadmapTreeLayout.ts`: Dynamic root anchoring, search match metadata, layout height bounding.
  - `frontend/pages/roadmap.vue`: Intelligent chapter grouping heuristics for crawled/uploaded docs without colon delimiters.
  - `frontend/i18n/locales/en.json` & `vi.json`: Localization keys for search, focus active, and auto-accordion.
- **Zero VPS Overhead Invariant:** 100% client-side changes; 0 backend modifications, 0 database queries, 0 VPS memory overhead.
