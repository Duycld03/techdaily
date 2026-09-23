# Spec Delta

## ADDED Requirements

### Requirement: WebGL Shallow Reactivity, OrbitControls Hygiene and Mobile Bottom-Sheet Drawer
The Knowledge Graph 3D and 2D Studio (`pages/graph.vue`, `GraphCanvas3D.vue`, `GraphCanvas.vue`, `GraphDetailDrawer.vue`, `GraphControlBar.vue`) SHALL store WebGL and canvas instances strictly in `shallowRef` to prevent reactive memory overhead, manage OrbitControls event listeners via VueUse hygiene, provide an adaptive touch-swipeable bottom-sheet drawer on mobile viewports $< 768\text{px}$, and clear safe area boundaries.

#### Scenario: Three.js and ForceGraph3D Shallow Reactivity
- **WHEN** user loads the 3D Cosmos view in `GraphCanvas3D.vue`
- **THEN** the Three.js scene, camera, renderer, ForceGraph3D instance, and controls SHALL be encapsulated in `shallowRef`
- **AND** Vue deep reactivity SHALL NOT create proxies over 3D meshes, geometries, or materials.

#### Scenario: OrbitControls Event Teardown
- **WHEN** user navigates away from `pages/graph.vue` or switches between 2D and 3D modes
- **THEN** all OrbitControls listeners (`start`, `end`) and animation frame loops SHALL be safely cancelled and disposed without memory leaks.

#### Scenario: Mobile Bottom-Sheet Detail Drawer
- **WHEN** user taps a graph node on a mobile viewport ($< 768\text{px}$)
- **THEN** the node details SHALL present as a sliding bottom sheet with max height `85dvh`, touch drag handle, and safe area bottom clearance (`env(safe-area-inset-bottom)`)
- **AND** connected topic chips SHALL wrap into 2 columns without horizontal overflow.

#### Scenario: Floating Control Bar Mobile Safe Area
- **WHEN** user interacts with floating canvas control tools (`GraphControlBar.vue`)
- **THEN** the bar SHALL be positioned with safe area clearance (`pb-[max(0.75rem,env(safe-area-inset-bottom))]`) preventing collision with home indicator bars.
