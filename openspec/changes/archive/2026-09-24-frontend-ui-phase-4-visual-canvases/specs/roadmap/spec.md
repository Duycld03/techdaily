# Spec Delta: roadmap

## ADDED Requirements

### Requirement: Mindmap Canvas Dynamic Bounding & Control Docking
The interactive roadmap mindmap view (`RoadmapMindmapCanvas.vue`) on `/roadmap` SHALL enforce responsive viewport height bounding and floating control clearance:

1. **Responsive Viewport Height**: The mindmap canvas container SHALL dynamically scale across viewports:
   - On mobile viewports (< 640px): `h-[520px]`.
   - On tablet/small laptop viewports (640px - 1023px): `h-[640px]`.
   - On desktop viewports (>= 1024px): `h-[calc(100dvh-18rem)] min-h-[640px] max-h-[860px]`.
   preventing awkward vertical dead zones or excessive page scrolling.
2. **Floating Control Deck Layout**:
   - The mindmap search input pod SHALL dock in the top-left corner (`top-3 sm:top-4 left-3 sm:left-4 z-10`) with `max-w-[180px] sm:max-w-[240px] md:max-w-xs`.
   - The zoom/pan/focus floating toolbar SHALL dock in the top-right corner (`top-3 sm:top-4 right-3 sm:right-4 z-10`) with glassmorphic styling (`dark:bg-canvas-subtle/90 backdrop-blur-md`).
3. **Gesture & Zoom Boundaries**:
   - Canvas wheel zoom SHALL be clamped between 0.4x and 2.5x to prevent node inversion or micro-scale loss.
   - Panning SHALL support both mouse drag and touch pinch/drag gestures with `touch-none` and pointer event isolation.
4. **Active Node Focus Feedback**: The "Focus Active" trigger SHALL smoothly pan and center the viewport on the user's active chapter and slice without disorienting layout jumps.

#### Scenario: User resizes browser window on mindmap view
- **WHEN** user resizes the browser window while viewing the interactive mindmap on `/roadmap`
- **THEN** the canvas container height dynamically adjusts to the viewport height without clipping controls or creating nested page scrollbars.

#### Scenario: Floating controls remain accessible during panning
- **WHEN** user pans across the hierarchical tree nodes
- **THEN** the top-left search input and top-right zoom controls remain pinned in place
- **AND** interaction with control buttons does not trigger canvas panning.
