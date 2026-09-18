## Why

TechDaily's frontend currently relies on the default Nuxt Docs / VitePress aesthetic: a generic `slate-950`/`slate-900` background paired with default Nuxt Green (`brand-500` `#22c55e`), standard document borders (`border-slate-800`), and a flat sidebar navigation. While functional, this creates the impression of a static documentation template rather than an intelligent, high-focus AI-driven engineering mastery platform.

Modern software engineers value sleek, distraction-free, keyboard-first developer tools (such as Linear, Raycast, and Kiro IDE) combined with the motivational clarity of modern learning hubs. Through design exploration and benchmarking against five core architectural references, we establish the **Dev-Learning Studio** visual language for TechDaily:
1. **OLED Depth & Electric Violet Theme:** Replacing Nuxt Green and muddy slate with a deep OLED canvas (`#09080e`), refined card surfaces (`#12101b`), translucent hairline borders (`border-white/[0.08]`), and Electric Violet / Cyber Indigo accents (`#8b5cf6` / `#a78bfa`).
2. **Interactive Developer Shell:** Replacing the plain static header with a keyboard-driven Topbar featuring an interactive `⌘K` Quick Jump command palette trigger, dynamic Streak fire badge, language switcher, and refined user profile pill.
3. **Ergonomic Studio Sidebar:** Upgrading the navigation sidebar with crisp icon geometry, subtle glow on active states, and fluid collapse behavior that preserves maximum canvas space for reading, 3D cosmos, and mindmap exploration.

Establishing these design tokens and shell primitives in Phase 1 creates the reusable design system foundation, enabling all subsequent screens (Bento Dashboard, 3-Column IDE Reader, and future features) to inherit the modern aesthetic automatically without duplicate rework.

## What Changes

- **Core Design Tokens in `frontend/tailwind.config.js`:**
  - Introduce new `canvas` palette (`DEFAULT: '#09080e'`, `subtle: '#12101b'`, `elevated: '#1a1726'`, `border: 'rgba(255, 255, 255, 0.08)'`).
  - Redefine `brand` palette from Nuxt green to Electric Violet (`500: '#8b5cf6'`, `400: '#a78bfa'`, `600: '#7c3aed'`, `glow: 'rgba(139, 92, 246, 0.35)'`).
  - Add `streak` palette for gamified consistency (`amber: '#f59e0b'`, `glow: 'rgba(245, 158, 11, 0.4)'`).
  - Add `cyber` cyan palette (`400: '#22d3ee'`, `500: '#06b6d4'`) for graph data and high-tech telemetry.
- **Base Typography & Utility Classes in `frontend/assets/css/main.css`:**
  - Standardize glassmorphism card utilities (`.glass-card`, `.hairline-border`, `.glow-subtle`).
  - Update selection highlighting and scrollbar styling to match the Electric Violet aesthetic.
- **Modernized Navigation Shell in `frontend/layouts/default.vue` & Components:**
  - Refactor the top navigation bar with a centered `⌘K Quick Jump` search trigger button with keyboard shortcut badge.
  - Implement an accessible Command Palette modal component triggered via `Cmd+K` / `Ctrl+K` or clicking the topbar search input, allowing fast jumping to topics, books, knowledge graph, and roadmap.
  - Upgrade active streak badge to an interactive glowing pill displaying active study streak and daily goal status.
  - Upgrade the sidebar navigation with modern icons, active state glowing indicators, and clean collapsed/expanded transitions.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Update UI presentation requirements, styling tokens, and layout shell requirements to enforce the Dev-Learning Studio design language, hairline border standards, and `⌘K` command navigation.

## Impact

- **Frontend Styling:** Upgraded color palette, typography, and utility classes in `tailwind.config.js` and `main.css`.
- **User Experience:** Immediate visual elevation across the entire application shell; instant keyboard-accessible navigation via `Cmd+K`.
- **Backward Compatibility:** All existing page layouts (`/`, `/reader`, `/graph`, `/roadmap`, `/review`, `/quiz`, `/library`) remain 100% functional, automatically inheriting the refined colors and typography without layout breakage.
- **Backend & Database:** Zero backend changes, zero API contract modifications, zero database migrations.
