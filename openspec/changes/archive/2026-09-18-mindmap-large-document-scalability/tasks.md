# Tasks: Scalable Mindmap Architecture for Large Technical Documents

## 1. Chapter De-fragmentation & Heuristics

- [x] 1.1 In `frontend/pages/roadmap.vue`, implement two-pass chunk clustering heuristics in the `chapterMilestones` computed property. Group consecutive standalone chunks lacking explicit delimiters into cohesive thematic modules of 3–5 slices each, preventing single-slice chapter explosion for large documents.
- [x] 1.2 In `frontend/pages/roadmap.vue`, ensure existing delimiter-based titles (with `:` or ` - `) continue to be cleanly grouped under their respective module prefixes without regression.
- [x] 1.3 Add unit tests in `frontend/tests/pages/roadmap.spec.ts` verifying that large document chunk sequences (e.g. 30–60 standalone chunks) consolidate into balanced chapter milestones without losing any slice data.

## 2. Windowed Root Node Anchoring & Layout Bounds

- [x] 2.1 In `frontend/utils/roadmapTreeLayout.ts`, update `computeRoadmapTreeLayout` to compute `rootY` based on the centroid of active and expanded chapter branches with layout boundary clamping, ensuring the root node remains in close visual proximity to the active study window.
- [x] 2.2 In `frontend/utils/roadmapTreeLayout.ts`, expand minimum zoom limit from 0.25x to 0.15x for extreme layouts and refine bezier curve control points for distant edges.
- [x] 2.3 In `frontend/tests/utils/roadmapTreeLayout.spec.ts`, add test cases verifying that for documents with 30+ chapters, `rootY` is positioned near the active chapter rather than the distant global midpoint.

## 3. Smart Viewport Centering & 1-Click "Focus Active" (🎯)

- [x] 3.1 In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, implement `focusActiveNode()` to calculate pan coordinates that center the active-today slice or active chapter at 1.0x scale (`scale = 1.0`).
- [x] 3.2 In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, update `onMounted` to invoke `focusActiveNode()` by default, presenting a crisp, legible view of today's learning milestone rather than a downscaled micro-view.
- [x] 3.3 In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, add a `🎯 Focus Active` button to the floating toolbar with Lucide `Target` icon and responsive tooltip.

## 4. Auto-Accordion Mode for Large Documents

- [x] 4.1 In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, implement auto-accordion logic: when chapter count exceeds 12, expanding a chapter automatically collapses other chapters unless the user explicitly triggered `Expand All`.
- [x] 4.2 In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, ensure `expandAll` and `collapseAll` toolbar buttons cleanly toggle full expansion and override auto-accordion until the next individual chapter click.
- [x] 4.3 Add component unit tests in `frontend/tests/components/roadmap/RoadmapMindmapCanvas.spec.ts` verifying single-chapter auto-accordion behavior when chapter count exceeds 12.

## 5. In-Canvas Search & Visual Filtering

- [x] 5.1 In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, add a reactive search input with a debounced query model to the top-left of the canvas container or integrated into the floating toolbar.
- [x] 5.2 In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`, implement search matching across chapter titles and slice titles/summaries: automatically expand matching chapters, apply highlighting rings to matching nodes, and set `opacity-25` on non-matching elements.
- [x] 5.3 In `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`, add translation keys for `focus_active`, `search_placeholder`, `no_matches`, and `auto_accordion`.

## 6. Verification & Automated Tests

- [x] 6.1 Run `npx vitest run frontend/tests/components/roadmap/RoadmapMindmapCanvas.spec.ts` and `frontend/tests/pages/roadmap.spec.ts` to verify all mindmap unit and integration tests pass.
- [x] 6.2 Run full frontend test suite `npm --prefix frontend test` to verify zero regressions across the application.
- [x] 6.3 Run `openspec validate mindmap-large-document-scalability` to ensure strict specification compliance across all planning artifacts.
