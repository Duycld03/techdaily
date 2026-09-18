## Context

The 3D WebGL knowledge graph in `frontend/components/graph/GraphCanvas3D.vue` displays architectural entities in an interactive spherical cosmos using `3d-force-graph`.

In the floating control HUD on the right side of the canvas, an "Auto Rotate" button (`isAutoRotate`) is provided. The previous implementation attempted to toggle Three.js `OrbitControls.autoRotate`:
```typescript
function toggleAutoRotate() {
  isAutoRotate.value = !isAutoRotate.value
  if (!graphInstance) return
  const controls = graphInstance.controls()
  if (controls) {
    controls.autoRotate = isAutoRotate.value
    controls.autoRotateSpeed = 0.8
  }
}
```
This fails to produce any movement because:
1. `3d-force-graph` defaults to `TrackballControls` where `autoRotate` does not exist.
2. In accordance with the battery-conservation invariant, the continuous WebGL render loop settles and pauses after 120 ticks, so `controls.update()` is never invoked during idle periods.

## Goals / Non-Goals

**Goals:**
- Provide a smooth, cinematic 360-degree orbital rotation around the knowledge graph center when "Auto Rotate" is enabled.
- Preserve the user's current camera distance ($R$) and elevation ($y$) during rotation so zooming or tilting is respected.
- Ensure camera position updates wake the WebGL renderer on-demand without keeping an unneeded infinite rendering loop when auto-rotate is off.
- Cleanly clear timers on toggle-off and component unmount to prevent memory leaks or ghost computations.

**Non-Goals:**
- Replacing `3d-force-graph` with raw Three.js.
- Changing the 2D Cytoscape graph canvas behavior.

## Decisions

### 1. Direct Spherical Orbital Trajectory Driver
- **Decision**: Drive rotation via an interval-based trigonometric orbit using `graphInstance.cameraPosition()`.
- **Rationale**:
  - Calling `graphInstance.cameraPosition({ x, y, z })` automatically requests an internal Three.js frame render, resolving the render-on-demand freeze.
  - It works completely independent of whether the underlying controls are TrackballControls, OrbitControls, or custom controls.
- **Formulas**:
  - Given current camera position $(x, y, z)$:
    $$R = \sqrt{x^2 + z^2}$$
    $$\theta = \operatorname{atan2}(x, z)$$
  - Each tick ($\approx 25\text{ms}$ / $40\text{ FPS}$):
    $$\theta \leftarrow \theta + \Delta\theta \quad \text{where } \Delta\theta = \frac{2\pi}{2400} \text{ rad} \ (\approx 60\text{ seconds per full } 360^\circ \text{ revolution})$$
    $$x_{\text{new}} = R \cdot \sin(\theta), \quad z_{\text{new}} = R \cdot \cos(\theta), \quad y_{\text{new}} = y$$
    $$\text{graphInstance.cameraPosition}(\{ x: x_{\text{new}}, y: y_{\text{new}}, z: z_{\text{new}} \})$$

### 2. Gesture Integration & Pause
- **Decision**: If the user clicks or drags the canvas, synchronize the internal angle $\theta = \operatorname{atan2}(x, z)$ with the new camera position so rotation resumes seamlessly from the user's newly chosen angle rather than jumping back.

### 3. Lifecycle Cleanliness
- **Decision**: Store the timer handle in a component-scoped variable (`autoRotateTimer`). Explicitly call `clearInterval` in:
  1. `toggleAutoRotate()` when `isAutoRotate.value` transitions to `false`.
  2. `onUnmounted()` lifecycle hook.
  3. Graph cleanup handler before destroying the canvas.

## Risks / Trade-offs

- **[Risk] CPU usage while auto-rotating**:
  → **Mitigation**: 40 FPS interval (25ms) calling `cameraPosition()` consumes < 1% CPU on modern hardware and only updates camera matrices without recalculating force simulation physics. When toggled off, CPU usage immediately drops back to 0%.

## Migration Plan

1. Update `GraphCanvas3D.vue`:
   - Implement `startAutoRotate()` and `stopAutoRotate()` functions.
   - Refactor `toggleAutoRotate()` to start/stop the orbital timer.
   - Ensure `onUnmounted()` cleans up the timer.
2. Update unit tests in `frontend/tests/components/graph/GraphCanvas3D.spec.ts` to verify orbital timer invocation and cleanup.
