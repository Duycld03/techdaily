# Proposal: Graph Visual Legend Vertical Stack & Tablet Minimap Clearance

## 1. Problem Statement

Recent visual reviews of the knowledge graph across multiple viewports (`Desktop 1920px`, `Tablet 768px`, and `Mobile 375px`) revealed two significant ergonomics and layout defects in `GraphLegend.vue`:

1. **Severe Text Truncation with Ellipses:**
   The Visual Legend card was constrained to `w-56 sm:w-60` ($\approx 240\text{px}$) while using a two-column grid (`grid-cols-2 gap-1`) for entity items. Accounting for card padding ($24\text{px}$), icon indicator ($12\text{px}$), and gap ($8\text{px}$), each column had only $\approx 70\text{px}$ of horizontal text space. Consequently, longer labels were forcefully truncated with ellipses:
   - English: `"Curriculum ..."` (for *Curriculum Topic*) and `"Personal N..."` (for *Personal Note / Highlight*).
   - Vietnamese: `"Chủ đề gi..."` (for *Chủ đề giáo trình*) and `"Ghi chú & ..."` (for *Ghi chú & Trích đoạn*).
   This truncation occurred even on large $1920\times 1080$ desktop screens.

2. **Visual Crowding and Conflict with Minimap on Tablet ($768\times 1024$):**
   On a $768\text{px}$ tablet viewport with the standard $240\text{px}$ navigation sidebar open, the remaining canvas width is only $528\text{px}$.
   - The expanded Visual Legend card occupied $240\text{px}$ at `bottom-5 left-5`.
   - The Minimap occupied $182\text{px}$ at `bottom-4 right-4`.
   - Together they consumed $422\text{px}$ of the $528\text{px}$ canvas bottom edge ($\approx 80\%$), leaving a meager $\approx 50\text{px}$ gap.
   - Any attempt to widen the card horizontally to resolve text truncation (e.g., to `w-80` / $320\text{px}$) would cause the right edge of the Legend to directly collide with and overlap the Minimap.

## 2. Proposed Solution

We propose a two-fold architectural fix for `GraphLegend.vue`:

1. **Vertical Stack Layout (Single Column):**
   - Replace the rigid `grid-cols-2` layout with a clean vertical flex stack (`flex flex-col gap-1`) for both Entity Hierarchy and Flashcard Retention items.
   - Completely remove the `truncate` CSS class from all legend labels. Every entity and status name will render in full without ellipses, ensuring 100% legibility in both English and Vietnamese.
   - Standardize the expanded card width to `w-56` ($224\text{px}$), which provides ample room for the longest strings (`Personal Note / Highlight`, `Ghi chú & Trích đoạn`) without exceeding the boundary.

2. **Guaranteed Clearance with Minimap ($\ge 86\text{px}$ on Tablet):**
   - With a fixed `w-56` ($224\text{px}$) width and `left-5` ($20\text{px}$), the Legend's right edge reaches $244\text{px}$.
   - The Minimap occupies $182\text{px}$ with `right-4` ($16\text{px}$), meaning its left edge starts at $528 - 198 = 330\text{px}$.
   - The guaranteed clearance buffer on $768\text{px}$ tablets is $330\text{px} - 244\text{px} = \mathbf{86\text{px}}$, preventing any physical collision or visual overcrowding.

3. **Tablet & Mobile Default Collapsed Ergonomics:**
   - Update `getInitialCollapsedState()` to default to collapsed (pill button: `[ ? Visual Legend ^ ]`) on screens $< 1024\text{px}$ (tablets and mobile devices) when no previous user choice is stored.
   - This keeps the canvas workspace clear by default for tactile navigation, while allowing users to tap and expand the legend on demand.
   - Persist user toggle preferences to `localStorage` (`techdaily_graph_legend_collapsed`).

## 3. Scope & Impact

- **Frontend:**
  - `frontend/components/graph/GraphLegend.vue`: Vertical stack layout, unclipped labels, responsive initial collapse threshold ($< 1024\text{px}$).
  - `frontend/tests/components/graph/GraphLegend.spec.ts`: Update tests to assert vertical list rendering, absence of `truncate` class, and proper dimensions.
  - `frontend/e2e/test-graph-roadmap-responsive.mjs`: Update E2E verification to check that unclipped text exists and that Legend and Minimap maintain clear separation.
- **Backend / Database / API:**
  - Zero changes required.
