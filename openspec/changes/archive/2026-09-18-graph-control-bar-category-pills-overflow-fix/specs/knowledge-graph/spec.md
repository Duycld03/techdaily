## MODIFIED Requirements

### Requirement: Multi-Dimensional Graph Filtering & Live Search
The knowledge graph view SHALL include a floating glassmorphic control bar (`GraphControlBar.vue`) positioned above the canvas, providing real-time client-side filtering across multiple dimensions and engine modes without triggering backend network requests:
1. **Engine Mode Switcher (2D / 3D):** A prominent dual-button toggle allowing the user to seamlessly switch between the **2D Planar Canvas** (Cytoscape.js) and the **3D WebGL Cosmos** (`3d-force-graph` / Three.js). The active mode SHALL persist in `localStorage` under key `techdaily_graph_view_mode`.
2. **Pillar Category Filter:** Filter chips allowing the user to view all nodes or isolate a specific pillar (`All`, `Backend Runtime`, `Data Storage`, `Distributed Systems`, `Frontend Engineering`, `Engineering Craft`). The filter container SHALL employ a responsive wrapping layout (`flex-wrap gap-1.5`) without hidden scrollbars or box-model clipping across both English and Vietnamese locales, ensuring that all 6 pill options remain 100% visible and discoverable. All category pills SHALL resolve explicit localization keys without falling back to raw untranslated strings.
3. **Node Type Toggles:** Toggle buttons to show or hide specific node types (`Topics`, `Books`, `Flashcards`, `Highlights`).
4. **Mastery Status Filter:** Dropdown or pill selector to filter flashcard nodes by SM-2 status (`All`, `Learning`, `Reviewing`, `Mastered`).
5. **Live Search Input:** Text input that dynamically matches node titles, tags, and summary keywords. Matching nodes SHALL remain fully opaque and highlighted, while non-matching nodes SHALL fade to 15% opacity with edges dimmed in both 2D and 3D modes.
6. **Reset Filters CTA:** A button to immediately reset all filters, search inputs, and node opacities back to the default global view.

#### Scenario: Switching between 2D and 3D view modes
- **WHEN** the user clicks the "3D Cosmos" mode button in `GraphControlBar.vue`
- **THEN** the 2D canvas is smoothly unmounted or hidden
- **AND** the 3D WebGL Galaxy canvas mounts, preserving active category and type filters
- **AND** the choice is stored in `localStorage` so subsequent visits default to the chosen engine.

#### Scenario: Filtering by pillar category
- **WHEN** the user clicks the "Data Storage & Persistence" pillar filter chip
- **THEN** all topic, book, card, and highlight nodes associated with other categories are hidden from the canvas
- **AND** the viewport smoothly animates to focus on the Data Storage cluster.

#### Scenario: Filtering by flashcard mastery level
- **WHEN** the user selects "Mastered" in the mastery status filter
- **THEN** card nodes with status `Learning` or `Reviewing` are hidden from the canvas
- **AND** only cards with SM-2 interval $\ge 21$ days (`Mastered`) remain visible alongside their connected topic nodes.

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
- **THEN** all 6 category pills ("Tất Cả", "Backend & Runtime", "Database & Storage", "Distributed Systems", "Frontend & Web", "Engineering Craft") are fully visible without horizontal clipping or truncation
- **AND** each pill resolves its translated label rather than falling back to raw untranslated English strings
- **AND** on viewports narrower than the combined pill width, the container wraps naturally into multiple clean rows.
