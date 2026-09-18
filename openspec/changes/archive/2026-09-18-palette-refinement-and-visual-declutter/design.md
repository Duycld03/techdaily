# Design: UI Palette Refinement & Visual Declutter

## Context

TechDaily's dark mode visual theme is defined in `frontend/tailwind.config.js` and component templates. Following Phase 1 and Phase 2, the UI has rich glassmorphic and card components, but suffers from "purple-on-purple" chromatic contamination. Secondary cards, backgrounds, icons, tags, and buttons all compete using varying saturations of violet. See `proposal.md - Why` for further background.

## Goals / Non-Goals

**Goals:**
- Provide a clean, neutral obsidian dark base (`#09090b`) that gives high contrast and eye comfort.
- Follow the 60-30-10 color hierarchy: 60% neutral dark obsidian, 30% crisp typography & structural hairline borders, 10% intentional Iris Violet accent.
- Remove redundant ambient glows and gradient overlays that cause visual fog.
- Retain existing responsive layouts, component contracts, and test coverage without breaking functionality.

**Non-Goals:**
- Redesigning application layouts or changing existing route architectures.
- Removing or modifying feature capabilities (such as SM-2 calculations or the Knowledge Graph).
- Altering light mode behaviors beyond ensuring brand color compatibility.

## Decisions

### Decision 1: Neutralize Canvas Palette in Tailwind Configuration
- **Choice**:
  - `canvas.DEFAULT`: `#09090b` (Zinc-950 obsidian base)
  - `canvas.subtle`: `#121215` (Slightly lifted surface for panels/sidebars)
  - `canvas.elevated`: `#18181b` (Surface for dropdowns, cards, and modal backdrops)
- **Rationale**: Purging the red/blue tint from dark backgrounds allows true white typography to look crisp and prevents dark cards from feeling muddy.

### Decision 2: Refine Brand Violet to Deep Iris
- **Choice**:
  - `brand-500`: `#7c3aed` (Deep Violet)
  - `brand-600`: `#6d28d9` (Rich Iris)
  - `brand-400`: `#a78bfa` (Muted lilac for high-contrast dark text)
- **Rationale**: Reduces neon glare by approximately 30-35% while preserving developer-centric studio personality.

### Decision 3: De-purple Dashboard Elements
- **Choice**:
  - Convert `ACTIVE READING SLICE` badge to `text-slate-400 font-mono text-[11px]`.
  - Icon containers in `TodayBentoDashboard.vue` shift from `bg-brand-500/10 text-brand-500` to `bg-white/[0.04] text-slate-400 border border-white/[0.06]`.
  - Strip `bg-brand-500/10 blur-3xl` radial blur background divs from the Welcome Banner and cards.
- **Rationale**: Ensures the primary CTA button (`Continue Reading`) has zero visual competition within the card.

### Decision 4: Subtle Sidebar Active Navigation Highlight
- **Choice**:
  - Active sidebar link: `bg-white/[0.06] text-white font-semibold border-l-2 border-brand-500`.
  - Icon: `text-brand-400`.
- **Rationale**: Mirrors the Linear/Raycast sidebar aesthetic—calm, uncluttered, with a precise indicator line rather than a large saturated rectangular block.

## Risks / Trade-offs

- **Risk**: Existing unit tests checking for specific CSS color classes in component templates may fail if assertions test exact Tailwind classes.
  - **Mitigation**: Verified that existing component tests assert semantic roles, text, and data-testids rather than brittle class strings. Unit test suites will be run across both frontend and backend to guarantee zero regressions.
