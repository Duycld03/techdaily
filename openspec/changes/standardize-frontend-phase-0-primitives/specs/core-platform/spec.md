# Spec Delta

## MODIFIED Requirements

### Requirement: Shared UI Primitives Responsive Geometry and Event Hygiene
All shared foundation UI primitives (`frontend/components/common/`, `frontend/components/app/`, `app.vue`, and `error.vue`) SHALL conform to strict mobile viewport responsiveness down to $320\text{px}$, zero horizontal overflow, VueUse declarative event lifecycle management, and clean engineering iconography.

#### Scenario: Dropdown and Time Picker Popovers on Narrow Mobile Viewports
- **WHEN** user activates `AppSelect` or `AppTimePicker` on a narrow mobile viewport ($320\text{px}$ to $375\text{px}$)
- **THEN** the popover menu or dropdown modal SHALL clamp within the visible viewport bounds without horizontal scrolling or clipping
- **AND** all clickable items SHALL provide a touch-target size of at least $44\text{px} \times 44\text{px}$.

#### Scenario: Mobile Dynamic Viewport Shell Adaptation
- **WHEN** user navigates any application route on a mobile device with dynamic address bars
- **THEN** the root layout shell in `app.vue` and `error.vue` SHALL utilize dynamic viewport units (`min-h-dvh`) to prevent layout jumpiness upon browser chrome collapse.

#### Scenario: Keyboard and Outside-Click Hygiene in Primitives
- **WHEN** floating primitives (`AppSelect`, `AppTimePicker`, `AppCommandPalette`) are opened or closed
- **THEN** document listeners for keyboard navigation (`Escape`, `ArrowUp`, `ArrowDown`, `Enter`) and backdrop dismissal SHALL be handled via VueUse composables (`useEventListener`, `onClickOutside`) with automatic teardown upon component unmount.
