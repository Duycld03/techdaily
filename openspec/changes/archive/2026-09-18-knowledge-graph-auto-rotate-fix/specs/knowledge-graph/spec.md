## MODIFIED Requirements

### Requirement: Client-Side WebGL 3D Force-Directed Galaxy Visualization
The client application SHALL provide an alternative 3D interactive knowledge graph visualization at `/graph` rendered with WebGL (via Three.js / `3d-force-graph`), executing calculations entirely on the client-side GPU without placing computational or memory load on the backend server.

The 3D visualization SHALL represent architectural entities in an interactive spherical cosmos:
1. **Pillar Hub Nodes:** Rendered as glowing primary celestial bodies with large radii and pillar-specific emissive glow colors.
2. **Topic Nodes:** Rendered as medium planetary spheres color-coded by their parent engineering pillar category.
3. **Book Nodes:** Rendered as textured or emblem-accented spherical bodies orbiting their parent pillar hubs.
4. **Card Nodes:** Rendered as compact glowing spheres color-coded by SM-2 retention status (Learning: amber, Reviewing: blue, Mastered: emerald).
5. **Highlight Nodes:** Rendered as crystalline or accent-colored satellites orbiting source books and topics.
6. **Relational Edges:** Rendered as glowing 3D vector splines or translucent beams linking interconnected nodes across $(x, y, z)$ space.

The 3D visualization SHALL provide 360-degree OrbitControls supporting rotation around arbitrary axes, smooth pan, pinch-zoom, and a camera reset button. The 3D visualization SHALL provide a functional 360-degree Auto-Rotate mode driven by an active orbital camera trajectory, rotating the camera smoothly around the constellation center at the current altitude and distance, and pausing automatically upon user drag interaction. The 3D engine SHALL implement distance-based Level-of-Detail (LOD) label culling:
- Text labels for Cards and Highlights SHALL be culled at overview camera distances and SHALL reveal only when the camera zooms close or when the user hovers over/taps the node.
- Pillar Hub and Topic labels SHALL remain visible across standard orbital distances.

To conserve user device battery and eliminate main-thread lag:
- The 3D force simulation SHALL settle node positions within a bounded warmup tick threshold (max 120 ticks) and halt physics calculations.
- The rendering loop SHALL throttle or pause when the camera is static and no animations are active (render-on-demand).
- Clicking any 3D node SHALL smoothly interpolate the camera toward the selected node and open `GraphDetailDrawer.vue`.

#### Scenario: 3D Galaxy visualization initialization
- **WHEN** the user activates 3D Cosmos mode on `/graph`
- **THEN** the application lazily loads the 3D WebGL engine without blocking the initial route transition
- **AND** initializes a 3D force-directed cosmos displaying nodes as glowing spheres in $(x, y, z)$ coordinates
- **AND** the physics simulation settles within 120 ticks without freezing the UI thread.

#### Scenario: 360-degree camera orbital navigation
- **WHEN** the user clicks and drags on the 3D canvas
- **THEN** the camera rotates smoothly around the orbital focus center at 60 FPS
- **AND** zooming with the mouse wheel smoothly scales the camera distance along the line of sight.

#### Scenario: 360-degree camera auto-orbit rotation
- **GIVEN** a user is on `/graph` in `3D Cosmos` mode
- **WHEN** the user clicks the "Auto Rotate" button in the floating HUD
- **THEN** the active indicator lights up with an accent highlight
- **AND** the camera begins continuous circular rotation around the constellation center $(0,0,0)$ along the $(x, z)$ orbital plane at consistent velocity
- **WHEN** the user clicks the button again
- **THEN** camera auto-rotation ceases immediately, leaving the camera positioned at its current vantage angle.

#### Scenario: Distance-based Level-of-Detail label culling
- **WHEN** the camera is positioned at a wide galaxy overview distance
- **THEN** text labels for Card and Highlight nodes are culled to prevent visual clutter
- **WHEN** the user zooms in close to a specific cluster or hovers over a node
- **THEN** high-contrast billboard labels for the targeted nodes dynamically appear facing the camera.

#### Scenario: 3D node selection and detail drawer sync
- **WHEN** the user clicks on a 3D node sphere
- **THEN** the camera smoothly animates to center on the selected node
- **AND** `GraphDetailDrawer.vue` opens with complete node details and 1-click action bridges matching the 2D view.

#### Scenario: Render-on-demand battery preservation
- **WHEN** the 3D layout has settled and the user is not actively rotating or zooming the camera
- **THEN** the WebGL frame loop halts active re-renders until interaction resumes, consuming negligible CPU and GPU cycles when idle.
