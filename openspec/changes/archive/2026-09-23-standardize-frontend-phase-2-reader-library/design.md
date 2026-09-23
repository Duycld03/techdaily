# Design

## Context

See `proposal.md` for motivation. The Reader Studio (`pages/read/[bookId].vue`), Document Library (`pages/library.vue`), and Study Notes (`pages/notes.vue`) provide intensive reading and curation workflows. Under `antfu/skills` guidelines, the reading pane and typography composable must be updated for `h-dvh` viewport stability, VueUse event hygiene, and 320px responsive controls.

## Goals / Non-Goals

**Goals:**
- Replace `h-screen` in reader layout containers with `h-dvh` to avoid clipping on mobile devices.
- Refactor `useReaderTypography.ts` line 111 from manual `window.addEventListener('storage')` to VueUse `useEventListener`.
- Standardize the typography setting popover (`Aa`) with responsive segmented controls that adapt cleanly on 320px viewports.
- Ensure the PDF upload modal in `library.vue` uses responsive modal constraints (`max-w-lg w-full max-h-[85dvh]`).

**Non-Goals:**
- Changing Markdown-it rendering algorithms or Shiki code-highlighting themes.
- Modifying backend book slicing or indexing APIs.

## Decisions

### 1. `h-dvh` Viewport Adoption for Immersive Mode
- *Rationale*: Reader mode occupies 100% of the viewport. On mobile browsers, `100vh` calculates height based on visible chrome, clipping the bottom navigation bar when chrome expands. `h-dvh` solves this.

### 2. VueUse `useEventListener` in Composables
- *Rationale*: Manual `window.addEventListener('storage')` requires manual teardown and can cause leaks in SSR/SPA lifecycles. VueUse handles this automatically.

### 3. Responsive Typography Popover
- *Rationale*: Segmented button groups need `flex-wrap sm:flex-nowrap` or `grid` to prevent button truncation when translating to Vietnamese.

## Risks / Trade-offs

- **Risk**: `dvh` support in older browsers.
  - **Mitigation**: Tailwind CSS provides fallback behavior; modern target browsers (Chrome 108+, Safari 15.4+) fully support dynamic viewport units.
