# Design

## Context

See `proposal.md` — Why. This is a client-only visual refresh of the knowledge graph at `/graph`. No backend, DTO, or store-shape changes.

Ground truth from the current implementation (verified by code inspection):

- **Node geometry is already aligned** with the v3.4.1 design in `frontend/components/graph/GraphCanvas.vue`: pillar `54×54` ellipse border `3.5` (`:46-58`), topic `36×36` ellipse (`:100-107`), book `34×26` round-rectangle (`:146-154`), card `26×26` diamond (`:157-165`), highlight `22×22` (`:190-198`). The stale numbers (56/44/28/24) live only in the old spec text, so the geometry work here is spec correction, not code churn — except the mastered card is `28×28` (`:180-187`) instead of `26`, and highlight uses `shape: hexagon` (`:190-198`).
- **Color drift across renderers** is the real code work:
  - Highlight: 2D `#c4b5fd`/`#8b5cf6` violet (`GraphCanvas.vue:190-198`) vs legend/3D/minimap cyan `#06b6d4`.
  - Book: 2D `#94a3b8`/`#475569` slate (`GraphCanvas.vue:146-154`) vs legend/3D indigo `#6366f1`.
  - SystemDesign pillar: 2D `#8b5cf6` (`:82-87`) vs 3D `#7c3aed`.
  - DatabaseStorage topic: 2D `#34d399` emerald (`:116-120`) vs canonical cyan/teal.
- **Legend** (`GraphLegend.vue`) already has header (`Layers` icon + `graph.legend.title`), collapse/expand + `localStorage` persistence, geometry-matched swatches (pillar circle, topic circle, book rect, highlight diamond), SM-2 rows (amber/blue/`bg-brand-500` violet), and hover wiring `store.setHoveredLegendType(type)` (`:35-37`). It has **no count badges** (`:133-149`) and **no footer**.
- **Store** (`useKnowledgeGraphStore.ts`) already exposes `stats.nodeTypeCounts` (`:155-159`) and `hoveredLegendType` + `setHoveredLegendType` (`:58,237-239`).
- **Edges**: 2D (`GraphCanvas.vue:218-260`) styles `TopicToPillar`/`BookToPillar` (solid, w2, op0.85), `CardToHighlight`/`CardToPillar` (dotted, w1.2, op0.6), `SharedTag` (dashed `[4,4]`, w1.5); `CardToTopic`, `BookToTopic`, `HighlightToBook`, `HighlightToTopic` fall through to the generic base edge (solid w1.5). No `line-dash-pattern` on the dotted rule. 3D (`GraphCanvas3D.vue:304-322`) ignores `relationType` entirely (uniform `linkWidth 0.8`, active `2.5`, `directionalParticleSpeed 0.007`).
- **Dimming**: legend hover sets `.legend-dimmed`; 2D dims nodes to `0.15` (`:268-272`) but edges to `0.05` (`:274-278`); 3D dims to `0.15` (`GraphCanvas3D.vue:86-96`).
- **i18n** legend keys live at `en.json`/`vi.json:625-637`.

## Goals / Non-Goals

**Goals:**
- One canonical visual token set (per-category palette, per-type color/shape) applied identically across `GraphCanvas.vue` (2D), `GraphCanvas3D.vue` (3D), `GraphLegend.vue`, and `GraphMinimap.vue`.
- Legend entity rows show payload counts; legend gains the isolate-hint + `L` footer.
- Every one of the 9 `relationType` values resolves to exactly one of the three edge families.

**Non-Goals:**
- No changes to the graph payload, store shape, filtering logic, drawer content, minimap pan/zoom sync, navigation, or physics tuning.
- No relocation of the legend (stays docked bottom-left).
- No perpetual edge particle animation in 2D Cytoscape (would violate the settled-idle 0%-CPU invariant).

## Decisions

