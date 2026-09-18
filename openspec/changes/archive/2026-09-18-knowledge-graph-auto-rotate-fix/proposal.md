## Why

In `frontend/components/graph/GraphCanvas3D.vue`, the floating HUD toolbar includes an "Auto Rotate" (`Tự Động Xoay`) button with a `<Compass>` icon. This feature is intended to provide users with a cinematic 360° orbital camera view, slowly rotating around the center of the 3D knowledge graph constellation so all clusters and branches can be appreciated from every perspective without continuous mouse dragging.

However, clicking the button currently produces no visual movement. This failure occurs because:
1. `3d-force-graph` defaults to Three.js `TrackballControls` rather than `OrbitControls`. The `controls.autoRotate` property is exclusive to OrbitControls and is completely ignored by TrackballControls.
2. The performance-optimized simulation intentionally stops its continuous render loop after 120 ticks (render-on-demand). Without an active render loop calling `controls.update()`, camera coordinates remain frozen.

Implementing a dedicated orbital camera trajectory driver using `graphInstance.cameraPosition()` restores smooth, cinematic 360° rotation across all platforms.

## What Changes

- **Active Spherical Orbit Driver**: In `frontend/components/graph/GraphCanvas3D.vue`, replace the ineffective `controls.autoRotate` with a timer-based camera orbit driver:
  - When enabled, calculate camera position incrementally along a circular trajectory:
    $$x = R \cdot \sin(\theta), \quad z = R \cdot \cos(\theta)$$
    preserving the user's current orbital radius $R = \sqrt{x^2 + z^2}$ and height $y$.
  - Update `graphInstance.cameraPosition({ x, y, z })` on a lightweight interval (every 25ms / 40 FPS), which automatically triggers WebGL frame re-rendering on demand.
- **Interactive Gesture Disarming**: Pause or sync the orbital angle $\theta$ when the user manually drags or pans the canvas so auto-rotation does not fight user interactions.
- **Clean Lifecycle Teardown**: Stop the orbit timer cleanly when the feature is toggled off or when the component unmounts to prevent memory leaks.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `knowledge-graph`: Update `Requirement: Client-Side WebGL 3D Force-Directed Galaxy Visualization` to mandate that activating the Auto Rotate control initiates continuous 360° camera orbital rotation around the constellation center.

## Impact

- **Frontend Components**: `frontend/components/graph/GraphCanvas3D.vue`
- **Testing**: `frontend/tests/components/graph/GraphCanvas3D.spec.ts`
- **Backend / Database**: Zero impact.
