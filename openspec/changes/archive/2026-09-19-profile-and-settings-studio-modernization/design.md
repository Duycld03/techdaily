# Design

## Context

See `proposal.md` for motivation. All major user-facing learning and exploration modules (Home Bento Dashboard, Focus Studio, Reader, Roadmap Mindmap, 3D Knowledge Graph, Quiz, Review, Library, Notes, and Insights) have been modernized into the Dev-Learning Studio theme. The remaining surfaces are system and identity configuration views: `frontend/pages/settings.vue`, `frontend/pages/profile.vue`, and child components (`EngineerProfileHero.vue`, `DomainGoalTracker.vue`, `ThemeToggle.vue`, `LocaleSelector.vue`).

These views currently use Tailwind default slate values (`bg-slate-900`, `bg-slate-950`, `border-slate-800`), resulting in visual dissonance when transitioning from the OLED Obsidian Canvas.

## Goals / Non-Goals

**Goals:**
- Unify `settings.vue` and `profile.vue` containers into Obsidian Canvas (`dark:bg-canvas`, `dark:bg-canvas-subtle`).
- Elevate information cards into `.glass-card` and `.glass-panel` components with hairline borders (`dark:border-white/[0.08]`).
- Modernize `EngineerProfileHero.vue` identity hero and milestone statistics with subtle violet glows (`glow-subtle`).
- Modernize `DomainGoalTracker.vue` engineering pillar coverage bars with Electric Violet gradients (`from-brand-600 to-brand-500`).
- Update `ThemeToggle.vue` and `LocaleSelector.vue` to `.glass-panel` and hairline borders.
- Preserve 100% test compatibility with `tests/pages/settings.spec.ts` and `tests/pages/profile.spec.ts`.

**Non-Goals:**
- Altering any backend endpoint, DTO, or entity.
- Modifying the decluttering boundary between `/profile` and `/settings` (e.g. notification schedule controls remain strictly in `/settings`).
- Introducing any external chart or UI library.

## Decisions

### 1. Token Hierarchy & Surface Mapping
- **Canvas Base**: Root containers apply `bg-slate-50 dark:bg-canvas`.
- **Card Surfaces**: Information sections use `.glass-card` (`bg-white/80 dark:bg-canvas-subtle/80 backdrop-blur-md border border-slate-200/80 dark:border-white/[0.08] shadow-sm`).
- **Interactive Elevated Panels**: Tabs and selector bars use `.glass-panel` (`dark:bg-canvas-elevated/90`).
- **Accent Theme**: Active tab indicators, toggle switches, primary save buttons, and progress bars standardize on Electric Violet (`brand-600` / `brand-500` / `brand-400`).

### 2. Form Inputs & Select Controls
- Text inputs (`Name`, `Current Password`, `New Password`, `Confirm Password`), time inputs (`Preferred Study Time`, `Streak Alert Time`), and select dropdowns (`Target Role`, `Timezone`) will use:
  `bg-white dark:bg-canvas-subtle border border-slate-200/80 dark:border-white/[0.08] text-slate-900 dark:text-slate-100 focus:border-brand-500 focus:outline-none rounded-xl text-sm transition-all shadow-sm`
- Daily Goal Pace chips (5m, 10m, 15m, 30m) use:
  - Active: `bg-brand-500/15 border-brand-500 text-brand-400 font-bold shadow-sm`
  - Inactive: `bg-white dark:bg-canvas-subtle border-slate-200/80 dark:border-white/[0.08] text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-canvas-elevated`

### 3. Pillar Coverage Progress Visualization
- In `DomainGoalTracker.vue`, replace default solid color bars with smooth gradient fills:
  - Progress bar track: `bg-slate-100 dark:bg-white/[0.06]`
  - Progress bar fill: `bg-gradient-to-r from-brand-600 to-brand-500`
  - Telemetry badges: High-contrast monospace counters (`font-mono text-xs text-brand-400`).

## Risks / Trade-offs

- **Test Selector Fragility**: Some tests in `tests/pages/settings.spec.ts` or `tests/pages/profile.spec.ts` may assert specific class names or button texts.
  - *Mitigation*: Inspect tests beforehand; preserve all existing ARIA attributes, IDs, and reactive bindings (`v-model`, `@click`).
