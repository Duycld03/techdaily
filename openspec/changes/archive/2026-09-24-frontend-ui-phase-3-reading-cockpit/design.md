# Design: Frontend UI Phase 3 - Reading Cockpit & Document Studio

## Context

See `proposal.md` for motivation. Currently, `frontend/pages/read/[bookId].vue` is an extensive monolithic component (>1,500 lines) that tightly couples:
- Top sticky reader navigation bar, book progress, and typography popovers.
- Dual-mode Table of Contents (collapsible desktop sidebar and off-canvas mobile slide-over drawer).
- Article prose rendering with Shiki highlighter integration and takeaway boxes.
- Floating selection action toolbar, highlight creation, and note popover.
- Bottom slice navigation cards.

Simultaneously, `frontend/pages/today.vue` exhibits subtle spacing and padding density inconsistencies on Windows 11 high-DPI viewports (1080p, 2K/4K) between the outline navigator, center reading pane (`DocReaderPane.vue`), and right challenge dock (`InterviewChallengePane.vue`).

## Goals / Non-Goals

**Goals:**
- Extract single-responsibility reader chrome components into `frontend/components/reader/`:
  - `ReaderHeaderBar.vue` (navigation, titles, slice progress, novel-style typography controls, theme toggle, quiz trigger).
  - `ReaderTocSidebar.vue` (searchable chapter outline, slice duration, active state, completion checkmarks for both desktop and mobile drawer).
  - `ReaderNavigationCards.vue` (symmetric bottom slice navigation cards with English/Vietnamese anti-clipping safeguards).
- Decouple reading chrome from reading prose in `read/[bookId].vue`, reducing file size by >50%.
- Standardize centered article prose canvas (`#main`) with responsive width presets (`max-w-3xl`, `max-w-4xl`, `max-w-full`) and symmetric gutter margins.
- Harmonize `/today` daily reading cockpit layout density and gap metrics across desktop, tablet, and mobile.
- Add unit tests for extracted components in `frontend/tests/components/reader/`.

**Non-Goals:**
- Altering the backend API endpoints, DTO contracts, or database schema.
- Changing markdown parsing logic (`useMarkdownRenderer`) or Shiki token styling.
- Reworking SM-2 spaced repetition algorithm or note-to-card generation.

## Decisions

### 1. Component Boundary & Props/Emits Interface

The monolithic reader is refactored into modular components following Vue 3 Composition API best practices (`<script setup lang="ts">`):

```
+--------------------------------------------------------------------------+
|                     pages/read/[bookId].vue                              |
|                                                                          |
|  +--------------------------------------------------------------------+  |
|  |                 ReaderHeaderBar.vue                                |  |
|  |  [< Library] [Contents]  Book Title - Chapter  [Aa] [Theme] [Quiz]  |  |
|  +--------------------------------------------------------------------+  |
|                                                                          |
|  +---------------------+  +-------------------------------------------+  |
|  | ReaderTocSidebar    |  | Main Reading Canvas (#main)               |  |
|  | - Search filter     |  | - Key Takeaways Callout                   |  |
|  | - Slices list       |  | - Prose Markdown (MarkdownIt + Shiki)    |  |
|  | - Completion checks |  | - Floating Toolbar (Selection/Note)      |  |
|  | (Desktop / Mobile)  |  | - ReaderNavigationCards.vue               |  |
|  +---------------------+  +-------------------------------------------+  |
+--------------------------------------------------------------------------+
```

- **`ReaderHeaderBar.vue`**:
  - **Props**: `book: BookDetail | null`, `currentChunk: ChunkSummary | null`, `currentChunkOrder: number`, `totalChunks: number`, `progressPercent: number`, `isTocOpen: boolean`.
  - **Emits**: `toggle-toc`, `open-mobile-toc`, `launch-quiz`.
  - **Internal state**: Embeds typography popover (`Aa`) interacting with `useReaderTypography()`, and theme toggle via `ThemeToggle.vue`.
- **`ReaderTocSidebar.vue`**:
  - **Props**: `book: BookDetail | null`, `chunks: ChunkSummary[]`, `activeChunkOrder: number`, `completedSlices: Set<number>`, `isOpen: boolean`, `isMobile: boolean`.
  - **Emits**: `select-slice: (order: number) => void`, `close-mobile-toc: () => void`.
- **`ReaderNavigationCards.vue`**:
  - **Props**: `prevChunk: ChunkSummary | null`, `nextChunk: ChunkSummary | null`, `isFinalSlice: boolean`.
  - **Emits**: `navigate-prev`, `navigate-next`, `return-library`.
- **Orchestrator (`read/[bookId].vue`)**:
  - Retains data fetching (`libraryStore`, `notesStore`), active slice route watcher, lookahead prefetching (2.5s debounce), floating text selection toolbar, and reflection note modals.

### 2. Centered Prose Layout & Responsive Density

- The reading prose container in `#main` utilizes dynamic class binding from `useReaderTypography()`:
  - Standard: `max-w-3xl mx-auto`
  - Wide: `max-w-4xl mx-auto`
  - Full: `max-w-full mx-auto`
- Horizontal gutter padding is standardized to `px-4 sm:px-8 md:px-12` ensuring that when the TOC sidebar collapses or expands, the article does not jump or skew horizontally.
- All action buttons and badge pills enforce `whitespace-nowrap shrink-0` across English and Vietnamese locales to satisfy the Bilingual Responsive Layout Invariant.

### 3. Studio Cockpit Spacing on `/today`

- Harmonize `/today` grid and container wrappers:
  - Header pacer bar: `px-3 sm:px-6 py-2 sm:py-3` with crisp border separation.
  - Multi-column studio: `gap-4 lg:gap-6` between Left Rail (`Outline`), Center Canvas (`DocReaderPane`), and Right Dock (`InterviewChallengePane`).
  - Mobile bottom navigation bar / tab bar: minimum touch height 44px with fixed icons and localized labels.

## Risks / Trade-offs

- **Risk: Component Event Synchronization**: Emitting slice changes from `ReaderTocSidebar` and `ReaderNavigationCards` could cause out-of-sync route navigation.
  - **Mitigation**: Route changes remain owned by `router.push({ query: { slice: order } })` in the parent orchestrator, with slice changes reacting via route query watchers.
- **Risk: Mobile Touch Gestures & Body Scroll Lock**: An open mobile TOC drawer could allow background body scrolling.
  - **Mitigation**: Use `useEventListener` and conditional body overflow management or VueUse `useScrollLock` to cleanly prevent background scroll while the mobile drawer is open.
- **Risk: Test Regressions**: Existing tests asserting on monolithic reader classes.
  - **Mitigation**: Maintain identical IDs and ARIA labels (`#main`, `#btn-toc`, navigation links) and write new focused Vitest specs for each extracted component.
