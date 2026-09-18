# Proposal: IDE Reader and Concept Studio

## Why
Currently, the reading experience in TechDaily is fragmented across two separate paradigms: the split-pane `/today` focus page and the standalone `/read/[bookId].vue` page. While functional, the reader retains legacy documentation website traits (plain borders, loose headers, separate tabs on mobile, and disconnected scenario panes). In modern engineering workflows (benchmarked against Kiro IDE and Linear), developers thrive in an integrated **Concept Studio**: a high-contrast, distraction-free reading canvas with an interactive outline navigator on the left, line-anchored syntax-highlighted markdown in the center, and a docked architectural scenario copilot on the right.

Modernizing the reader into a unified **IDE Concept Studio** elevates reading from passive consumption to an active, engineering-grade learning environment.

## What Changes
- **3-Column IDE Studio Architecture**:
  - **Left Rail (Outline & Slice Navigator)**: Collapsible sidebar showing curriculum slices/chapters, reading progress percentages, completion checkmarks, and duration badges with Electric Violet active states.
  - **Center Stage (Distraction-Free Markdown Engine)**: High-contrast typography ($\ge 16\text{px}$ body text), line-height $1.75$, syntax-highlighted codeblocks with language tags and instant copy, breadcrumb bar (`Book > Chapter > Slice N`), and floating selection toolbar (Highlight, Note, Explain Term).
  - **Right Dock (Interactive Scenario Studio)**: Docked architectural challenge pane with collapsible drawer toggle (`⌘J` / toggle button), allowing engineers to test their comprehension against senior interview trade-offs side-by-side with authoritative source documentation.
- **Top Studio Control Bar**:
  - Compact breadcrumb navigation and slice pacer controls (`< Prev Slice` / `Next Slice >`).
  - Integrated reading progress bar with time-to-read estimate (`⚡ 4 min read • 47%`).
  - Typography inspector popover (Font scale slider, Sans/Serif/Mono font toggles, line-height options).
  - Panel toggles for left outline and right copilot drawers.
- **Component Modernization**:
  - Modernize `frontend/components/today/DocReaderPane.vue` to adopt the IDE studio visual language.
  - Modernize `frontend/components/today/InterviewChallengePane.vue` with elevated glass surfaces, sleek option selection pills, and instant trade-off analysis cards.
  - Unify styling and interaction patterns between `/today` and `/read/[bookId].vue`.

## Capabilities

### Modified Capabilities
- `reader`: Update specification to include the 3-column IDE Studio layout, collapsible outline navigator, top studio control bar with breadcrumbs and typography inspector, and docked scenario drawer.
- `today-reader`: Align Focus Studio reading requirements with the new IDE Concept Studio layout, ensuring responsive panel collapsing across desktop, tablet, and mobile viewports.

## Impact
- **Frontend Components & Pages**:
  - `frontend/pages/today.vue`: Adopts the 3-column studio layout with collapsible drawers.
  - `frontend/components/today/DocReaderPane.vue`: High-contrast markdown canvas, studio header, and floating toolbar.
  - `frontend/components/today/InterviewChallengePane.vue`: Modernized option pills and scenario review cards.
  - `frontend/composables/useReaderTypography.ts`: Enhanced typography controls.
- **Tests**:
  - Unit tests for reader rendering, panel toggling, and typography controls.
- **Backend / Database**: Zero API contract changes or database migrations required.
