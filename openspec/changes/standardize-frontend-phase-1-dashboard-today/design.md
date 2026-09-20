# Design

## Context

See `proposal.md` for motivation. The Home Dashboard (`pages/index.vue`) and Daily Focus Studio (`pages/today.vue`) display complex metrics, domain constellations, AI synthesis overviews, and senior scenario drills. Under `antfu/skills` standards, these surfaces must be standardized for responsive Bento card stacking, SVG metric viewBox scaling, and modal scroll containment.

## Goals / Non-Goals

**Goals:**
- Guarantee Bento layout reflows from multi-column desktop grids to single-column vertical stacks on mobile viewports ($< 768\text{px}$).
- Scale SVG concentric progress rings in `ConcentricMetricCard.vue` cleanly across $320\text{px}$ to $480\text{px}$ screens without numeric overlap.
- Constrain `TermExplainerModal.vue` to `max-h-[85dvh]` with scroll containment and safe area bottom clearance.
- Replace manual key listeners or timeouts with VueUse `useEventListener` and `useTimeoutFn`.

**Non-Goals:**
- Changing AI generation prompts or backend curriculum pacing endpoints.
- Altering the SM-2 algorithm or telemetry calculation models.

## Decisions

### 1. Mobile Bento Stacking (`grid-cols-1 md:grid-cols-2 lg:grid-cols-3`)
- *Rationale*: Hardcoded 2-column or 3-column layouts squish metric cards and progress bars on narrow displays. Standardizing on `grid-cols-1 md:grid-cols-2 lg:grid-cols-3` ensures ample breathing room.

### 2. SVG Metric ViewBox Scaling
- *Rationale*: Fixed width/height SVG rings cause clipping or horizontal overflow on 320px devices. Setting `viewBox="0 0 200 200"` and `class="w-full h-auto max-w-[200px]"` ensures fluid scaling.

### 3. Modal Max Height and Safe Area
- *Rationale*: Long AI term explanations push action buttons off-screen on mobile devices without max-height constraints.

## Risks / Trade-offs

- **Risk**: Card heights becoming uneven in 2-column tablet layouts.
  - **Mitigation**: Use `flex flex-col justify-between` on inner card contents.