- **Single source of visual tokens.** Introduce one shared token map (category → fill/border, node type → color/shape, SM-2 status → color) and consume it from all four graph components rather than re-declaring hexes per file. *Why:* the drift above exists precisely because each renderer hard-codes its own colors. *Alternative rejected:* patch each file's literals independently — cheaper now, but reproduces the divergence on the next design tweak.
- **Canonical palette = the v3.4.1 design banner**, not the current 2D literals: SystemDesign `#7c3aed`/`#a78bfa`, Backend `#0284c7`/`#38bdf8`, Database `#0891b2`/`#22d3ee`, Frontend `#f59e0b`/`#fbbf24`, EngineeringCraft `#ec4899`/`#fb7185`; Book `#6366f1`; Highlight `#06b6d4`; SM-2 Learning `#f59e0b` / Reviewing `#3b82f6` / Mastered `#7c3aed`. This unifies 2D toward the values 3D/legend already use for highlight/book and toward the design banner for the two disagreeing category hexes.
- **Highlight shape → diamond, differentiated from card by size.** Cytoscape reserves `diamond` for cards today; both card (`26`) and highlight (`22`) become diamonds, disambiguated by size and color (violet-family card vs cyan highlight), matching the legend's rotated-square swatch. *Alternative rejected:* keep highlight `hexagon` — contradicts the design and the legend swatch.
- **Edge family mapping covers all 9 relation types.** The design names only 5; extend deterministically: Solid = `TopicToPillar`, `BookToPillar`; Dotted = `CardToHighlight`, `CardToPillar`, `CardToTopic`; Dashed (associative, default) = `SharedTag`, `BookToTopic`, `HighlightToBook`, `HighlightToTopic`. Implement as a `relationType → family` resolver so unknown/unlisted types default to the dashed family instead of the unstyled base edge. *Why:* removes the current silent fallback for 4 relation types and the total relationType-blindness in 3D.
- **Legend counts read `stats.nodeTypeCounts` (payload totals), not a recomputed visible set.** The store already exposes it; the design mockup shows static totals. *Alternative rejected:* recompute per active filter — extra state not requested and not shown in the design.
- **Edge dim unifies to `0.15`.** Raise 2D edge dim from `0.05` to `0.15` so all elements dim uniformly per the design's `opacity → 0.15` rule, matching node/3D dimming. Minor visual change; keeps one dim constant.
- **2D "particle flow" is emphasis-only.** The `0.007` directional-particle rate stays the 3D active-edge treatment. 2D distinguishes families by style/width/dash/curvature; any flow affordance runs only on active/selected/hovered edges and stops when idle. *Why:* Cytoscape has no native directional particles and a global animation loop breaks the 0%-idle-CPU requirement.
- **Testing split per AGENTS.md Pillar 3.** Vitest covers data contracts only: legend renders counts from `stats.nodeTypeCounts`; the `relationType → family` resolver returns the correct family per input; the SM-2/type color token map returns the expected hexes; hover still calls `setHoveredLegendType`. Geometry, colors-on-canvas, and layout integrity are verified through the headless-Chromium screenshot gate (Desktop 1440/1920 + Mobile 390) at implementation time — not through CSS/class assertions.

## Risks / Trade-offs

- **Overlap with in-flight change `personal-knowledge-graph`** → It edits the same `knowledge-graph` capability (graph projection + empty state), but different requirement blocks (extraction API / empty state) and different code (backend handler). This change touches visual requirements + frontend components only. Apply order is independent; if both are open at archive, re-run `openspec validate` after each archive. No shared requirement block is edited by both.
- **Card vs highlight both diamonds** → could read as the same entity at overview zoom. Mitigation: distinct size (26 vs 22) + distinct color family (violet vs cyan); highlight labels remain LOD-culled at overview anyway.
- **Raising 2D edge dim 0.05 → 0.15** → dimmed edges become slightly more visible. Mitigation: intentional per design; single constant, easy to tune.
- **Shared token map refactor touches 4 components** → risk of a missed literal. Mitigation: grep each old hex (`#8b5cf6`, `#34d399`, `#c4b5fd`, `#94a3b8`, `#475569`) after the change to confirm no stragglers; visual gate across both engines.

## Migration Plan

Pure frontend, no data migration. Rollback = revert the component/i18n/token commits. No feature flag needed; change is cosmetic and ships behind the existing `/graph` route.
