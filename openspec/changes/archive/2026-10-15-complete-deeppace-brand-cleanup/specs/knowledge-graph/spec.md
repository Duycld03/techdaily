# Knowledge Graph Spec Delta

## MODIFIED Requirements

### Requirement: Multi-Dimensional Graph Filtering & Live Search
The knowledge graph view SHALL include a floating glassmorphic control bar (`GraphControlBar.vue`) positioned above the canvas, providing real-time client-side filtering across multiple dimensions and engine modes without triggering backend network requests:
1. **Engine Mode Switcher (2D / 3D):** A prominent dual-button toggle allowing the user to seamlessly switch between the **2D Planar Canvas** (Cytoscape.js) and the **3D WebGL Cosmos** (`3d-force-graph` / Three.js). The active mode SHALL persist in `localStorage` under key `deeppace_graph_view_mode`.
2. **Pillar Category Filter:** Filter chips allowing the user to view all nodes or isolate a specific pillar (`All`, `Mental Models & Decisions`, `Habits & Deep Work`, `Backend Runtime`, `Database & Storage`, `Distributed Systems`, `Frontend & Web`, `Engineering Craft`). The filter container SHALL employ a responsive wrapping layout (`flex-wrap gap-1.5`) without hidden scrollbars or box-model clipping across both English and Vietnamese locales, ensuring that all 8 pill options remain 100% visible and discoverable. All category pills SHALL resolve explicit localization keys without falling back to raw untranslated strings.
3. **Node Type Toggles:** Toggle buttons to show or hide specific node types (`Books`, `Chunks`, `Flashcards`, `Highlights`).
4. **Mastery Status Filter:** Dropdown or pill selector to filter flashcard nodes by SM-2 status (`All`, `Learning`, `Reviewing`, `Mastered`). When `Mastered` is selected, the active indicator SHALL display primary brand violet styling (`bg-brand-600 text-white`) instead of emerald green.
5. **Live Search Input:** Text input that dynamically matches node titles, tags, and summary keywords. Matching nodes SHALL remain fully opaque and highlighted, while non-matching nodes SHALL fade to 15% opacity with edges dimmed in both 2D and 3D modes.
6. **Reset Filters CTA:** A button to immediately reset all filters, search inputs, and node opacities back to the default global view.
7. **Action Controls Visual Alignment (Fit to Screen & Reset Filters):** Dedicated action buttons ("Fit to Screen", "Reset Filters") providing 1-click viewport centering and filter clearance SHALL share a unified compact rendered height (`h-8`, 32px), typography scale (`text-xs font-bold`), icon dimensions (`w-3.5 h-3.5`), and padding (`px-2.5 py-1.5`) matching the exact visual footprint and vertical baseline of the adjacent 2D/3D view mode switcher container.

All control bar action buttons, mode switches, and filter chips SHALL utilize `.glass-panel`, `dark:bg-canvas-subtle`, `dark:bg-canvas-elevated`, and `dark:border-white/[0.08]`, completely eliminating legacy `dark:bg-slate-800`, `dark:bg-slate-900`, and `dark:border-slate-700`.

#### Scenario: Switching between 2D and 3D view modes
- **WHEN** the user clicks the "3D Cosmos" mode button in `GraphControlBar.vue`
- **THEN** the 2D canvas is smoothly unmounted or hidden
- **AND** the 3D WebGL Galaxy canvas mounts, preserving active category and type filters
- **AND** the choice is stored in `localStorage` so subsequent visits default to the chosen engine.

#### Scenario: Filtering by pillar category
- **WHEN** the user clicks the "Data Storage & Persistence" pillar filter chip
- **THEN** all book, chunk, card, and highlight nodes associated with other categories are hidden from the canvas
- **AND** the viewport smoothly animates to focus on the Data Storage cluster.

#### Scenario: Toggling node types via type filters
- **WHEN** the user disables the `Books`, `Chunks`, `Flashcards`, or `Highlights` toggle
- **THEN** nodes of that type and their connecting edges are hidden from the canvas
- **AND** no legacy `Topics` toggle is present in the control bar.

