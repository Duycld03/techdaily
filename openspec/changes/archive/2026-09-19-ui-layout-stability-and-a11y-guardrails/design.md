# Design: UI Layout Stability and Accessibility Guardrails

## Context

TechDaily's frontend uses Nuxt 3, Tailwind CSS, and headless/teleported modal patterns (`AppCommandPalette`, `TermExplainerModal`, `LibraryImportModal`, `ReviewModal`). When interacting with these components, three layout and accessibility frictions occur:
1. When modals toggle `document.body.style.overflow = 'hidden'`, the browser removes the vertical scrollbar track, reflowing full-width containers and causing a perceptible 15px horizontal jump.
2. In `frontend/assets/css/main.css`, keyboard accessibility was inadvertently disabled by applying `outline: none !important` to `:focus-visible`.
3. In `frontend/pages/read/[bookId].vue`, the reader container uses `h-screen` (`100vh`), which calculates height based on the retracted address bar, causing the bottom action footer to be hidden underneath mobile browser chrome on iOS Safari and mobile Chrome.
4. `AppCommandPalette` shares `z-50` with general dialogs, risking layering conflicts when opened via hotkey over an active dialog.

## Goals / Non-Goals

**Goals:**
- Eliminate horizontal layout shift (CLS = 0) on desktop screens when opening and closing modals.
- Restore crisp, high-contrast visual focus feedback for keyboard navigation (`Tab` / `Shift+Tab`) across all interactive elements.
- Ensure the reading studio fills exactly the visible mobile viewport (`h-dvh`) without clipping or jumpy address bar resizing.
- Standardize overlay layering precedence so Command Palette (`z-60`) always displays above dialogs (`z-50`).

**Non-Goals:**
- Overhauling reader UI or typography controls (reserved for Phase 3 `ide-reader-and-concept-studio`).
- Adding heavy external modal or accessibility npm packages.

## Decisions

### 1. Reserve Scrollbar Gutter at Document Root
- **Decision**: Add `scrollbar-gutter: stable` to `html` in `frontend/assets/css/main.css`.
- **Mechanism**: The browser reserves a permanent scrollbar track space even when the page content does not overflow or when `overflow: hidden` is applied to `body`.
- **Trade-off / Alternative**:
  - *Alternative 1*: Calculate scrollbar width in JS (`window.innerWidth - document.documentElement.clientWidth`) and inject padding-right dynamically. (Rejected: adds JS overhead, flash of style, and potential desynchronization with fixed headers).
  - *Chosen*: Native CSS `scrollbar-gutter: stable`. Baseline support since 2022 (Chrome 94+, Firefox 97+, Safari 17+).

### 2. Disentangle Mouse Focus from Keyboard Focus Ring
- **Decision**: In `frontend/assets/css/main.css`, split `:focus` rules into pointer vs keyboard selectors:
  ```css
  /* Suppress sticky outlines on mouse/pointer clicks */
  button:focus:not(:focus-visible),
  a:focus:not(:focus-visible),
  [role="button"]:focus:not(:focus-visible) {
    outline: none !important;
    box-shadow: none !important;
  }

  /* High-contrast studio focus ring for keyboard navigation */
  button:focus-visible,
  a:focus-visible,
  [role="button"]:focus-visible,
  input:focus-visible,
  textarea:focus-visible,
  select:focus-visible {
    outline: 2px solid #8b5cf6 !important;
    outline-offset: 2px !important;
  }
  ```
- **Rationale**: Complies with WCAG 2.1 AA (Criterion 2.4.7: Focus Visible) while keeping touch and mouse interactions clean and free of sticky browser outlines.

### 3. Dynamic Viewport Scaling (`h-dvh`)
- **Decision**: In `frontend/pages/read/[bookId].vue`, replace `h-screen` with `h-dvh` (Dynamic Viewport Height).
- **Mechanism**: `100dvh` automatically tracks the real visible screen height as the mobile browser address bar expands or contracts during scrolling, preventing bottom toolbar occlusion.

### 4. Semantic Overlay Stacking Order
- **Decision**: Update `frontend/components/app/AppCommandPalette.vue` overlay container from `z-50` to `z-60`.
- **Hierarchy**:
  ```text
  +-----------------------------------------------------------+
  | Tier 5: Toast Container        -> z-[9999]                |
  | Tier 4: Global Command Palette -> z-60                    |
  | Tier 3: Modals & Drawers       -> z-50                    |
  | Tier 2: Popovers & Dropdowns   -> z-40                    |
  | Tier 1: Sticky Topbar & Header -> z-30                    |
  +-----------------------------------------------------------+
  ```

## Risks / Trade-offs

- **Browser Support for `scrollbar-gutter`**:
  - Supported in all modern evergreen browsers (Chrome, Edge, Firefox, Safari 17+).
  - Legacy browsers ignore the property safely without breaking layout.
- **Focus Ring Invasiveness**:
  - Using `outline-offset: 2px` ensures the outline renders outside the element box without displacing adjacent elements or causing text reflow.
