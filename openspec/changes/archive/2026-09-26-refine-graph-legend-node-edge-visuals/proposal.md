# Proposal

## Why

The knowledge graph visuals no longer match the approved **v3.4.1 "Knowledge Graph Architecture & Legend Showcase"** design, and the current spec is internally inconsistent: the legend documents "Mastered" flashcards as emerald `#10b981`, while the 2D canvas requirement renders mastered cards brand violet `#7c3aed`. This change re-aligns the three visual layers the design covers — the `GraphLegend.vue` entity/SM-2 key, the node hierarchy geometry, and the synapse edge styling — to a single, self-consistent v3.4.1 source of truth. It is purely a client-side visual refresh; the graph data contract is untouched.

## What Changes

### Legend components (`GraphLegend.vue`)
- Header labeled **"Bảng Chú Thích"** / "Legend" with a `layers` icon; footer shows a "Hover item to isolate" hint and an `L` keyboard chip.
- The **Entities** group renders a live per-type count from `stats.nodeTypeCounts` on each row: Pillar, Topic, Book, Highlight.
- Entity swatch shapes mirror the on-canvas node geometry: **Pillar** = ringed filled circle, **Topic** = smaller circle, **Book** = rounded rectangle, **Highlight** = diamond (rotated square) — replacing any generic dot swatches.
- **SM-2 retention** group: `Đang Học` (`< 6d`, amber `#f59e0b`), `Đang Ôn Tập` (`6–20d`, blue `#3b82f6`), `Đã Thành Thạo` (`≥ 21d`, **brand violet `#7c3aed`**). This retires the emerald `#10b981` mastered swatch so the legend agrees with the canvas.
- Legend hover-to-isolate dims non-matching nodes/edges to **15% opacity (`0.15`)**, down from 20%, matching the canvas dimming rate and the live-search dimming rate.

### Node hierarchy geometry (2D `GraphCanvas.vue`)
- **Pillar Hub:** circle `56px` → **`54px`**, `3.5px` neon halo (blur `18px`), z-rank 50.
- **Topic:** circle `36px` (unchanged) carrying a `Day N` index badge, inheriting parent pillar color.
- **Book:** rounded rectangle `44px` → **`34×26px`, corner radius `6`**, indigo tone with a bookmark stroke.
- **Card / Flashcard:** diamond `28px` → **`26px`**, SM-2 dynamic color map.
- **Highlight:** hexagon `24px` → **diamond-cut `22px`**.
- Relative size ordering is preserved (Pillar > Topic > Book > Card > Highlight).

### Synapse edge styling (2D `GraphCanvas.vue`, 3D `GraphCanvas3D.vue`)
- **Solid Constellation** (`TopicToPillar`, `BookToPillar`): Bézier curve, width `2.0px`, curvature `0.35`, opacity `0.85`.
- **Dotted Synapse** (`CardToHighlight`, `CardToPillar`): width `1.2px`, dash `[3, 4]`, opacity `0.60`.
- **Shared-Tag Dashed** (`SharedTag` cross-cutting taxonomy): width `1.5px`, dash `[4, 4]`, bidirectional, directional-particle pulse rate `0.007`.

### Cross-cutting
- Update `GraphLegend`/`GraphCanvas` Vitest **data-contract** tests (counts wiring, SM-2 color/label tokens, dimming payload, edge-style resolver) and the `en`/`vi` i18n keys the legend consumes.
- 3D adopts the unified SM-2 / pillar color tokens and the edge particle-pulse rate; 3D nodes remain spheres (pixel geometry above is 2D-only).

### Non-goals
- No backend, API, or DTO changes — `GET /api/v1/graph` payload shape is unchanged.
- No legend relocation — it stays docked bottom-left per the HUD clearance standard (the mockup's bottom-right pill is a standalone showcase artifact that would collide with the minimap).
- No changes to filtering, detail-drawer content, minimap behavior, navigation, or 3D physics.

## Capabilities

### New Capabilities

_None._ This change modifies visual/geometry requirements of an existing capability only.

### Modified Capabilities

- `knowledge-graph`: Two requirements are **modified** and one is **added**. **Interactive Visual Graph Legend & Entity Guide** gains live per-type counts, geometry-matched entity swatches, a unified brand-violet "Mastered" color, and a 15% hover-dimming rate. **Client-Side Canvas 2D Force Layout Visualization** re-cuts node pixel geometry/shapes to the v3.4.1 dimensions (pillar 54px, book 34×26px, card 26px diamond, highlight 22px diamond-cut). A new **Knowledge Graph Synapse Edge Styling** requirement defines per-relation-type line width, dash pattern, curvature, opacity, and directional particle-pulse rate across both the 2D and 3D renderers. The **Client-Side WebGL 3D Force-Directed Galaxy Visualization** requirement is unchanged (its SM-2 colors already match; edges are covered by the new requirement).
