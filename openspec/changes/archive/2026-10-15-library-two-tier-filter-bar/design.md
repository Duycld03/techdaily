# Design

## Context

In `frontend/app/pages/library.vue`, the Document Library uses `BoardLayout.vue` which exposes `#header`, `#filters`, `#content`, and `#pagination` slots. Within the `#filters` slot, `currentTab === 'mine'` currently houses both the search input and the category pill array in a single flex container (`flex flex-col sm:flex-row sm:items-center justify-between gap-3 w-full py-0.5`). 

Because DeepPace represents 7 senior engineering tracks (Frontend, Backend, Database, System Design, Engineering Craft, Mental Models, Habits & Productivity) plus "All", displaying all 8 chips with localized strings alongside a search box (`sm:w-72`) forces horizontal overflow that wraps across 2–3 vertical rows. This misaligns the search input vertically, expands the toolbar unpredictably, and damages the visual hierarchy.

## Goals / Non-Goals

**Goals:**
- Implement a clean 2-Tier Header architecture inside `#filters` for `currentTab === 'mine'`:
  - **Tier 1**: Dedicated search input row with quick clear (`X`) and active filter reset trigger.
  - **Tier 2**: Full-width category chip carousel displaying all 7 DeepPace categories plus "All".
- Guarantee zero vertical line-wrapping on any viewport size (`whitespace-nowrap flex-nowrap overflow-x-auto no-scrollbar`).
- Preserve all 7 senior engineering learning disciplines on the main surface without hiding non-coding pillars in modal submenus.
- Maintain smooth color transitions and zero width jitter or layout shifts (`font-semibold` on both active and inactive states).

**Non-Goals:**
- Modifying the Discover Catalog tab (`currentTab === 'discover'`), which retains its existing quick-filter + modal structure tailored for catalog browsing.
- Modifying backend API contracts or PostgreSQL schema (the existing `GET /api/v1/library/books` endpoint already handles `category` and `search` parameters).

## Decisions

### Decision 1: Two-Tier Header Isolation over Modal Submenus
- **Choice**: Separate search input onto Tier 1 and category chips onto Tier 2.
- **Rationale**: DeepPace differentiates itself from ordinary coding interview platforms through its emphasis on Engineering Craft, Mental Models, and Habits/Deep Work. Hiding these 3 categories behind a "Bộ Lọc" modal would treat them as second-class tracks. A 2-tier layout gives each control its natural geometry: search expands comfortably, and categories span the full width.
- **Alternatives Considered**:
  - *4 Quick Pills + Filter Modal* (Disliked by user because senior developer skills should be visible and 1-click accessible).
  - *Grouped Dropdown / Segmented Button* (Adds extra click friction to category switching).

### Decision 2: Horizontal Carousel (`overflow-x-auto no-scrollbar`) for Tier 2
- **Choice**: Apply `whitespace-nowrap flex-nowrap overflow-x-auto no-scrollbar` to the category container.
- **Rationale**:
  - On desktop (1080p, 1440p): The full-width container comfortably displays all 8 pills on a single horizontal line with ample breathing room.
  - On mobile/tablet (390px, 768px): The row scrolls horizontally with native touch gesture support, preventing multi-row layout distortion and keeping toolbar height deterministic.
- **Alternatives Considered**:
  - *Flex wrap*: The root cause of the bug being fixed.

### Decision 3: Tier 1 Quick Reset Action
- **Choice**: Provide an inline "Reset" action when `selectedCategory !== undefined || searchQuery`.
- **Rationale**: When users are browsing on narrow viewports and have scrolled horizontally past the "All" pill, this allows a 1-tap reset to view the entire library catalog without having to scroll back.

## Risks / Trade-offs

- **[Risk] Incremental vertical space usage (~36px)**:
  → *Mitigation*: The 2-tier structure uses compact vertical padding (`py-0.5` and `gap-2.5`). The structured layout is far cleaner than wrapped pills that consumed 80px+ of chaotic vertical space previously.
- **[Risk] Horizontal scroll discoverability on narrow mobile screens**:
  → *Mitigation*: Category chips at the right viewport edge clip naturally, communicating scrollability without requiring obstructive scrollbars.
