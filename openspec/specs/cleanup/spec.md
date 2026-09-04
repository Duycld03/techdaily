# Cleanup Specification

## Purpose
Establishes clean architectural boundaries, 3-tier grouped navigation standards, seamless reader-to-quiz practice bridges, and database schema pruning of legacy unused features.

## Requirements

### Requirement: Grouped Navigation Standard
Both desktop sidebar (`components/layout/AppSidebar.vue`) and mobile drawer (`components/layout/AppHeader.vue`) MUST adhere to the standardized 3-tier grouped layout: Practice (`/today`, `/roadmap`, `/quiz`, `/review`), Knowledge (`/insights`, `/library`, `/notes`), and System (`/profile`, `/settings`).

#### Scenario: User opens desktop sidebar navigation
- **WHEN** user views application on desktop viewport
- **THEN** sidebar renders navigation items grouped into Practice, Knowledge, and System sections with translated group headers.

#### Scenario: User opens mobile navigation drawer
- **WHEN** user taps hamburger menu on mobile viewport
- **THEN** mobile drawer displays matching 3-tier grouped navigation structure with parity to desktop sidebar.

---

### Requirement: Reader-to-Quiz Bridge Standard
When viewing a book chapter in `/read/[bookId]`, the reader view MUST offer a prominent button to practice quizzes on that chapter. Navigating to `/quiz?topic=<topic>` MUST automatically populate the Quiz Arena topic input.

#### Scenario: User clicks practice quiz from reader
- **WHEN** user clicks "Quiz This Chapter" in `/read/[bookId]`
- **THEN** application navigates to `/quiz` with query parameter `topic={chapterTitle}` and pre-fills topic input field.

---

### Requirement: Database & Entity Standard
The `DailyDrills` entity contains only core drill fields (`UserId`, `QuestionId`, `DocumentChunkId`, `ScheduledDate`, `Status`, `SelectedOptionIndex`, `IsCorrect`, `Score`, `AttemptCount`, `SubmittedAt`). Legacy `AiReviews` table and audio storage columns MUST be removed from domain and infrastructure models.

#### Scenario: Developer queries database entities
- **WHEN** inspecting database context and migrations
- **THEN** `AiReviews` table and legacy audio columns are removed and `DailyDrills` contains only required drill attempt fields.
