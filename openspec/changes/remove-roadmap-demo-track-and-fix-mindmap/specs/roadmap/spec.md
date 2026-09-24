# Spec Delta: roadmap

## MODIFIED Requirements

### Requirement: Document-First Roadmap Progression & Track Switching
The system SHALL provide an interactive roadmap progression view at `/roadmap` that operates in a document-first paradigm, prioritizing the user's active learning materials:

1. **Active Track Resolution**: The roadmap SHALL automatically select the user's currently active document book (`focusStore.data.pacer.bookId` or the first item in `focusStore.data.pacer.availableBooks`), eliminating the separate hardcoded demo starter pack track.
2. **Unified Track Switcher**: The track switcher dropdown SHALL list the user's in-progress and available document books (`availableBookTracks`), displaying each book's title, active status badge, completion count, and percentage progress bar.
3. **Library Discovery Bridge**: The track switcher SHALL provide a direct "+ Khám phá Thư Viện" navigation link to `/library`, allowing users to select or upload new books.
4. **Empty State Guidance**: When the user has no active document books in progress, the roadmap SHALL present an encouraging empty state with a 1-click CTA leading to `/library` rather than falling back to an unconfigurable mock demo track.

#### Scenario: User navigates to roadmap with active book in progress
- **GIVEN** an authenticated user whose active pacer is set to a document book (e.g., `30-Day Senior Fullstack Curriculum` or `ASP.NET Core 10 Architecture Guide`)
- **WHEN** the user visits `/roadmap`
- **THEN** the roadmap header and milestone track display the active book's chapter milestones and real progress
- **AND** the track switcher dropdown highlights the active document book without displaying a duplicate demo track.

#### Scenario: User switches active track between reading documents
- **WHEN** user opens the track switcher dropdown on `/roadmap` and selects another document book
- **THEN** the roadmap timeline and mindmap update smoothly to reflect the selected book's chapter milestones and slice progress.

### Requirement: Hierarchical Mindmap Interactive View
The interactive hierarchical mindmap view (`RoadmapMindmapCanvas.vue`) SHALL ensure clean, collision-free branch node presentation:

1. **Sequential Numeric Branch Index**: Every chapter branch node SHALL render a clean, sequential integer index badge (`1`, `2`, `3`, `4`, ...) representing its chapter order. Branch index values SHALL NOT contain raw category enum strings or concatenated string values.
2. **Badge & Title Collision Prevention**: The 32px chapter index badge (`w-8 h-8 rounded-xl`) SHALL hold only the numeric chapter index without text overflow, ensuring zero visual overlap with the chapter title or slice completion counts.
3. **Title Truncation & Responsive Density**: Chapter titles and slice node labels SHALL enforce strict truncation with ellipsis (`truncate`), preserving legible spacing on 1080p, 2K, and mobile viewports.

#### Scenario: User opens mindmap view for multi-chapter document
- **WHEN** user toggles to the Mindmap View on `/roadmap`
- **THEN** all chapter branch nodes display numeric badges (`1`, `2`, `3`, ...) cleanly separated from chapter titles
- **AND** no category strings (such as `FrontendWeb` or `BackendRuntime`) overflow or overlap with node text.
