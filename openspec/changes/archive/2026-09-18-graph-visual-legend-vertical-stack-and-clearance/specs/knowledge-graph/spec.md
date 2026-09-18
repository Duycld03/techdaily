# knowledge-graph Specification Delta

## MODIFIED Requirements

### Requirement: Interactive Visual Graph Legend & Entity Guide
The knowledge graph view SHALL feature a floating, collapsible visual legend panel (`GraphLegend.vue`) positioned in the bottom-left viewport corner (`bottom-5 left-5`), providing an intuitive visual key for all node geometries, relative scales, category colors, and SM-2 retention metrics across both 2D and 3D view modes.

The legend panel SHALL present:
1. **Entity Hierarchy Section:**
   - Single-column vertical stack formatting (`flex flex-col gap-1`), rendering unclipped unabridged text labels for every entity type without using text truncation or ellipsis characters across both English and Vietnamese locales.
   - **Pillar Hubs:** Large circles (2D) / Luminous cosmic hubs (3D) color-coded by the 5 canonical engineering pillars.
   - **Topics:** Elliptical nodes (2D) / Planetary spheres (3D) color-coded by parent pillar ("Curriculum Topic" / "Chủ đề giáo trình").
   - **Books:** Rounded rectangles (2D) / Indigo spheres (3D) representing ingested documentation ("Tech Book" / "Sách kỹ thuật").
   - **Highlights:** Compact hexagons (2D) / Cyan satellites (3D) representing personal quotes and notes ("Personal Note / Highlight" / "Ghi chú & Trích đoạn").
2. **Flashcard Retention Status Section (SM-2):**
   - Single-column vertical stack formatting (`flex flex-col gap-1`), rendering complete status labels and duration intervals without text wrapping or clipping.
   - **Learning ($< 6$ days):** Amber indicator (`#f59e0b`).
   - **Reviewing ($6–20$ days):** Blue indicator (`#3b82f6`).
   - **Mastered ($\ge 21$ days):** Emerald indicator (`#10b981`).
3. **Interactive Hover Dimming:**
   - Hovering over any entity row in the legend SHALL highlight matching nodes across the canvas and dim non-matching nodes to 20% opacity.
   - Leaving the hover area SHALL immediately restore full standard node opacities.
4. **Collapsible Header, Safe Clearance & Persistence:**
   - The expanded card container width SHALL be standardized to 224 pixels (`w-56`), guaranteeing at least 80 pixels of horizontal clearance between the legend card and the 2D Minimap on 768px tablet viewports with the navigation sidebar open.
   - On viewports narrower than 1024 pixels (tablet and mobile devices), the legend panel SHALL default to a collapsed pill trigger button (`[ ? Visual Legend ^ ]`) on initial page load when no user preference is stored in `localStorage`.
   - The collapsed state SHALL persist in `localStorage` under `techdaily_graph_legend_collapsed`.

#### Scenario: Legend displays entity hierarchy and SM-2 status color keys
- **GIVEN** a user is on `/graph` in either 2D or 3D view mode
- **WHEN** the user views the bottom-left corner of the screen
- **THEN** `GraphLegend.vue` displays color-coded badges and descriptions for Pillar Hubs, Topics, Books, Highlights, and SM-2 Flashcard retention states (Learning, Reviewing, Mastered).

#### Scenario: Complete unclipped legend text in English and Vietnamese
- **GIVEN** a user views `/graph` with the visual legend expanded
- **WHEN** inspecting the entity hierarchy items and retention status items
- **THEN** all item labels (including "Curriculum Topic", "Personal Note / Highlight", "Chủ đề giáo trình", and "Ghi chú & Trích đoạn") are rendered in their entirety without text truncation or ellipsis characters (`...`).

#### Scenario: Tablet clearance between Visual Legend and Minimap
- **GIVEN** a user views `/graph` on a 768px tablet viewport in 2D mode with the sidebar open
- **WHEN** the user expands the Visual Legend panel
- **THEN** the horizontal clearance gap between the right edge of the Visual Legend and the left edge of the Minimap is at least 80 pixels
- **AND** neither component overlaps or occludes the other.

#### Scenario: Interactive legend hover dims non-matching graph nodes
- **GIVEN** the legend panel is expanded
- **WHEN** the user hovers over the "Flashcards" or "Mastered" entry in the legend
- **THEN** all non-matching nodes and edges on the active graph canvas dim to 20% opacity
- **AND** matching flashcard nodes remain fully opaque with prominent glowing accents
- **WHEN** the cursor leaves the legend item
- **THEN** all nodes and edges return to their standard opacity.

#### Scenario: Collapsing and expanding the legend panel with state persistence
- **WHEN** the user clicks the collapse button on the legend header
- **THEN** the legend smoothly transitions into a minimal floating pill button labeled "Legend" (or "Chú Thích")
- **AND** the preference is saved in `localStorage`
- **WHEN** the user reloads the page or navigates back to `/graph`
- **THEN** the collapsed state is automatically preserved.

#### Scenario: Default collapsed state on tablet and mobile viewports
- **GIVEN** a user on a tablet ($768\text{px}$) or mobile device ($< 1024\text{px}$) with no prior `techdaily_graph_legend_collapsed` preference in `localStorage`
- **WHEN** the user navigates to `/graph`
- **THEN** the visual legend initializes in the collapsed state as a floating pill button `[ ? Visual Legend ^ ]`.
