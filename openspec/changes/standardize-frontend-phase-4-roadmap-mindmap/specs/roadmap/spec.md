# Spec Delta

## MODIFIED Requirements

### Requirement: Interactive Mindmap SVG Shallow Reactivity and Touch Viewport Containment
The Roadmap Mindmap Canvas (`RoadmapMindmapCanvas.vue`) and Dual-View Switcher (`RoadmapViewSwitcher.vue`) SHALL utilize `shallowRef` for tree node hierarchies to eliminate proxy traversal overhead, support touch pinch-to-zoom and pan gestures on mobile viewports $< 768\text{px}$, and preserve zero-shift border transitions.

#### Scenario: Mobile Mindmap Touch Pan and Zoom Gestures
- **WHEN** user touches and drags on the SVG mindmap canvas on a mobile viewport ($< 768\text{px}$)
- **THEN** the canvas SHALL pan smoothly across coordinates without triggering unwanted page scrolling
- **AND** touch pinch gestures SHALL adjust the SVG scale matrix within bounded zoom levels ($0.4\times$ to $2.5\times$).

#### Scenario: D3 Hierarchy Reactivity Performance
- **WHEN** curriculum tracks with hundreds of milestone nodes are loaded into `RoadmapMindmapCanvas.vue`
- **THEN** the hierarchical layout and node tree SHALL be stored in a `shallowRef` to prevent Vue deep reactivity proxy traps.

#### Scenario: Dual-View Switcher Zero-Shift Border Geometry
- **WHEN** user toggles between "Timeline" and "Mindmap" views in `RoadmapViewSwitcher.vue`
- **THEN** the active button indicator SHALL maintain identical border box dimensions (`border border-transparent` vs `border border-white/[0.12]`) with zero layout shift.
