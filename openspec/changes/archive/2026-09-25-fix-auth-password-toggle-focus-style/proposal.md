# Proposal

## Why

In `frontend/pages/login.vue`, the password visibility toggle button currently uses `absolute inset-y-0 right-0 pr-3.5 flex items-center` without `focus:outline-none`, whereas the confirm password toggle button uses `absolute right-3.5 top-1/2 -translate-y-1/2` with `focus:outline-none`. When a user navigates the form using the Tab key, focusing the password visibility toggle button renders an unconstrained, full-height rectangular outline that clips against the parent container's `rounded-xl` border and collides visually with the container's `:focus-within` border.

Aligning both password toggle buttons with consistent positioning, compact touch targets, and standardized keyboard focus ring styling resolves this visual collision and maintains high visual polish across all authentication form fields.

## What Changes

- Refactor the primary password visibility toggle button in `frontend/pages/login.vue` to use centered absolute positioning (`absolute right-3.5 top-1/2 -translate-y-1/2`) matching the confirm password field.
- Standardize focus ring behavior across all password toggle buttons on `/login` (and `/settings` where applicable) using `rounded-md`, `focus-visible:ring-2 focus-visible:ring-brand-500 focus-visible:outline-none focus:outline-none`, ensuring clean geometric boundaries when navigated via Tab.
- Keep the button accessible via keyboard and screen readers (`:aria-label`), while eliminating visual overflow outside the input's rounded border.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `auth`: Update the authentication form input requirements to specify that inline action accessory buttons (such as password visibility toggle buttons) must render centered with compact geometry and clean, non-colliding focus rings when focused via keyboard navigation.

## Impact

- **Frontend**: `frontend/pages/login.vue` (and any related password inputs) will render consistent, polished focus outlines when tabbing through input fields.
- **Accessibility**: Keyboard navigation remains functional and compliant with WCAG focus visibility standards without aesthetic glitches.
- **Dependencies**: No new npm dependencies or backend changes required.
