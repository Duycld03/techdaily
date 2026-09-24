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

### Requirement: Document Chunk and Library Slice Projections
The system SHALL serve document chunk and library slice DTOs (`DailyFocusChunkDto`, `BookSliceDto`) without serializing or allocating `MicroQuiz` objects.

#### Scenario: User queries today's reading slice
- **WHEN** an authenticated user sends `GET /api/v1/daily/today`
- **THEN** the returned `DailyFocusChunkDto` contains `id`, `chapterTitle`, `summaryMarkdown`, `originalTextMarkdown`, `keyTakeaways`, and `estimatedReadMinutes`, omitting any `microQuiz` key.

#### Scenario: User queries book slice from library
- **WHEN** a client sends `GET /api/v1/library/books/{id}/slices/{sliceOrder}`
- **THEN** the returned `BookSliceDto` contains chapter metadata, original markdown, and key takeaways without `microQuiz` fields.

### Requirement: Repository Branch & Deployment Environment Hygiene
The GitHub repository configuration and remote branches SHALL reflect only official production infrastructure and active development streams:
1. **Remote Prototype Branch Pruning**: Transient prototype branches created by external third-party generation tools (e.g. `v0/*`) MUST be pruned from the remote repository once prototyping exploration concludes.
2. **Canonical Homepage Resolution**: The repository homepage metadata SHALL point exclusively to the authenticated production deployment host (`https://techdaily.duckdns.org`) or remain unset, prohibiting stray staging or external preview SaaS URLs.
3. **Deployment Environment Isolation**: GitHub Deployment Environments SHALL represent verified, operational application deployments. External automated preview bots (such as `vercel[bot]`) that are no longer part of the production deployment pipeline MUST be purged and disconnected to prevent phantom builds.

#### Scenario: Inspecting repository deployment and branch metadata
- **WHEN** an engineer or visitor views the repository homepage and deployment sidebar on GitHub
- **THEN** no stray `v0/*` branches appear in remote branches, the homepage URL points to the canonical VPS host, and third-party preview deployment widgets are absent.