#### Scenario: Filtering by flashcard mastery level
- **WHEN** the user selects "Mastered" in the mastery status filter
- **THEN** card nodes with status `Learning` or `Reviewing` are hidden from the canvas
- **AND** only cards with SM-2 interval $\ge 21$ days (`Mastered`) remain visible alongside their connected chunk nodes.

#### Scenario: Live search node focus
- **WHEN** the user types `"MVCC"` into the search input
- **THEN** nodes containing `"MVCC"` in their title, summary, or tags remain fully highlighted with a glowing border
- **AND** all unrelated nodes fade to 15% opacity
- **AND** pressing Enter or clicking a match centers the viewport onto that node at 1.5x zoom.

#### Scenario: Resetting filters
- **WHEN** the user clicks the "Reset" button after applying multiple filters
- **THEN** all node type toggles, category chips, mastery filters, and search queries return to default
- **AND** all nodes and edges return to 100% visibility.

#### Scenario: Bilingual responsive category pills wrapping and complete localization
- **GIVEN** a user views `/graph` in Vietnamese locale (`vi-VN`)
- **WHEN** inspecting the Category Pillars filter row in `GraphControlBar.vue`
- **THEN** all 8 category pills ("Tất Cả", "Mô Hình Tư Duy & Quyết Định", "Thói Quen & Tập Trung Sâu", "Backend & Runtime", "Database & Storage", "Distributed Systems", "Frontend & Web", "Engineering Craft") are fully visible without horizontal clipping or truncation
- **AND** each pill resolves its translated label rather than falling back to raw untranslated English strings
- **AND** on viewports narrower than the combined pill width, the container wraps naturally into multiple clean rows.

#### Scenario: Action buttons match 2D/3D toggle height and typography
- **WHEN** a user views the knowledge graph control bar on `/graph`
- **THEN** the "Fit to Screen" and "Reset Filters" buttons have an identical height (`h-8`, 32px), typography scale (`text-xs font-bold`), and icon size (`w-3.5 h-3.5`) aligning horizontally with the 2D/3D mode switcher.

#### Scenario: DeepPace category filtering in control bar
- **WHEN** the user selects the "Mô Hình Tư Duy" or "Thói Quen & Tập Trung" category chip in `GraphControlBar.vue`
- **THEN** all nodes belonging to other categories are filtered out from the canvas
- **AND** the viewport smoothly animates to focus on the selected DeepPace constellation
- **AND** all 8 filter chips wrap responsively without visual clipping or overflow.

### Requirement: Interactive Visual Graph Legend & Entity Guide
The knowledge graph view SHALL feature a floating, collapsible visual legend panel (`GraphLegend.vue`) positioned in the bottom-left viewport corner (`bottom-5 left-5`), providing an intuitive visual key for all node geometries, relative scales, category colors, and SM-2 retention metrics across both 2D and 3D view modes.

The legend panel SHALL present:
1. **Header:** A title labeled "Visual Legend" (`en-US`) / "Bảng Chú Thích" (`vi-VN`) with a layers glyph, and a collapse control.
2. **Entity Hierarchy Section:**
   - Single-column vertical stack formatting (`flex flex-col gap-1`), rendering unclipped unabridged text labels for every entity type without using text truncation or ellipsis characters across both English and Vietnamese locales.
   - Each entity row SHALL display the count for that node type sourced from the graph payload `stats.nodeTypeCounts`. When a count is unavailable the row SHALL omit the number rather than render `undefined` or `NaN`.
   - Each entity row's swatch SHALL mirror the on-canvas node geometry for that type:
     - **Pillar Hubs:** Ringed filled circle (2D) / Luminous cosmic hubs (3D) color-coded by the 5 canonical engineering pillars.
     - **Topics:** Smaller filled circle (2D) / Planetary spheres (3D) color-coded by parent pillar ("Curriculum Topic" / "Chủ đề giáo trình").
     - **Books:** Rounded-rectangle swatch (2D) / Indigo spheres (3D) representing ingested documentation ("Tech Book" / "Sách kỹ thuật").
     - **Highlights:** Diamond (rotated-square) swatch (2D) / Cyan satellites (3D) representing personal quotes and notes ("Personal Note / Highlight" / "Ghi chú & Trích đoạn").
