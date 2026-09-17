# Proposal: Unified Roadmap Active Track

## Why

### Executive Summary
The `/roadmap` page (`frontend/pages/roadmap.vue`) currently presents two competing, mutually exclusive top-level tabs: `[ Active Book ]` and `[ Switch to Curriculum ]`. When an active book pacer exists, these tabs duplicate header metrics, split user attention, and create severe semantic confusion. In TechDaily's unified learning architecture, the 30-day curriculum is simply a seeded demo track, while user-imported books (PDF uploads, crawled web documentation series) are first-class learning tracks. 

Instead of forcing users to toggle between two disconnected views with duplicated headers, `/roadmap` must present a single, cohesive timeline view directly synchronized with the user's active learning track on `/today`. A clean "Track Switcher" dropdown in the header allows users to inspect their active book, preview any other document in their library, or review the 30-day curriculum track, all within one consistent visual hierarchy.

---

### Detailed Problem Statement

An audit of `frontend/pages/roadmap.vue` and user interaction patterns reveals four critical UX and architectural defects:

1. **Dual-Tab Redundancy & Competing Views:**
   At lines 324–351 in `frontend/pages/roadmap.vue`, the page renders a tab switcher:
   ```html
   <div v-if="focusStore.data?.pacer" class="flex items-center gap-2 p-1.5 bg-slate-100 dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl w-fit">
     <button @click="activeMode = 'book'">Active Book</button>
     <button @click="activeMode = 'curriculum'">Switch to Curriculum</button>
   </div>
   ```
   Both Section A (`activeMode === 'book'`) and Section B (`activeMode === 'curriculum'`) render nearly identical header banners with their own gradient backgrounds, duplicate sprint/progress metric counter cards (`Award` icon, progress fractions, completion percentages), and duplicate global progress bars. This creates visual clutter and redundant DOM structure.

2. **Cognitive Load & Mental Model Fragmentation:**
   Users landing on `/roadmap` are presented with a binary choice before seeing their actual learning progress. If a user is actively reading an ingested technical book (e.g. *Designing Data-Intensive Applications* or *PostgreSQL 17 Internals*), the presence of `[ Switch to Curriculum ]` confuses them as to whether their daily streak and drill progress belong to the book or the curriculum. The interface implies two disparate systems rather than one continuous learning journey.

3. **Semantic Inconsistency (Demo Track vs. First-Class Tracks):**
   In TechDaily's domain model, the original 30-Day Senior Curriculum represents a seeded default track for new visitors. Uploaded PDFs, crawled documentation, and markdown books are equally first-class learning tracks driven by the same reading pacer engine (`PacerInfo` in `useDailyFocusStore`). Segregating "Book" and "Curriculum" into separate macro pages contradicts the platform's unified track philosophy.

4. **Asymmetry with `/today`'s Pacer Header:**
   On `/today` (`frontend/pages/today.vue`), users switch between in-progress reading tracks seamlessly using a sleek 1-click book switcher dropdown (`focusStore.data.pacer.availableBooks`). However, `/roadmap` lacks this switcher, preventing users from inspecting the roadmaps of their other in-progress library books without first navigating back to `/today` or `/library` to switch books.

---

## What Changes

We propose a unified, track-synchronized roadmap experience that eliminates the competing tabs and establishes complete parity with `/today`:

```
BEFORE (Competing Tabs & Duplicate Headers):
┌────────────────────────────────────────────────────────────┐
│ [ Active Book ]  [ Switch to Curriculum ]   <- Redundant   │
├────────────────────────────────────────────────────────────┤
│ ┌────────────────────────────────────────────────────────┐ │
│ │ 📖 Active Book: Designing Data-Intensive Applications  │ │
│ │ Progress: 14/42 slices (33%) [========>              ] │ │
│ └────────────────────────────────────────────────────────┘ │
│ Chapter Milestones ...                                     │
└────────────────────────────────────────────────────────────┘

AFTER (Single Unified View with Synchronized Track Switcher):
┌────────────────────────────────────────────────────────────┐
│ ┌────────────────────────────────────────────────────────┐ │
│ │ 🗺️ LỘ TRÌNH HỌC • [ 📖 Designing Data-Intensive... ▾ ] │ │
│ │ Chương 3: Storage & Retrieval                          │ │
│ │ Tiến Độ: 14/42 lát cắt (33%) • Còn 28 ngày             │ │
│ │ [=====================>                              ] │ │
│ └────────────────────────────────────────────────────────┘ │
│ [Search Chapters...] [Expand All] [Collapse All]           │
│ ▾ Chapter 1: Reliability, Scalability, Maintainability     │
│ ▾ Chapter 2: Data Models & Query Languages                 │
│ ▸ Chapter 3: Storage & Retrieval (Active Today 🔥)         │
│   ├── Slice 14: LSM-Trees vs B-Trees [Start Drill ⚡]      │
│   └── Slice 15: Indexing in PostgreSQL [Read 📖]           │
└────────────────────────────────────────────────────────────┘
```

