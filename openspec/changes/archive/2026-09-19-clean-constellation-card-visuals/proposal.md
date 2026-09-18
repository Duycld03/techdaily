# Proposal

## Why

In `DomainConstellationCard.vue`, an animated pulsing outer halo (`class="animate-pulse"`) is rendered around active constellation vertices. The user explicitly requested removing this pulsing effect and any remaining animation overhead completely, as flashing and pulsing visual effects are unnecessary in an engineering-focused Dev-Learning Studio and introduce unwanted visual motion.

## What Changes

- Remove the pulsing outer halo circle element (`<circle v-if="n.pulse" class="animate-pulse" ... />`) entirely from `frontend/components/dashboard/DomainConstellationCard.vue`.
- Clean up the `pulse?: boolean` field from `ConstellationNode` interface and the node definitions array in `DomainConstellationCard.vue`.
- Ensure the constellation canvas renders clean, crisp, static engineering vertices with zero animations or visual distractions.
- Update unit tests in `frontend/tests/components/dashboard/DomainConstellationCard.spec.ts` to assert the absence of pulsing animated halos.

## Capabilities

### Modified Capabilities

- `core-platform`: Update the Home Command Center Dashboard and Domain Constellation Card requirements to specify static, distraction-free SVG node presentation without animated pulsing rings or flashing effects.

## Impact

- **Affected Code**: `frontend/components/dashboard/DomainConstellationCard.vue`, `frontend/tests/components/dashboard/DomainConstellationCard.spec.ts`.
- **User Experience**: Calmer, high-contrast, distraction-free presentation of the 5 engineering pillars on the Home Bento Dashboard.
- **Breaking Changes**: None.
