# Curated Catalog Specification

## Purpose
Enable administrators to prepare and publish selected documents that authenticated learners can preview and explicitly add to their personal libraries.

## Requirements

### Requirement: Administrator draft ingestion
Administrators SHALL create catalog drafts from uploaded PDFs or web URLs with title, description, category, source language, and optional source attribution. The system SHALL NOT mandate bureaucratic compliance records such as permission basis, permission evidence, or explicit distribution rights checkboxes. The category selection SHALL expose all 7 official DeepPace categories including `MentalModels` and `HabitsProductivity`. Draft processing SHALL preserve source language and use existing upload, extraction, URL validation and sanitization boundaries.

#### Scenario: Administrator uploads a PDF
- **WHEN** an administrator submits a valid PDF and metadata
- **THEN** the system accepts processing asynchronously and returns a draft identifier and processing status
- **AND** the draft is absent from learner discovery and personal libraries.

#### Scenario: Administrator imports a web source
- **WHEN** an administrator explicitly imports a validated URL and confirms its preview
- **THEN** the system extracts and slices the selected article or detected PDF into a catalog draft
- **AND** redirects and detected PDF targets receive the same SSRF checks as personal imports.

#### Scenario: Invalid input
- **WHEN** a PDF exceeds existing limits, is invalid, or a URL targets a prohibited address
- **THEN** processing is rejected with a structured error and no publishable entry is created.

#### Scenario: Admin creates catalog draft with essential metadata only
- **WHEN** an administrator uploads a PDF or provides a web URL with title, category, and language without filling permission basis or evidence fields
- **THEN** the system accepts and creates the catalog draft in PostgreSQL without validation errors, defaulting internal compliance tracking cleanly.

#### Scenario: Admin selects habits or mental models category
- **WHEN** an administrator selects `HabitsProductivity` (e.g. for *Atomic Habits*) or `MentalModels`
- **THEN** the catalog draft records category 5 or 6 accurately, aligning with domain enums.

---

### Requirement: Explicit preparation and recovery
Administrators SHALL see processing progress and errors and explicitly initiate preparation or retry. Publication SHALL require all extracted slices to be readable and the first min(3, slice count) slices successfully formatted. Remaining slices SHALL retain readable source content for existing on-demand curation.

#### Scenario: Preparation fails
- **WHEN** initial formatting fails, including missing AI configuration
- **THEN** the draft remains unpublished and shows a recoverable error
- **AND** no fabricated fallback is treated as successful preparation.

#### Scenario: Retrying interrupted work
- **WHEN** an administrator retries a failed or interrupted preparation
- **THEN** the system prevents concurrent duplicate processing and duplicate slice orders
- **AND** requests a replacement upload if the original temporary file is unavailable.

#### Scenario: Viewing a draft does not generate content
- **WHEN** an administrator opens a draft or changes filters
- **THEN** no new AI generation begins without an explicit preparation action.

---

### Requirement: Reviewed publication lifecycle
Catalog entries SHALL support Draft, Published and Hidden visibility. Publication SHALL require prepared readable slices and basic attribution. Publication SHALL NOT require a separate prerequisite administrator review record (`ReviewedByUserId`) or permission compliance proofs; an administrator's direct publish action SHALL constitute explicit review and authorization. Missing requirements SHALL block publication with actionable errors.

#### Scenario: Publish a prepared draft
- **WHEN** an administrator confirms review and publishes a ready draft with complete metadata
- **THEN** it becomes discoverable and the system records the publishing administrator and time.

#### Scenario: Publish an incomplete draft
- **WHEN** an administrator publishes an entry with incomplete initial slices
- **THEN** the system rejects publication and identifies the unmet requirements.

#### Scenario: Hide or edit a published entry
- **WHEN** an administrator hides a published entry
- **THEN** it disappears from learner listing, preview and new adoption
- **AND** existing personal copies remain usable.
- **WHEN** the administrator needs to change its source, content or metadata
- **THEN** the entry must be hidden first and reviewed again before republication.

#### Scenario: Admin publishes document directly after slices are prepared
- **WHEN** a catalog document has its initial slices formatted and the administrator clicks Publish
- **THEN** the system immediately transitions the document to `Published` visibility without requiring a prior manual review sign-off step.

---

