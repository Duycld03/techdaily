# Design: Frontend UI Phase 4 - Exploratory & Visual Canvases

## Context

See `proposal.md - Why` for motivation.

TechDaily features two core exploratory visual surfaces:
1. **Roadmap Learning Paths (`frontend/pages/roadmap.vue`)**: Houses both a sequential milestone timeline track and an interactive SVG/HTML hierarchical mindmap canvas (`RoadmapMindmapCanvas.vue`). Currently, the mindmap uses fixed pixel heights (`h-[620px] sm:h-[720px]`), which either underfills large 1080p/2K desktop monitors or causes nested vertical scrollbars on compact screens.
2. **Knowledge Graph Explorer (`frontend/pages/graph.vue`)**: Houses a full-bleed 2D Cytoscape and 3D Three.js WebGL canvas with floating HUD controls (`GraphControlBar.vue`), locator minimap (`GraphMinimap.vue`), interactive legend (`GraphLegend.vue`), and inspect drawer (`GraphDetailDrawer.vue`). Currently, the root container uses `calc(100vh - 4rem)` which drifts when mobile browser navigation bars collapse.

## Goals / Non-Goals

**Goals:**
- Formalize the `CanvasLayout` archetype establishing full-bleed viewport bounding and z-index hierarchy.
- Upgrade canvas height bounds to modern dynamic viewport units (`100dvh`) with standard fallbacks.
- Establish clean HUD docking and z-index separation (`z-10` minimap, `z-20` controls, `z-30` overlays, `z-40` inspect drawer).
- Ensure zero gesture collision: canvas panning and wheel zooming must never trigger parent page scroll or hijack HUD button clicks.
- Harmonize visual density and design tokens across dark and light themes without clipped control bars.

**Non-Goals:**
- Rewriting Cytoscape layout algorithms (CoSE-Bilkent, concentric, circle) or Three.js force simulations.
- Altering the backend `GET /api/v1/graph` or `GET /api/v1/curriculum/roadmap` API contracts.
- Modifying tree computation heuristics in `roadmapTreeLayout.ts`.

## Decisions

### Decision 1: Standardize Canvas Height on Dynamic Viewport Units (`100dvh`)
In `graph.vue` and `RoadmapMindmapCanvas.vue`, replace legacy `100vh` sizing with responsive `100dvh` equations:
- `graph.vue`: `h-[calc(100vh-4rem)] h-[calc(100dvh-4rem)]`
- `RoadmapMindmapCanvas.vue`: `h-[520px] sm:h-[640px] lg:h-[calc(100dvh-18rem)] lg:min-h-[640px] lg:max-h-[860px]`
- **Rationale:** Prevents address bar jump on iOS Safari and Chrome Android while providing generous vertical immersion on 1080p and 2K desktop displays.

### Decision 2: Z-Index Layering and Pointer-Events Clearance Standard
Enforce unified layer architecture for full-bleed canvas layouts:
- `z-0`: Base canvas rendering elements (Cytoscape DOM, Three.js WebGL canvas, SVG mindmap tree).
- `z-10`: Minimap, watermark, and subtle locator aids.
- `z-20`: Floating HUD control decks (`GraphControlBar`, `GraphLegend`, mindmap search & zoom toolbar). Containers use `pointer-events-none`, and interactive control cards use `pointer-events-auto`.
- `z-30`: Loading overlays, error states, and empty filter guidance panels with backdrop blur (`backdrop-blur-md`).
- `z-40`: Detail inspection drawers (`GraphDetailDrawer.vue`, milestone details) with smooth slide-over transitions.
- `z-50`: Top-level popovers, modals, and track switcher dropdown menus.

### Decision 3: Touch & Gesture Isolation
On all canvas surfaces:
- Canvas root elements apply `select-none touch-none` with wheel/touch handlers.
- All floating HUD decks, buttons, and popovers attach `@mousedown.stop`, `@touchstart.stop`, and `@wheel.stop` to ensure control interactions never inadvertently trigger canvas panning or zooming.

### Decision 4: Responsive Mobile Inspection Docking
- On desktop viewports (>= 768px), inspect drawers render as right slide-over panels (`w-80 md:w-96`), leaving the central canvas accessible.
- On mobile viewports (< 768px), inspect drawers render as bottom sheets with safe-area insets (`env(safe-area-inset-bottom)`), while secondary widgets (like minimap) auto-hide to preserve touch area.

## Risks / Trade-offs

- **Risk:** Variable viewport heights on desktop browsers with complex bookmark bars.
  - **Mitigation:** Clamping with `min-h-[640px]` and `max-h-[860px]` prevents mindmap canvases from either collapsing into unusable strips or stretching to absurd lengths.
- **Risk:** Z-index conflict between track switcher dropdown and mindmap floating toolbar.
  - **Mitigation:** The header track switcher uses `z-50` when open, ensuring its dropdown popover hovers cleanly above mindmap HUD controls.
