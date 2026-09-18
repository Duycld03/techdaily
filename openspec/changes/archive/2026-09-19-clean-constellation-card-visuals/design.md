# Design

## Context

In `DomainConstellationCard.vue`, the active constellation node `dist` (`Distributed`) defines `pulse: true` which conditionally mounts an outer pulsing aura circle:

```vue
<circle
  v-if="n.pulse"
  :cx="n.x"
  :cy="n.y"
  r="8"
  :stroke="n.color"
  stroke-width="1.5"
  fill="none"
  opacity="0.6"
  class="animate-pulse"
/>
```

The user requested completely removing this pulsing circle and any remaining motion, preferring a clean, static, distraction-free representation of all 5 pillar vertices.

## Goals / Non-Goals

**Goals:**
- Eliminate all animated pulse circles and flashing effects from the constellation SVG canvas.
- Remove obsolete `pulse?: boolean` typing and properties from `ConstellationNode`.
- Keep the static node styling intact: solid center dot, translucent outer boundary circle, and pillar name label.

**Non-Goals:**
- Changing the constellation node coordinates, edges, or colors.
- Altering the card metrics (148 nodes, 210 edges).

## Decisions

### 1. Remove Pulsing Outer Halo
Delete the conditional `<circle v-if="n.pulse" class="animate-pulse" ... />` from `DomainConstellationCard.vue`.

### 2. Clean Up Constellation Node Types and Definitions
- In `DomainConstellationCard.vue`:
  ```ts
  interface ConstellationNode {
    id: string
    name: string
    x: number
    y: number
    color: string
  }
  ```
- Remove `pulse: true` from `{ id: 'dist', name: 'Distributed', x: 120, y: 22, color: '#8b5cf6' }`.

### 3. Update Unit Tests
In `DomainConstellationCard.spec.ts`:
- Update the test to verify that neither `circle.animate-pulse` nor `circle.animate-ping` exists in the component.

## Risks / Trade-offs

- **None**: Removing an optional visual decoration reduces DOM node count and eliminates continuous CSS opacity animations on the client, improving CPU/GPU battery efficiency.