### Requirement: Authenticated discovery and bounded preview
Authenticated learners SHALL browse only published entries using paginated search and category, language and source-type filters. Details SHALL include attribution, description, slice count, reading-time estimate, table of contents and the first slice as a preview. Internal processing errors and permission evidence SHALL remain administrator-only.

#### Scenario: Filter and browse
- **WHEN** a learner searches by title or description and selects filters
- **THEN** matching published entries are returned with total count and pagination metadata
- **AND** a stable publication-date and identifier ordering prevents ambiguous page boundaries.

#### Scenario: Hidden entry requested directly
- **WHEN** a learner requests a draft, hidden or deleted entry by identifier
- **THEN** the response is 404 without exposing its metadata or content.

#### Scenario: Preview without adoption
- **WHEN** a learner opens a published entry
- **THEN** the first slice and table of contents are visible without adding a personal book, changing progress or invoking AI
- **AND** other full slices are not exposed by the learner catalog API.

---

### Requirement: Atomic and idempotent catalog adoption
A learner SHALL explicitly add a published document to their library as a prepared personal copy. Adoption SHALL return the owned book identifier, perform no external fetch or AI call, and create at most one active copy per learner and catalog entry. Failures SHALL leave no partial book or slices.

#### Scenario: First adoption
- **WHEN** a learner adds a published entry
- **THEN** a ready personal book and its readable slices are created with new identifiers and source attribution
- **AND** the response permits immediate reader navigation without extraction waiting.

#### Scenario: Duplicate or concurrent request
- **WHEN** the learner retries after a lost response or sends concurrent adoption requests
- **THEN** every successful response identifies the same active personal book
- **AND** existing progress is unchanged.

#### Scenario: Hidden during adoption
- **WHEN** hiding an entry commits before an adoption obtains its publication check
- **THEN** new adoption is rejected without creating a personal copy.

#### Scenario: Re-add after personal deletion
- **WHEN** a learner deletes their copy and later adds the still-published entry again
- **THEN** a new personal book is created with fresh progress
- **AND** deleted notes and previous progress are not restored.

---

### Requirement: Personal snapshot independence
Adopted content SHALL remain independent of administrator and other learner activity. Adoption SHALL copy document content and prepared learning content only, preserve attribution and source language, and never copy anyone's highlights, notes, drills, review cards, quiz progress, pacers or audio usage records.

#### Scenario: Two learners adopt one entry
- **WHEN** one learner edits or deletes their book or creates notes and flashcards
- **THEN** the other learner's content and learning state and the catalog entry are unchanged.

#### Scenario: Administrator republishes a changed document
- **WHEN** an administrator updates a hidden entry and republishes it
- **THEN** existing personal copies are unchanged
- **AND** subsequent first-time adoptions receive the currently published content.

---

### Requirement: Catalog interface states and accessibility
Discovery and administration SHALL provide English and Vietnamese UI, accessible controls and responsive layouts. Empty, loading, failed, prepared and already-added states SHALL have explicit actions; failures SHALL preserve entered metadata and allow appropriate retry.

#### Scenario: No published documents
- **WHEN** a learner opens an empty discovery catalog
- **THEN** the page offers personal import without placeholder books or automatic generation.

#### Scenario: Already added after refresh
- **WHEN** a learner reloads discovery after adoption
- **THEN** the entry shows an open-existing-book action instead of creating another copy.

#### Scenario: Bilingual desktop and mobile verification
- **WHEN** discovery, preview and admin forms are inspected in English and Vietnamese at desktop and 390px mobile widths
- **THEN** text and actions remain legible, keyboard accessible and free of clipping or collisions
- **AND** body text is at least 14px on mobile and 16px on larger screens.

---

### Requirement: Discover tab advanced filtering and quick categories
The system SHALL allow learners to discover published catalog documents through search and filtering. The discovery interface SHALL provide quick category pills alongside an Advanced Filter modal supporting category, source type, and language criteria with active filter counters and clear/reset actions.

#### Scenario: Quick category filtering
- **WHEN** a learner clicks a quick category pill on the discovery tab
- **THEN** the catalog list updates immediately with documents matching that category
- **AND** the active pill is visually highlighted with zero layout shifting.

