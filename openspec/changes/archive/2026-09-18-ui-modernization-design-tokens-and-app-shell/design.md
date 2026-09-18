## Context

TechDaily's UI redesign transitions the application away from the default Nuxt/VitePress documentation theme toward a high-focus **Dev-Learning Studio** inspired by developer power-tools (Linear, Kiro IDE, Raycast) and modern learning hubs.

This Change focuses strictly on **Phase 1: Design Tokens, Typography, and Application Shell Architecture**, establishing the foundational styling system and navigation shell before specialized screens (Bento Dashboard, 3-Column IDE Reader) are tackled in subsequent iterations.

## Goals / Non-Goals

**Goals:**
- Replace default Nuxt green (`#22c55e`) with Electric Violet (`#8b5cf6`) and deep OLED canvas (`#09080e`).
- Introduce hairline border standards (`border-white/[0.08]` in dark mode, `border-slate-200` in light mode) and glassmorphic card utilities.
- Introduce a centralized `⌘K` / `Ctrl+K` Command Palette component for instantaneous navigation across books, topics, graph, and roadmap.
- Modernize the default application shell (`frontend/layouts/default.vue`), topbar, and sidebar with glowing active indicators and polished typography.
- Maintain full backward compatibility with all existing Vue components and pages.

**Non-Goals:**
- Completely rewriting page-specific interior layouts (e.g. `/`, `/reader`, `/graph`, `/review`) in this change. Those pages inherit the new color tokens immediately and will receive dedicated layout updates in subsequent phases.
- Modifying backend APIs, database entities, or authentication protocols.

## Decisions

### 1. Token Architecture in `tailwind.config.js`
- **Decision**: Define semantic color layers (`canvas`, `brand`, `streak`, `cyber`) alongside standard Tailwind palettes.
  ```javascript
  colors: {
    canvas: {
      DEFAULT: '#09080e',      // Main deep background
      subtle: '#12101b',       // Primary card & panel surface
      elevated: '#1a1726',     // Dropdowns, popovers, modals
      border: 'rgba(255, 255, 255, 0.08)' // Hairline separator
    },
    brand: {
      50: '#f5f3ff',
      100: '#ede9fe',
      200: '#ddd6fe',
      300: '#c4b5fd',
      400: '#a78bfa',
      500: '#8b5cf6',          // Primary Electric Violet
      600: '#7c3aed',
      700: '#6d28d9',
      800: '#5b21b6',
      900: '#4c1d95',
      950: '#2e1065',
      glow: 'rgba(139, 92, 246, 0.35)'
    },
    streak: {
      amber: '#f59e0b',
      glow: 'rgba(245, 158, 11, 0.4)'
    },
    cyber: {
      400: '#22d3ee',
      500: '#06b6d4'
    }
  }
  ```
- **Rationale**: Replacing the `brand` palette at the Tailwind config level automatically propagates the new Electric Violet aesthetic throughout existing components that use `brand-500`, `brand-400`, etc., avoiding hundreds of manual class renames.

### 2. Hairline Borders & Glassmorphism Utilities
- **Decision**: Provide reusable CSS classes in `frontend/assets/css/main.css`:
  - `.glass-card`: `bg-canvas-subtle/80 backdrop-blur-md border border-white/[0.08] shadow-sm rounded-xl`
  - `.glass-panel`: `bg-canvas-elevated/90 backdrop-blur-lg border border-white/[0.1] shadow-lg rounded-2xl`
  - `.hairline-border`: `border border-white/[0.08] dark:border-white/[0.08]`
- **Rationale**: Provides consistent visual depth without heavy box shadows or opaque muddy cards.

### 3. Keyboard-Driven Command Palette (`AppCommandPalette.vue`)
- **Decision**: Implement a native Vue 3 teleported modal listening to `window` keydown events (`Meta+K` / `Ctrl+K`).
- **Features**:
  - Global search input with instant client-side filtering.
  - Quick action shortcuts: Jump to Today's Reading (`/`), Spaced Repetition Review (`/review`), Interview Quiz (`/quiz`), Knowledge Graph 2D/3D (`/graph`), Architecture Roadmap (`/roadmap`), and Document Library (`/library`).
  - Search book titles, curriculum topics, and flashcard decks.
  - Keyboard navigation: `ArrowDown`, `ArrowUp`, `Enter` to navigate, `Escape` to close.
  - Seamless touch trigger on mobile devices via the topbar search pill.

### 4. App Shell Modernization (`default.vue` Layout)
- **Top Navigation Bar**:
  - Left: Refined TechDaily monogram/logo with glowing dot indicator.
  - Center: Pill-shaped search bar `[ 🔍 Quick Jump to Topic, Doc, or Slice...  ⌘K ]` (desktop), condensed icon trigger (mobile).
  - Right:
    - Glowing Streak badge `[ 🔥 7 Days ]` with hover tooltip.
    - Compact language toggle (EN / VI).
    - Refined User Profile avatar pill with status ring.
- **Sidebar**:
  - Sleek icon rail on tablet/desktop, collapsible with smooth transitions.
  - Active navigation item receives an Electric Violet glow accent (`bg-brand-500/10 text-brand-400 border-l-2 border-brand-500`).

## Risks / Trade-offs

- **[Risk]** Potential color contrast regression in Light Mode if tokens only optimize for dark mode.
  → **Mitigation**: Define light mode mappings for `canvas` (`#ffffff`, subtle `#f8fafc`) and ensure `brand-600` maintains WCAG AA contrast against light backgrounds.
- **[Risk]** Keydown event collision with browser defaults (e.g. `Ctrl+K` browser search).
  → **Mitigation**: Use `event.preventDefault()` when `(event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'k'` is detected.

## Migration & Verification Plan

1. Update `frontend/tailwind.config.js` with new palette and typography settings.
2. Update `frontend/assets/css/main.css` with glassmorphic utility classes and base canvas styles.
3. Build `frontend/components/app/AppCommandPalette.vue` and integrate into `frontend/layouts/default.vue`.
4. Refactor `frontend/components/app/AppNavbar.vue` (or header section in `default.vue`) with the centered `⌘K` trigger, streak pill, and updated profile ring.
5. Add unit tests for `AppCommandPalette.vue` validating keydown handling, list filtering, and keyboard selection.
6. Verify layout stability with `npm test` and run responsive smoke test across Desktop, Tablet, and Mobile viewports.