3. **Flashcard Retention Status Section (SM-2):**
   - Single-column vertical stack formatting (`flex flex-col gap-1`), rendering complete status labels and duration intervals without text wrapping or clipping.
   - **Learning ($< 6$ days):** Amber indicator (`#f59e0b`).
   - **Reviewing ($6–20$ days):** Blue indicator (`#3b82f6`).
   - **Mastered ($\ge 21$ days):** Brand violet indicator (`#7c3aed`), matching the mastered-flashcard fill rendered on the canvas (the legacy emerald `#10b981` swatch is retired).
4. **Interactive Hover Dimming:**
   - Hovering over any entity or retention row in the legend SHALL keep matching nodes fully opaque and dim non-matching nodes and their edges to 15% opacity (`0.15`) in both the 2D and 3D renderers.
   - Leaving the hover area SHALL immediately restore full standard node and edge opacities.
5. **Footer:** The legend footer SHALL display an isolate hint ("Hover item to isolate" / "Di chuột để tách biệt") and an `L` keyboard-shortcut chip.
6. **Collapsible Header, Safe Clearance & Persistence:**
   - The expanded card container width SHALL be standardized to 224 pixels (`w-56`), guaranteeing at least 80 pixels of horizontal clearance between the legend card and the 2D Minimap on 768px tablet viewports with the navigation sidebar open.
   - On viewports narrower than 1024 pixels (tablet and mobile devices), the legend panel SHALL default to a collapsed pill trigger button (`[ ? Visual Legend ^ ]` / `[ ? Bảng Chú Thích ^ ]`) on initial page load when no user preference is stored in `localStorage`.
   - The collapsed state SHALL persist in `localStorage` under `deeppace_graph_legend_collapsed`.

#### Scenario: Legend displays entity hierarchy and SM-2 status color keys
- **GIVEN** a user is on `/graph` in either 2D or 3D view mode
- **WHEN** the user views the bottom-left corner of the screen
- **THEN** `GraphLegend.vue` displays geometry-matched swatches and descriptions for Pillar Hubs, Topics, Books, Highlights, and SM-2 Flashcard retention states (Learning, Reviewing, Mastered)
- **AND** each entity row shows its live node-type count sourced from `stats.nodeTypeCounts`.

#### Scenario: Entity rows show node-type counts from the graph payload
- **GIVEN** the loaded graph payload reports `stats.nodeTypeCounts` of `{ pillar: 5, topic: 48, book: 19, highlight: 184 }`
- **THEN** the Pillar, Topic, Book, and Highlight entity rows display `5`, `48`, `19`, and `184` respectively
- **AND** a node type whose count is missing renders no number rather than `undefined` or `NaN`.

#### Scenario: Mastered retention swatch matches canvas violet fill
- **GIVEN** a user views the expanded legend
- **WHEN** inspecting the SM-2 retention section
- **THEN** the "Mastered (≥ 21 days)" indicator renders brand violet `#7c3aed`, identical to the mastered-flashcard fill on the canvas
- **AND** no emerald `#10b981` mastered swatch is present.

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
- **WHEN** the user hovers over the "Highlights" or "Mastered" entry in the legend
- **THEN** all non-matching nodes and edges on the active graph canvas dim to 15% opacity (`0.15`)
- **AND** matching nodes remain fully opaque with prominent glowing accents
- **WHEN** the cursor leaves the legend item
- **THEN** all nodes and edges return to their standard opacity.

#### Scenario: Collapsing and expanding the legend panel with state persistence
- **WHEN** the user clicks the collapse button on the legend header
- **THEN** the legend smoothly transitions into a minimal floating pill button labeled "Visual Legend" (or "Bảng Chú Thích")
- **AND** the preference is saved in `localStorage`
- **WHEN** the user reloads the page or navigates back to `/graph`
- **THEN** the collapsed state is automatically preserved.

#### Scenario: Default collapsed state on tablet and mobile viewports
- **GIVEN** a user on a tablet ($768\text{px}$) or mobile device ($< 1024\text{px}$) with no prior `deeppace_graph_legend_collapsed` preference in `localStorage`
- **WHEN** the user navigates to `/graph`
- **THEN** the visual legend initializes in the collapsed state as a floating pill button (`[ ? Visual Legend ^ ]` / `[ ? Bảng Chú Thích ^ ]`).
