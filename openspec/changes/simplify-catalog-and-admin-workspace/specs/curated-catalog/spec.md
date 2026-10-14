# Spec Delta

## MODIFIED Requirements

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
