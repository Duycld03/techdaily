# Design: Curriculum Timeline & Milestone Track

## Context

`frontend/pages/roadmap.vue` manages roadmap visualization across two primary tracks:
1. **Active Document Book Track**: Custom uploaded technical documentation broken into chapters and slice milestones.
2. **30-Day Senior Fullstack Curriculum Track**: The curated demo starter pack organized by 4 technical modules.

Currently, both tracks render cards in a multi-column responsive grid (`grid-cols-1 md:grid-cols-2 lg:grid-cols-3`) without a continuous visual spine or milestone telemetry, reducing the narrative feel of sequential progression.

## Goals / Non-Goals

**Goals:**
- Implement a continuous vertical milestone spine with progress gradients and connector beads linking chapters, modules, and daily slices.
- Highlight today's active milestone with an illuminated glowing pulse, active flame badge, and fast-launch CTA.
- Elevate timeline surfaces with studio design tokens (`bg-canvas-subtle`, `bg-canvas-elevated`, hairline borders `border-white/[0.08]`, and `.glass-card`).
- Maintain seamless bilingual support (`en-US`, `vi-VN`) with `whitespace-nowrap shrink-0` on badges.
- Retain full compatibility with the existing `mindmap` dual-view switcher.

**Non-Goals:**
- Zero backend API or database schema changes (existing DTOs provide all required statuses).
- No modifications to the Cytoscape mindmap canvas renderer.

## Decisions

### Decision 1: Pure Tailwind CSS Vertical Spine
Implement the timeline spine using pure CSS positioning:
- Vertical connector bar: `relative pl-8 sm:pl-10` with an absolute vertical line `w-0.5 bg-gradient-to-b from-brand-500/40 via-brand-500/20 to-slate-200 dark:to-white/[0.06]`.
- Node beads: Circular milestone indicators positioned directly on the vertical line indicating completion (`CheckCircle2`), active state (`Flame` with pulse ring), or upcoming (`Circle`).
- *Rationale*: Eliminates third-party timeline libraries, minimizing bundle size while remaining fully responsive and reactive.

### Decision 2: Unified Spine Language Across Book & Curriculum Tracks
Both the Book Chapter Milestones and the 30-Day Curriculum Modules share the identical timeline spine component structure and status styling.
- *Rationale*: Users experience the same high-end linear progression telemetry whether reading custom uploaded documentation or the starter curriculum.

### Decision 3: Illuminated Active Node Telemetry
Today's active learning node receives enhanced visual telemetry:
- Glowing ring: `ring-2 ring-brand-500/30 dark:ring-brand-400/40 shadow-lg shadow-brand-500/10`.
- Ember flame badge: `bg-amber-500/10 text-amber-400 border border-amber-500/20`.
- Fast-action CTA: Direct button to jump to the active slice/day without opening accordions.

## Risks / Trade-offs

- **Risk**: Compact mobile viewports ($< 640\text{px}$) could suffer from horizontal indentation crowding.
  - **Mitigation**: Use compact padding (`pl-7 sm:pl-10`) and smaller node beads (`w-6 h-6 sm:w-8 sm:h-8`) on mobile, with flexible truncation on titles and `line-clamp-2` on summaries.
