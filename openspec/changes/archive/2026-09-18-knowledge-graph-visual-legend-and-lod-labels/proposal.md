## Why

In the 3D knowledge graph view (`/graph`), all 30+ topic nodes currently render text billboard labels simultaneously alongside the 5 primary pillar hubs at overview distances. This results in an overwhelming "text storm" where labels collide, overlap, and completely obscure the underlying spherical constellation, defeating the purpose of the 3D space.

Additionally, users lack an intuitive on-screen visual legend (key) explaining what the geometric shapes, relative sphere sizes, and colors represent (such as large pillar spheres vs medium topic spheres vs SM-2 flashcard retention statuses: amber for Learning, blue for Reviewing, emerald for Mastered). Introducing strict distance-based Level-of-Detail (LOD) label decluttering alongside an interactive visual legend panel transforms the graph into a clean, legible, and self-explanatory architectural cosmos.

## What Changes

- **Overview LOD Label Decluttering in 3D**: At default galaxy overview distance, restrict visible 3D text billboards strictly to the 5 primary Pillar Hubs. Topic, book, flashcard, and highlight labels are culled at overview zoom and revealed dynamically upon hover, node selection, close camera zoom ($d < 250$), or when the user enables the label toggle.
- **HUD Quick Label Visibility Toggle**: Add a toggle button with an `[Aa]` icon to the bottom-right floating HUD in `GraphCanvas3D.vue`, allowing users to switch between "Clean Cosmos" (Hubs only) and "Full Inspection" (All labels) at any time.
- **Interactive Visual Legend (`GraphLegend.vue`)**: Implement a glassmorphic, collapsible legend widget positioned in the bottom-left corner (`bottom-5 left-5`), providing an intuitive visual key for both 2D and 3D views:
  - **Entity Hierarchy**: Pillar Hubs (large glowing circles/spheres), Topics (medium ovals/spheres), Books (indigo rectangles/spheres), Highlights (cyan hexagons/satellites).
  - **Flashcard Retention Colors**: Learning (amber, interval $< 6$d), Reviewing (blue, interval $6–20$d), Mastered (emerald, interval $\ge 21$d).
  - **Relational Connections**: Translucent beams and animated particle pulses.
- **Interactive Legend Hovering**: Hovering over any entity item in the legend dims non-matching graph nodes to 20% opacity and highlights matching nodes, allowing instant visual isolation of specific concept types.
- **Bilingual Localization**: Full parity across English (`en.json`) and Vietnamese (`vi.json`) for all legend labels and tooltips.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `knowledge-graph`: 
  - Update `Requirement: Client-Side WebGL 3D Force-Directed Galaxy Visualization` to enforce strict overview LOD culling (Pillars only by default) and HUD label toggle.
  - Add `Requirement: Interactive Visual Graph Legend & Entity Guide` defining the collapsible visual key panel and hover-dimming interactions.

## Impact

- **Frontend Components**:
  - `frontend/components/graph/GraphLegend.vue`: New interactive visual legend component.
  - `frontend/components/graph/GraphCanvas3D.vue`: Refined LOD label rendering logic, HUD toggle button, and hover state synchronization.
  - `frontend/pages/graph.vue`: Mount `GraphLegend.vue` across both 2D and 3D views.
  - `frontend/i18n/locales/en.json` & `vi.json`: New localized legend keys.
- **Backend / Database**: Zero impact. Client-side presentation and interaction improvements only.
