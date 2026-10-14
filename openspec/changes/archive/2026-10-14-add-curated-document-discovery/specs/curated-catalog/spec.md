# Curated Catalog Delta

## Purpose

Enable administrators to prepare and publish selected documents that authenticated learners can preview and explicitly add to their personal libraries.

## ADDED Requirements

### Requirement: Administrator draft ingestion
Administrators SHALL create catalog drafts from uploaded PDFs or web URLs with title, description, category, source language, source attribution and reuse-permission information. Draft processing SHALL preserve source language and use existing upload, extraction, URL validation and sanitization boundaries.

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

### Requirement: Reviewed publication lifecycle
Catalog entries SHALL support Draft, Published and Hidden visibility. Publication SHALL require prepared content, complete attribution, permission information covering distribution and AI processing, and explicit administrator review. Missing requirements SHALL block publication with actionable errors.

#### Scenario: Publish a prepared draft
- **WHEN** an administrator confirms review and publishes a ready draft with complete metadata
- **THEN** it becomes discoverable and the system records the publishing administrator and time.

#### Scenario: Publish an incomplete draft
- **WHEN** an administrator publishes an entry with missing permission information or incomplete initial slices
- **THEN** the system rejects publication and identifies the unmet requirements.

#### Scenario: Hide or edit a published entry
- **WHEN** an administrator hides a published entry
- **THEN** it disappears from learner listing, preview and new adoption
- **AND** existing personal copies remain usable.
- **WHEN** the administrator needs to change its source, content or metadata
- **THEN** the entry must be hidden first and reviewed again before republication.

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

### Requirement: Personal snapshot independence
Adopted content SHALL remain independent of administrator and other learner activity. Adoption SHALL copy document content and prepared learning content only, preserve attribution and source language, and never copy anyone's highlights, notes, drills, review cards, quiz progress, pacers or audio usage records.

#### Scenario: Two learners adopt one entry
- **WHEN** one learner edits or deletes their book or creates notes and flashcards
- **THEN** the other learner's content and learning state and the catalog entry are unchanged.

#### Scenario: Administrator republishes a changed document
- **WHEN** an administrator updates a hidden entry and republishes it
- **THEN** existing personal copies are unchanged
- **AND** subsequent first-time adoptions receive the currently published content.

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
