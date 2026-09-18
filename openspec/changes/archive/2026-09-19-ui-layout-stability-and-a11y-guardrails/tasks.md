# Tasks

## 1. CSS & Layout Stability Standards

- [x] 1.1 Add `scrollbar-gutter: stable` to `html` in `frontend/assets/css/main.css` to reserve scrollbar track space and prevent horizontal layout shift (CLS) when dialogs and drawers lock body scroll.
- [x] 1.2 Disentangle pointer and keyboard focus states in `frontend/assets/css/main.css`: apply `:focus:not(:focus-visible)` for mouse/tap outline suppression and add `:focus-visible` rules with `outline: 2px solid #8b5cf6; outline-offset: 2px;` to restore WCAG 2.1 AA keyboard focus indicators.

## 2. Dynamic Viewport Scaling & Overlay Hierarchy

- [x] 2.1 Update `frontend/pages/read/[bookId].vue` reader container from `h-screen` (`100vh`) to `h-dvh` to prevent content clipping underneath mobile dynamic browser address bars.
- [x] 2.2 Elevate `frontend/components/app/AppCommandPalette.vue` backdrop overlay container from `z-50` to `z-60` to enforce semantic stacking hierarchy over standard dialogs and drawers.

## 3. Verification & Automated Tests

- [x] 3.1 Run full frontend test suite (`npm --prefix frontend test`) to ensure zero regressions across all components.
- [x] 3.2 Validate in headless browser that scrollbar track space remains stable during modal activation and keyboard focus rings trigger on Tab navigation.
- [x] 3.3 Validate OpenSpec change specifications and sync requirements with `openspec validate --changes` and `openspec validate --specs`.