#### Scenario: Opening advanced filter modal
- **WHEN** a learner clicks the advanced filter button
- **THEN** a dedicated modal opens displaying all available categories, source types (Web Docs, PDF Books, Markdown), and languages (English, Vietnamese)
- **AND** the learner can select multiple criteria, reset filters to default, or apply the selection.

#### Scenario: Active filter badge count
- **WHEN** one or more non-default filters are active in the discovery view
- **THEN** the advanced filter button displays a badge showing the count of active criteria.

---

### Requirement: Document preview and direct reader navigation
The system SHALL provide a bounded reading preview for published catalog documents without double scrollbars. For documents already present in the learner's personal library, the action SHALL navigate directly to the reader view for that document.

#### Scenario: Single-scroll preview modal
- **WHEN** a learner opens the document preview modal on desktop or mobile viewports
- **THEN** the modal shell does not display an outer vertical scrollbar
- **AND** the Table of Contents scrolls independently within its assigned height.

#### Scenario: Direct reader navigation for adopted documents
- **WHEN** a learner clicks the "Open Book" action on a catalog card or inside the preview modal for an already-adopted document
- **THEN** the application navigates directly to `/read/{bookId}` with navigation context
- **AND** deliberate practice can begin immediately without navigating through `/library`.

---

### Requirement: Modular catalog administration components
The catalog administration surface SHALL be partitioned into modular components with balanced layout geometry:
1. `AdminCatalogHeader`: Header actions with conditional "+ New Draft" visibility (hidden when draft form is open).
2. `AdminCatalogNavRail`: Navigation rail with expanded width (`md:w-84 lg:w-96`) displaying filter dropdowns without text truncation and without duplicate "Ready" status indicators on catalog cards.
3. `AdminCatalogDraftForm`: Minimal draft ingestion form restricted to essential metadata.
4. `AdminCatalogDocumentDetail`: Document inspection and direct publish actions without redundant review buttons.
5. `AdminCatalogSlicesReview`: Slice status listing without redundant review confirmation triggers.
6. `AdminCatalogSliceModal`: Slice preview modal.

#### Scenario: Navigating catalog document selection
- **WHEN** an administrator selects a document from the navigation rail
- **THEN** the document detail view mounts with prepopulated metadata and slice review state without re-rendering unrelated rail items.

#### Scenario: Switching between draft creation and document inspection
- **WHEN** an administrator clicks New Draft or selects an existing document
- **THEN** the active view switches cleanly between the ingestion form and the document inspection workspace while preserving filter and search criteria.

#### Scenario: Nav rail renders filters without text clipping
- **WHEN** the catalog administration page is viewed in Vietnamese
- **THEN** the filter select controls display full text ("Tất cả hiển thị", "Tất cả trạng thái") without ellipsis truncation on desktop viewports.

---

### Requirement: Complete bilingual localization of catalog administration
All user-visible text within the catalog administration interface SHALL use internationalization tokens (`$t` and `t`) resolved against English and Vietnamese locale dictionaries. This includes form labels, placeholders, dropdown filter options, slice review summaries, modal content, and all system toast notifications.

#### Scenario: Administrator views catalog in Vietnamese
- **WHEN** an administrator switches language to Vietnamese (`vi`)
- **THEN** all form labels, placeholders, filter options, badges, and feedback toasts render in Vietnamese without English fallbacks or untranslated tokens.

#### Scenario: Administrator triggers validation or async action
- **WHEN** an action triggers a toast notification (such as missing title, upload error, metadata save, or publication success)
- **THEN** the toast message renders in the user's selected language.

---

### Requirement: Responsive typography and layout collision prevention
Catalog administration views SHALL adhere to DeepPace typography standards. Body text, form input values, and descriptive summaries SHALL be at least `text-sm` (14px) on mobile and `text-base` (16px) on desktop/tablet viewports. Micro-text (`text-xs` / 12px) SHALL be restricted strictly to compact status pills and timestamps. All buttons, badges, and action bars SHALL enforce `whitespace-nowrap shrink-0` and responsive gap spacing.

#### Scenario: Responsive inspection on mobile and desktop
- **WHEN** an administrator opens catalog administration on mobile (390px) or desktop (1440px) in English and Vietnamese
- **THEN** labels, inputs, and action buttons remain legible without truncation, wrapping collisions, or overflow.
