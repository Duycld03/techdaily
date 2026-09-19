# Design: Unify System Primary Theme Across Learning Interfaces

## Context

TechDaily's frontend uses a dual-palette architecture defined in `frontend/tailwind.config.js`:
- Obsidian Canvas (`#09090b`, `#121215`, `#18181b`) for dark-mode backdrops and panels.
- Deep Iris Violet (`brand-500` `#7c3aed`, `brand-600` `#6d28d9`, `brand-400` `#a78bfa`, `brand-300` `#c4b5fd`) as the system's primary identity color.

Across multiple legacy and iteratively developed learning surfaces (Today's interview challenge, Quiz arena and review queue, Spaced Repetition flashcards, Roadmap timeline, Mindmap tree, Knowledge Graph 2D/3D cosmos, and Insights), completed items, correct answer indicators, mastered cards, and telemetry badges were styled using various shades of emerald green (`#10b981`, `emerald-500`, `emerald-600`, `emerald-50/80`). This produces visual disharmony against the obsidian-and-violet brand shell.

See `proposal.md` for motivation and background.

## Goals / Non-Goals

**Goals:**
- Unify completed milestones, correct choices, and mastered indicators across `/today`, `/quiz`, `/review`, `/roadmap`, `/graph`, and `/insights` to the system primary brand color (`brand-500` / `brand-600` / `#7c3aed`).
- Maintain high contrast and WCAG 2.1 AA accessibility across both light mode (`slate-50`) and dark mode (`#09090b` canvas).
- Preserve semantic error colors (`rose-500` / `rose-600`) and developing/in-progress indicators (`amber-500`) so user feedback remains instantly clear.
- Update Cytoscape 2D canvas stylesheets and Three.js 3D cosmos material colors for Mastered nodes from `#10b981` to `#7c3aed`.

**Non-Goals:**
- Modifying backend schemas, database entities, or API response structures.
- Changing third-party syntax highlighting themes inside Shiki code blocks.
- Removing or altering existing functional interactions, keyboard shortcuts, or SM-2 algorithms.

## Decisions

### Decision 1: Standardized Tailwind Primary Token Pairings

For UI components across Vue SFCs, replace emerald classes with standardized `brand` utility pairings:

| UI Context | Legacy Emerald Styling | Unified Primary Brand Styling |
|---|---|---|
| Correct option background & border | `bg-emerald-50/80 dark:bg-emerald-500/10 border-emerald-500/60 text-emerald-900 dark:text-emerald-300 ring-emerald-500/30` | `bg-brand-50/80 dark:bg-brand-500/10 border-brand-500/60 text-brand-900 dark:text-brand-300 ring-brand-500/30` |
| Option badge (letter A/B/C/D) | `bg-emerald-600 text-white` | `bg-brand-600 text-white` |
| Feedback banner (Correct) | `bg-emerald-500/10 border-emerald-500/30 text-emerald-700 dark:text-emerald-300` | `bg-brand-500/10 border-brand-500/30 text-brand-700 dark:text-brand-300` |
| Feedback icon (CheckCircle) | `text-emerald-500` / `text-emerald-600` | `text-brand-500` / `text-brand-400` |
| Completed roadmap milestone/day | `bg-emerald-500 text-white ring-slate-50` | `bg-brand-600 text-white ring-slate-50 dark:ring-canvas shadow-sm` |
| Completed slice card border/badge | `border-emerald-500/30`, `bg-emerald-50 text-emerald-600` | `border-brand-500/40`, `bg-brand-50 dark:bg-brand-950/60 text-brand-600 dark:text-brand-400 border-brand-200 dark:border-brand-800` |
| Roadmap progress bar gradient | `from-brand-500 to-emerald-400` | `from-brand-600 via-brand-500 to-brand-400` |
| Mindmap completed edges & nodes | `stroke-emerald-400`, `bg-emerald-500` | `stroke-brand-500 dark:stroke-brand-400`, `bg-brand-600 text-white` |
| SM-2 Easy button (Grade 4) | `bg-emerald-50/80 dark:bg-emerald-500/10 border-emerald-200 text-emerald-700` | `bg-brand-50/80 dark:bg-brand-500/10 border-brand-200/80 dark:border-brand-500/30 text-brand-700 dark:text-brand-300 hover:bg-brand-100/90 dark:hover:bg-brand-500/20` |
| Review session completion icon | `bg-emerald-500/10 text-emerald-600 border-emerald-500/20` | `bg-brand-500/10 text-brand-600 dark:text-brand-400 border-brand-500/20` |
| Insights solution tab & benchmark | `bg-emerald-500/15 text-emerald-700 border-emerald-500/30`, `text-emerald-500` | `bg-brand-500/15 text-brand-700 dark:text-brand-300 border-brand-500/30`, `text-brand-500 fill-brand-500` |

### Decision 2: Knowledge Graph Mastered Color Constants

In graph visualizers where colors are passed as raw hex values into Canvas 2D or WebGL shaders:
- Cytoscape 2D (`GraphCanvas.vue`): Update `node[type = "card"][status = "Mastered"]` and `node[type = "card"][status = "mastered"]` background color from `#10b981` to `#7c3aed`, with border color updated from `#34d399` to `#c4b5fd`.
- WebGL 3D (`GraphCanvas3D.vue`): Update status color resolver `s === 'mastered' ? '#7c3aed'` and tooltip interval color `#a78bfa`.
- Minimap 2D (`GraphMinimap.vue`): Update `status === 'mastered' ? '#7c3aed'`.
- Legend (`GraphLegend.vue`): Update Mastered key indicator color from `bg-emerald-500` to `bg-brand-500`.
- Control Bar (`GraphControlBar.vue`): Update Mastered status active pill styling from `bg-emerald-600 text-white` to `bg-brand-600 text-white`.
- Detail Drawer (`GraphDetailDrawer.vue`): Update status badge for `mastered` from `bg-emerald-500/10 text-emerald-600 border-emerald-500/30` to `bg-brand-500/10 text-brand-600 dark:text-brand-400 border-brand-500/30`.

### Decision 3: Preserving Error and In-Progress Semantics

To prevent ambiguity between "Selected" and "Correct" during active answering:
- During active selection (unsubmitted): Options use iris violet ring/border (`border-brand-500 bg-brand-500/10 ring-1 ring-brand-500/30`).
- Upon submission / review:
  - Correct option: Solid primary badge (`bg-brand-600 text-white`), firm border (`border-brand-500/80`), subtle tinted fill (`bg-brand-500/10`), and prominent check icon (`text-brand-500`).
  - Incorrect option: High-contrast red/rose (`border-rose-500/80 bg-rose-500/10 text-rose-700 dark:text-rose-300`), solid rose badge (`bg-rose-600 text-white`), and X icon (`text-rose-500`).
  - Unselected distractors: Neutral dark glass (`border-white/[0.06] bg-white/[0.03] text-slate-400`).

## Risks / Trade-offs

- **Risk: Contrast in Light Mode**: Darker violet text (`brand-600` / `#6d28d9` or `brand-700` `#5b21b6`) must be used against light backgrounds (`bg-brand-50`) to ensure $\ge 4.5:1$ contrast ratio for WCAG AA compliance.
  - *Mitigation*: Strictly pair `text-brand-700` with `bg-brand-50` in light mode, and `text-brand-300` / `text-brand-400` with `dark:bg-brand-950/40` or `dark:bg-brand-500/10` in dark mode.
- **Risk: Graph Visual Distinction**: Topic nodes and Mastered card nodes both having violet accents could decrease visual contrast on the graph canvas if topic categories also use purple.
  - *Mitigation*: Topic nodes are circular/elliptical with pillar category colors, while Card nodes are distinct diamond shapes (2D) or compact satellites (3D), preserving geometric differentiation.
