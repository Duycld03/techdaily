# Proposal

## Why

TechDaily currently exhibits three subtle but persistent frontend layout stability and accessibility defects:
1. **Scrollbar Gutter Layout Shift (CLS)**: Opening modals, drawers, or the Command Palette locks body scroll via `overflow: hidden`, causing the 15px scrollbar to vanish and shifting all centered page content horizontally to the right, then jumping back when closed.
2. **Keyboard Accessibility Defect**: In `frontend/assets/css/main.css`, `outline: none !important` is applied indiscriminately to `:focus-visible`, leaving keyboard power users and accessibility devices with zero visual indication of focused elements when tabbing.
3. **Mobile Viewport 100vh Overflow**: In `frontend/pages/read/[bookId].vue`, using `h-screen` (`100vh`) causes the bottom reader navigation bar to be clipped by dynamic address bars in mobile browsers (iOS Safari and Android Chrome).

## What Changes

- **Viewport Scrollbar Stabilization**:
  - Add `scrollbar-gutter: stable` to the root `html` element in `frontend/assets/css/main.css`, preserving the scrollbar track space and eliminating horizontal layout shifting when modals or drawers toggle `overflow: hidden`.
- **Keyboard Focus Ring Restoration (WCAG 2.1 AA Compliance)**:
  - Disentangle `:focus` from `:focus-visible` in `frontend/assets/css/main.css`.
  - Suppress outlines strictly for mouse/touch clicks via `:focus:not(:focus-visible)`.
  - Implement a dedicated, high-contrast Studio focus ring for keyboard navigation via `:focus-visible` (`outline: 2px solid #8b5cf6; outline-offset: 2px;`).
- **Dynamic Mobile Viewport Height**:
  - Replace `h-screen` with `h-dvh` in `frontend/pages/read/[bookId].vue` (and related reader shells) so the reader container dynamically adjusts to visible viewport heights without clipping under mobile browser toolbars.
- **Semantic Layering Alignment**:
  - Ensure overlay z-indices adhere to the semantic tier: Toast (`z-[9999]`) > Command Palette (`z-60`) > Modals/Drawers (`z-50`) > Dropdowns/Floating Menus (`z-40`) > Sticky Header (`z-30`).

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Refine requirement `Dev-Learning Studio Design System & Navigation Shell` to mandate scrollbar track stability (`scrollbar-gutter: stable`), WCAG keyboard navigation focus rings (`:focus-visible`), and dynamic mobile viewport scaling (`h-dvh`).

## Impact

- **User Experience**: Completely eliminates 15px page jumpiness when opening modals (`Cmd+K`, AI explainer, delete dialogs) and restores keyboard navigation.
- **Mobile Compatibility**: Fixes sticky toolbar clipping on iOS Safari.
- **Zero Breaking Changes**: Fully non-breaking CSS enhancements; passes all existing tests.
