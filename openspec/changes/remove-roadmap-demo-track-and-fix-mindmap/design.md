# Design: Remove Roadmap Demo Track & Fix Mindmap Layout Collisions

## Context

See `proposal.md - Why` for motivation.

In `frontend/pages/roadmap.vue`, the track switcher dropdown currently renders:
1. "TÀI LIỆU ĐANG ĐỌC" listing the user's active document books (e.g. `30-Day Senior Fullstack Curriculum` at 40%, `ASP.NET Core 10 Architecture Guide` at 4%).
2. A separate, hardcoded "Starter Pack / Demo Curriculum" track option (`track-curriculum-option`, "Lộ Trình Mẫu: Senior Fullstack (Demo)") with divergent progress (e.g. 1/30, 3.3%).
3. When toggling the Mindmap View on this demo track, `convertCurriculumToTree` in `frontend/utils/roadmapTreeLayout.ts` calculates chapter index as `mod.category + 1`. Because `mod.category` is a string (e.g., `"FrontendWeb"`), JavaScript performs string concatenation (`"FrontendWeb1"`). This causes text to overflow the 32x32px badge and collide directly over the chapter title.

## Goals / Non-Goals

**Goals:**
- Transition `/roadmap` to a pure document-first architecture where every learning roadmap maps directly to a document book.
- Remove the redundant demo curriculum option from the track switcher dropdown, eliminating duplicate tracks and progress confusion.
- Fix the branch index calculation in `roadmapTreeLayout.ts` so chapter index badges always render clean sequential integers (`1`, `2`, `3`, `4`).
- Prevent any badge text overflow or title collision in `RoadmapMindmapCanvas.vue`.
- Provide an inviting empty state when the user has zero reading books in progress, linking directly to `/library`.

**Non-Goals:**
- Deleting the backend curriculum seed JSON file or removing the seeded `DocumentBook` from PostgreSQL.
- Removing or altering the backend `/api/v1/curriculum/roadmap` endpoint (can be retained for background metrics or future onboarding wizards).
- Redesigning the mindmap tree physics or pan/zoom mechanics.

## Decisions

### Decision 1: Remove Demo Curriculum Track Option from Track Switcher
In `frontend/pages/roadmap.vue`:
- Remove the `track-curriculum-option` button block (lines 617-644).
- The track switcher popover will exclusively display:
  - Header: In-Progress Document Tracks ("Tài Liệu Đang Đọc").
  - List of real document books from `availableBookTracks`.
  - Footer: "+ Khám phá Thư Viện" navigation link to `/library`.
- If `availableBookTracks` has books, the active track defaults to `focusStore.data.pacer.bookId` or the first available book.
- If `availableBookTracks` is empty, the page renders a clean empty state with a call-to-action button to choose a book from the library.

### Decision 2: Fix Chapter Branch Index Calculation in `roadmapTreeLayout.ts`
In `frontend/utils/roadmapTreeLayout.ts`:
- Update `convertCurriculumToTree`:
  ```ts
  const chapters: TreeChapterBranch[] = (curriculum.modules || []).map((mod, idx) => {
    ...
    return {
      id: String(mod.category),
      index: idx + 1, // Clean integer: 1, 2, 3, 4
      title: mod.moduleTitle,
  ```
- This guarantees that whether a tree is generated from a book or curriculum, `ch.data.index` is always a clean numeric integer.

### Decision 3: Defensive Mindmap Badge Styling in `RoadmapMindmapCanvas.vue`
In `frontend/components/roadmap/RoadmapMindmapCanvas.vue`:
- Ensure the chapter index badge has `w-8 h-8 rounded-xl shrink-0 overflow-hidden flex items-center justify-center font-bold text-xs`.
- Ensure chapter title text container has `min-w-0 flex-1 truncate` with clear gap spacing (`gap-2.5`), preventing any text from overlapping.

## Risks / Trade-offs

- **Risk:** Existing tests in `roadmap.spec.ts` asserting `track-curriculum-option` exists.
  - **Mitigation:** Update unit tests to verify the document-first track switcher behavior, ensuring tests assert real document tracks and library discovery links.
- **Risk:** User bookmarks a direct URL with `?track=curriculum`.
  - **Mitigation:** In `onMounted`, if `queryTrack === 'curriculum'` or no book is selected, find the seeded `30-Day Senior Fullstack Curriculum` book in library or `availableBookTracks` and select its book ID.
