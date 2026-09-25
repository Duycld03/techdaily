# Design

## Context

In `frontend/pages/login.vue`, the password visibility toggle button was styled with `absolute inset-y-0 right-0 pr-3.5 flex items-center`, stretching the button across the full height of the input container. In contrast, the confirm password field's toggle button used `absolute right-3.5 top-1/2 -translate-y-1/2` with a compact footprint.

When focused via the keyboard (Tab key), browsers apply their default `:focus-visible` ring across the element's bounding box. For the full-height button in the password field:
1. The bounding box has sharp 90-degree corners that protrude past the container's `rounded-xl` border radius.
2. The outer container simultaneously applies `:focus-within` styling (`focus-within:border-brand-500 focus-within:ring-1 focus-within:ring-brand-500`), resulting in a visual clash between the container's purple border and the full-height button's focus outline.

## Goals / Non-Goals

**Goals:**
- Harmonize the positioning and dimensioning of both the Password and Confirm Password visibility toggle buttons in `frontend/pages/login.vue`.
- Ensure keyboard focus on the eye button renders as a neat, centered focus ring (`rounded-md` with `focus-visible:ring-2 focus-visible:ring-brand-500/50`) without clipping or extending to the container boundaries.
- Maintain full accessibility: preserve `type="button"`, ARIA labels (`:aria-label`), and keyboard operability via Space/Enter.

**Non-Goals:**
- Redesigning the entire authentication form or altering the layout of other fields.
- Removing keyboard focusability (e.g. `tabindex="-1"` is not preferred because keyboard-only and assistive technology users need the ability to toggle password visibility).

## Decisions

### Decision 1: Standardize button positioning with `top-1/2 -translate-y-1/2`
Both the password toggle button and confirm password toggle button will adopt the centered positioning:
```html
class="absolute right-3.5 top-1/2 -translate-y-1/2 p-1 rounded-md text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 focus:outline-none focus-visible:ring-2 focus-visible:ring-brand-500/50 transition cursor-pointer"
```
*Rationale*: Using `top-1/2 -translate-y-1/2` with a small padding (`p-1`) keeps the button's DOM bounding box compact (~24x24px to 28x28px) centered directly around the 16x16px SVG icon (`w-4 h-4`), preventing it from touching the container edges.

### Decision 2: Controlled `:focus-visible` styling
- Suppress default browser outline with `focus:outline-none`.
- Provide an intentional, rounded keyboard focus ring via Tailwind's `focus-visible:ring-2 focus-visible:ring-brand-500/50 rounded-md`.
- Mouse clicks will not trigger the ring (handled cleanly by `:focus-visible`), while keyboard navigation displays an aesthetically aligned ring.

## Risks / Trade-offs

- **Risk**: Touch targets on mobile screens could be smaller if padding is too compact.
  *Mitigation*: Keep `p-1` and ensure the input's right padding (`pr-10`) prevents text from overlapping the toggle icon.
