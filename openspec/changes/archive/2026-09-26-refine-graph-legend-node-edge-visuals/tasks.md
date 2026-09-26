# Tasks

## 1. Shared visual token module

- [x] 1.1 Create `frontend/utils/graphVisualTokens.ts` exporting the canonical maps from design.md — `categoryPalette` (category → `{ fill, border }`: SystemDesign `#7c3aed`/`#a78bfa`, BackendDotNet `#0284c7`/`#38bdf8`, DatabaseStorage `#0891b2`/`#22d3ee`, FrontendWeb `#f59e0b`/`#fbbf24`, EngineeringCraft `#ec4899`/`#fb7185`), `nodeTypeStyle` (book `#6366f1` round-rectangle, highlight `#06b6d4` diamond, card diamond), `sm2StatusColor` (Learning `#f59e0b`, Reviewing `#3b82f6`, Mastered `#7c3aed`), and `resolveEdgeFamily(relationType)` → `'solid' | 'dotted' | 'dashed'`. Verify with new `frontend/tests/utils/graphVisualTokens.spec.ts` asserting all 9 relation types map to the expected family (Solid: `TopicToPillar`,`BookToPillar`; Dotted: `CardToHighlight`,`CardToPillar`,`CardToTopic`; Dashed: `SharedTag`,`BookToTopic`,`HighlightToBook`,`HighlightToTopic`), an unknown type defaults to `dashed`, and each category/type/status returns its expected hex.

## 2. 2D canvas node styling (`frontend/components/graph/GraphCanvas.vue`)

- [x] 2.1 Replace the hard-coded node fills with `graphVisualTokens` lookups: highlight → `#06b6d4` (was `#c4b5fd`/`#8b5cf6`, `:190-198`), book → `#6366f1` (was `#94a3b8`/`#475569`, `:146-154`), SystemDesign pillar → `#7c3aed` (was `#8b5cf6`, `:82-87`), DatabaseStorage topic → `#22d3ee`/`#0891b2` (was `#34d399`, `:116-120`). Verify the visual gate shows cyan highlights, indigo books, violet Distributed pillar, and teal Data-Storage topics, and grep confirms the retired hexes are gone from the file.
- [x] 2.2 Change the highlight node `shape` from `hexagon` to `diamond` (`:190-198`) and set the Mastered card width/height to `26` (was `28`, `:180-187`) so all card diamonds are uniform 26px and highlights are a smaller 22px diamond. Verify via the visual gate that highlights render as diamonds distinct-by-size/color from cards.
- [x] 2.3 Route edges through `resolveEdgeFamily`: add explicit `line-dash-pattern: [3, 4]` to the dotted rule and include `CardToTopic` in it; move `BookToTopic`, `HighlightToBook`, `HighlightToTopic` into the dashed `[4, 4]` family (removing their silent fall-through to the base solid edge, `:218-260`). Verify with a data-contract test that the generated stylesheet contains a solid, a dotted `[3,4]`, and a dashed `[4,4]` rule and that each of the 9 relation types is covered by a family selector.
- [x] 2.4 Raise the `.legend-dimmed` edge opacity from `0.05` to `0.15` (`:274-278`) to match dimmed nodes and the 3D renderer. Verify via the visual gate that hovering a legend row leaves non-matching edges faint-but-visible at 15%.

## 3. 3D canvas parity (`frontend/components/graph/GraphCanvas3D.vue`)

- [x] 3.1 Source node sphere colors from `graphVisualTokens` (assert parity with the already-correct `#06b6d4`/`#6366f1`/`#7c3aed`, `:37-82`) and make `linkWidth`/`linkColor` reflect `resolveEdgeFamily` (family-based width ordering) instead of the uniform `0.8` that ignores `relationType` (`:304-322`), preserving `linkDirectionalParticleSpeed 0.007` on active edges. Verify via a data-contract test on the family→width helper and the 3D visual gate showing distinct edge families.

## 4. Legend components (`frontend/components/graph/GraphLegend.vue`)

- [x] 4.1 Render a per-row count badge for the pillar/topic/book/highlight entity rows from `store.stats?.nodeTypeCounts?.[type]`, omitting the number when the count is `undefined` (`:133-149`); keep the existing `legend-item-*` test ids. Verify with a Vitest test that mounts with a stubbed store (`stats.nodeTypeCounts = { pillar:5, topic:48, book:19, highlight:184 }`) and asserts the four counts render, plus a missing-count case renders no number (not `undefined`/`NaN`).
- [x] 4.2 Add a legend footer row showing the isolate hint (`$t('graph.legend.isolateHint')`) and an `L` `<kbd>` chip below the SM-2 section. Verify with a Vitest test asserting the footer hint text and kbd render, and that the existing collapse/expand and `setHoveredLegendType` specs still pass.
- [x] 4.3 Pull the entity/SM-2 swatch colors from `graphVisualTokens` (highlight cyan, book indigo, mastered violet) so the legend and canvas cannot drift. Verify via the visual gate that swatches match the on-canvas node colors and the existing `GraphLegend.spec.ts` suite passes.

## 5. Minimap parity (`frontend/components/graph/GraphMinimap.vue`)

- [x] 5.1 Point the minimap blip colors at `graphVisualTokens` (book was `#94a3b8` `:17`, highlight was `#c4b5fd` `:24`) so minimap dots match canvas node colors. Verify via the visual gate that minimap blips match their node colors in both light/dark mode.

## 6. Localization (`frontend/i18n/locales/{en,vi}.json`)

- [x] 6.1 Add `graph.legend.isolateHint` (en `"Hover item to isolate"`, vi `"Di chuột để tách biệt"`) near the other `graph.legend.*` keys (`:625-637`). Verify `npm test` legend/i18n specs pass with no missing-key fallback and both locales resolve the footer hint.

## 7. Verification gates (AGENTS.md Pillar 5 dual-gate)

- [x] 7.1 Gate 1 — run `npm test` from `frontend/` and confirm 100% pass, including the new token/edge-family, legend-count, and legend-footer specs and the still-green `GraphLegend.spec.ts` / `GraphCanvas.spec.ts` contracts.
- [x] 7.2 Gate 2 — drive headless Chromium via the `browser` device in `eval`: inject `techdaily_token`/`techdaily_user`, open `/graph` in 2D and 3D, and capture Desktop (1440×900) and Mobile (390×844) screenshots of the legend (counts + footer), the five node types, and the three edge families; present the images as visual proof in both English and Vietnamese.
- [x] 7.3 Grep the four graph components for retired literals (`#8b5cf6`, `#34d399`, `#c4b5fd`, `#94a3b8`, `#475569`, `hexagon`) and confirm none remain outside `graphVisualTokens.ts`.
