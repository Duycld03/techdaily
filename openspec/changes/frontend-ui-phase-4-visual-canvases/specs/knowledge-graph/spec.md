# Spec Delta: knowledge-graph

## ADDED Requirements

### Requirement: Knowledge Graph Explorer Canvas & HUD Clearance Standards
The Knowledge Graph Explorer interface at `/graph` SHALL enforce full-bleed visual canvas bounding and responsive control clearance:

1. **Dynamic Viewport Height Bounding**: The root explorer container SHALL utilize `h-[calc(100dvh-4rem)]` to ensure seamless full-height visualization without vertical double-scrollbars across desktop, tablet, and mobile devices.
2. **HUD Control Bar Anchoring**: The top glassmorphic control bar (`GraphControlBar.vue`) SHALL remain anchored at `top-3 sm:top-4`, constrained to `max-w-4xl mx-auto`, with responsive button padding and touch targets preventing collision with browser edges.
3. **Legend & Minimap Clearance**:
   - The interactive graph legend (`GraphLegend.vue`) SHALL dock at the bottom-left (`bottom-[calc(1.25rem+env(safe-area-inset-bottom))] left-3 sm:left-5`) with `pointer-events-none` container and `pointer-events-auto` content.
   - The locator minimap (`GraphMinimap.vue`) SHALL dock at the bottom-right (`bottom-[calc(1rem+env(safe-area-inset-bottom))] right-4`) on desktop/tablet (>= 640px) and remain hidden on small mobile viewports to prevent viewport clutter.
4. **Slide-Over Detail Drawer Integration**: The node detail inspection drawer (`GraphDetailDrawer.vue`) SHALL render with backdrop blur and responsive docking without colliding with the top control bar or bottom legend.

#### Scenario: Full-bleed graph display on high-DPI displays
- **WHEN** user opens `/graph` on a 1080p, 2K, or high-DPI monitor
- **THEN** the Cytoscape 2D and Three.js 3D canvases fill the entire available viewport height without page scrolling
- **AND** floating control decks remain cleanly anchored above the canvas.

#### Scenario: Responsive legend docking on mobile viewports
- **WHEN** user interacts with `/graph` on a mobile device (< 640px)
- **THEN** the minimap is hidden to preserve screen real estate
- **AND** the legend bar respects safe area insets without overlapping system navigation gestures.
