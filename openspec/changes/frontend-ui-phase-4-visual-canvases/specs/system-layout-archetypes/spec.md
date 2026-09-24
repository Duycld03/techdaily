# Spec Delta: system-layout-archetypes

## ADDED Requirements

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
