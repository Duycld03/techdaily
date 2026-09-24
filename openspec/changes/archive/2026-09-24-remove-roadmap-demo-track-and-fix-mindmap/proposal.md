# Proposal: Remove Roadmap Demo Track & Fix Mindmap Layout Collisions

## Why

The `/roadmap` interface features a legacy, hardcoded "Starter Pack / Curriculum Demo Track" (`Lộ Trình Mẫu: Senior Fullstack (Demo)`) that duplicates the real ingested `30-Day Senior Fullstack Curriculum` document book present in the user's library. This causes confusing track duplication in the track switcher dropdown with divergent progress tracking (e.g., 1/30 vs 12/30). Furthermore, rendering this demo track on the interactive mindmap canvas triggers an index calculation bug where category string keys (`"FrontendWeb"`) are string-concatenated (`mod.category + 1` -> `"FrontendWeb1"`), overflowing the 32px badge and colliding directly over chapter branch titles. Removing the legacy demo track and fixing the index calculation ensures clean, document-first roadmap navigation and flawless mindmap rendering.

## What Changes

- **Document-First Roadmap Orchestration (`frontend/pages/roadmap.vue`)**:
  - Remove the legacy hardcoded demo curriculum option (`track-curriculum-option`, `isCurriculumSelected` legacy toggle) from the track switcher dropdown.
  - Default the active roadmap track directly to the user's current reading book (`focusStore.data.pacer.bookId` or the first available book in `focusStore.data.pacer.availableBooks`), with an empty/guidance state prompting the user to select or upload a book from `/library`.
  - Streamline track switcher dropdown to list only real document tracks (`availableBookTracks`) with accurate progress bars, active indicators, and a clean "+ Khám phá Thư Viện" bridge.
- **Mindmap Branch Index & Layout Collision Fix (`frontend/utils/roadmapTreeLayout.ts`)**:
  - Fix branch index derivation in `convertCurriculumToTree` (and general tree converters) to use array enumeration index `idx + 1` rather than string concatenation `mod.category + 1`.
  - Ensure all chapter and branch node index badges receive clean integer strings (`1`, `2`, `3`, `4`), completely eliminating badge text overflow and title collisions.
- **Mindmap Canvas Polish (`frontend/components/roadmap/RoadmapMindmapCanvas.vue`)**:
  - Enforce `overflow-hidden text-ellipsis` and `max-w` constraints on chapter branch node titles and subtitle badges.

## Capabilities

### New Capabilities
*None.*

### Modified Capabilities
- `roadmap`: Refine roadmap track selection to be document-first, eliminating the redundant demo curriculum track and fixing mindmap branch index calculations.

## Impact

- **Frontend Code**: `frontend/pages/roadmap.vue`, `frontend/utils/roadmapTreeLayout.ts`, `frontend/components/roadmap/RoadmapMindmapCanvas.vue`.
- **Localization**: Clean up or deprecate unneeded demo track keys in `frontend/i18n/locales/en.json` and `vi.json`.
- **Tests**: `frontend/tests/pages/roadmap.spec.ts`, `frontend/tests/utils/roadmapTreeLayout.spec.ts`, `frontend/tests/components/roadmap/RoadmapMindmapCanvas.spec.ts`.
- **Breaking Changes**: Zero breaking changes. Backend endpoints remain unaffected.
