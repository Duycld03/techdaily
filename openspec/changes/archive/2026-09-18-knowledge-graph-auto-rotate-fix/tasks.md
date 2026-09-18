## 1. Implement Orbital Camera Trajectory Driver

- [x] 1.1 In `frontend/components/graph/GraphCanvas3D.vue`, replace the non-functional `controls.autoRotate` assignment with `startAutoRotate()` and `stopAutoRotate()` functions calculating spherical orbital coordinates ($x = R \sin\theta, z = R \cos\theta$) via `graphInstance.cameraPosition()`.
- [x] 1.2 In `frontend/components/graph/GraphCanvas3D.vue`, guarantee that `onUnmounted` and `toggleAutoRotate(false)` cleanly invoke `stopAutoRotate()` to prevent timer leaks.

## 2. Automated Tests & Quality Assurance

- [x] 2.1 In `frontend/tests/components/graph/GraphCanvas3D.spec.ts`, add unit tests asserting that toggling auto-rotate initiates continuous camera position orbital updates and toggling off or unmounting halts the interval.
- [x] 2.2 Run `npm --prefix frontend test` and verify all tests pass with zero regressions.
- [x] 2.3 Validate OpenSpec change integrity by running `openspec validate knowledge-graph-auto-rotate-fix --type change` and verifying 0 errors.