### Key Architectural & UX Changes

1. **Eliminate Competing Tabs:**
   - Remove the `activeMode` binary toggle (`'book' | 'curriculum'`) and the redundant tab bar.
   - `/roadmap` presents a single, authoritative roadmap container.

2. **Unified Header with Synchronized Track Switcher Dropdown:**
   - Integrate a versatile "Track Switcher" pill directly into the primary header banner, synchronized with `/today`'s active book state.
   - The dropdown displays:
     - **Active Track:** Marked with a bold `Active` badge and real-time progress bar.
     - **In-Progress Library Books:** Lists other books the user has started reading with slice progression indicators.
     - **30-Day Senior Curriculum:** Offers 1-click inspection of the foundational 30-day curriculum track with module counts and progress.
     - **+ Browse Library (`/library`):** Quick navigation bridge to discover and ingest new material.
   - Selecting any track smoothly updates the roadmap timeline without full page reloads. Selecting an in-progress book invokes `focusStore.switchBook()`, keeping `/today` and `/roadmap` in lockstep.

3. **Cohesive Chapter Milestones & Slice Visualizations:**
   - For document books, render sequential chapter milestone accordions with slice completion indicators, search filtering, and batch expand/collapse controls.
   - For the 30-day curriculum track, render the 4 core curriculum modules with day nodes and difficulty indicators.

4. **1-Click Action Bridges:**
   - **Active Slice Node:** 1-click bridge to `/today?bookId={id}&chunkOrder={order}` (or `/today?day={day}`), immediately opening today's reading and AI scenario challenge.
   - **Completed Slice Node:** 1-click bridge to review the architecture on `/today` or inspect full text in `/read/{bookId}?slice={order}`.
   - **Upcoming Slice Node:** 1-click preview in `/read/{bookId}?slice={order}`.

5. **Bilingual Support & Layout Invariant Compliance:**
   - Full localization in `en.json` and `vi.json` for all track switcher controls, badges, and milestone labels.
   - Enforce `whitespace-nowrap shrink-0` and responsive gap layout on all action buttons and badges to prevent text wrapping collisions across English and Vietnamese.

---

## Capabilities

### Modified Capabilities
- `roadmap`: Refactors requirement `Curriculum Roadmap Progression & Macro View` and adds requirement `Active Track Synchronization & Switcher` to unify the roadmap interface into a single timeline view synchronized with the active learning track on `/today`, providing an integrated track switcher dropdown and direct 1-click action bridges to daily drills and reading views.

---

## Impact

- **Frontend Template (`frontend/pages/roadmap.vue`):** 
  - Removes the dual-tab toggle markup and duplicate banners.
  - Adds the unified header banner with reactive track switcher dropdown and click-outside dismissal.
  - Unifies metric counters and global progress bars.
  - Preserves Chapter Milestone accordions, search filtering, and slice cards.
- **Frontend Stores (`useDailyFocusStore`, `useLibraryStore`, `useRoadmapStore`):**
  - Seamless state coordination when switching tracks via dropdown.
- **Frontend Localization (`frontend/i18n/locales/en.json`, `frontend/i18n/locales/vi.json`):**
  - Adds track switcher translation keys (`roadmap.track_switcher_label`, `roadmap.in_progress_tracks`, `roadmap.curriculum_track`, `roadmap.curriculum_track_desc`, `roadmap.browse_library`, `roadmap.active_track_badge`).
- **Frontend Test Suite (`frontend/tests/pages/roadmap.spec.ts`):**
  - Adds comprehensive Vitest tests verifying unified header rendering, track switcher dropdown interactions, state synchronization, and navigation bridges.
- **Backend & APIs:**
  - Zero backend changes. Fully reuses existing endpoints (`GET /api/v1/curriculum/roadmap`, `GET /api/v1/daily/today-focus`, `POST /api/v1/daily/switch-book`, `GET /api/v1/library/books/{id}`).

---

## Scope & Non-Goals

### In Scope
- Refactoring `frontend/pages/roadmap.vue` into a single, cohesive view.
- Integrating the Track Switcher dropdown into the roadmap header.
- Synchronizing track state across `useDailyFocusStore`, `useLibraryStore`, and `useRoadmapStore`.
- Direct 1-click bridge actions linking roadmap nodes to `/today` and `/read/[bookId]`.
- Bilingual strings in `en.json` and `vi.json`.
- Comprehensive Vitest unit and integration tests in `frontend/tests/pages/roadmap.spec.ts`.

### Non-Goals
- Changing backend database schemas or C# application handlers.
- Modifying the `/read/[bookId]` reader layout or markdown rendering engine.
- Modifying the spaced repetition flashcard algorithms or quiz grading mechanics.
- Altering the 30-day curriculum content structure.
