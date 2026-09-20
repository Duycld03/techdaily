# Spec Delta

## MODIFIED Requirements

### Requirement: Immersive Reader Studio Mobile Height and Typography Controls
The dedicated Reader Studio (`pages/read/[bookId].vue`) and document reading surfaces (`DocReaderPane.vue`) SHALL utilize dynamic viewport units (`h-dvh`), safe area padding, and responsive segmented typography controls to prevent layout clipping, text wrapping, and event listener leaks on mobile devices.

#### Scenario: Dynamic Viewport Height on Mobile Reader
- **WHEN** user reads technical documentation on a mobile device
- **THEN** the reader container and table-of-contents drawer SHALL use `h-dvh` to ensure reading position and bottom navigation bars stay visible when browser address bars auto-hide.

#### Scenario: Typography Settings Segmented Control on 320px Screens
- **WHEN** user opens typography settings popover on a $320\text{px}$ to $375\text{px}$ screen
- **THEN** the font selection chips ("Sans", "Serif", "Mono") and line-height buttons SHALL preserve `whitespace-nowrap shrink-0` and scale without button clipping.

#### Scenario: Storage Synchronization Event Hygiene
- **WHEN** reader typography preferences are updated in `useReaderTypography.ts`
- **THEN** cross-tab synchronization SHALL utilize VueUse `useEventListener(window, 'storage', ...)` ensuring automatic cleanup on unmount.
