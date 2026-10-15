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


#### Scenario: Web draft creation serializes contract payload
- **WHEN** an administrator submits a web source draft in the catalog administration form
- **THEN** the client serializes `url` conforming to the backend `AdminImportCatalogUrlApiRequest` schema with `authorOrSourceUrl` mapped to optional attribution
- **AND** the backend accepts the request with HTTP 202 without validation errors.

#### Scenario: Web draft creation without explicit prior extraction preview
- **WHEN** an administrator enters a URL in the web URL input field and clicks create draft without executing preview extraction first
- **THEN** the form resolves the target URL from the primary input field and submits successfully without false source URL required errors.
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
1. **Preparation Action Gating**:
- The "Prepare Initial Slices" action SHALL ONLY be presented to the administrator when a document has zero extracted slices or when processing is in an unready/pending state (`document.slices.length === 0 && document.processingStatus !== 'Ready'`).
- When a document is already published (`visibility === 'Published'`) or has its initial slices extracted and prepared, the interface SHALL NOT display the "Prepare Initial Slices" button.
- When processing has failed (`processingStatus === 'Failed'`) on a PDF document, the interface SHALL provide a dedicated replacement PDF file input before triggering retry, preventing `Catalog.MissingPdfFile` errors due to expired temporary upload files.

#### Scenario: Preparation action hidden for already prepared or published documents
- **WHEN** an administrator views a document in Catalog Admin that is already published and has 10 extracted slices
- **THEN** the "Prepare Initial Slices" action button is omitted from the slices review panel
- **AND** the panel displays a clean summary stating that all slices are prepared and ready for learners.

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

#### Scenario: Readable slices need initial formatting only
- **WHEN** a catalog entry already has readable source slices but its required initial formatting is incomplete
- **THEN** the existing explicit catalog preparation action remains available under catalog permission and publication safeguards
- **AND** the document-operations workspace does not expose this as a standalone AI, embedding or audio retry.

#### Scenario: Published catalog entry targeted for source retry
- **WHEN** a source retry targets a published entry
- **THEN** the server returns a conflict without modifying source, slices or visibility
- **AND** catalog changes still require hiding and explicit review before republication.
---

### Requirement: Reviewed publication lifecycle
Catalog entries SHALL support Draft, Published and Hidden visibility. Publication SHALL require prepared readable slices and basic attribution. Publication SHALL NOT require a separate prerequisite administrator review record (`ReviewedByUserId`) or permission compliance proofs; an administrator's direct publish action SHALL constitute explicit review and authorization. Missing requirements SHALL block publication with actionable errors.
1. **Published Document Immutability & UI Controls**:
- Documents in `Published` visibility SHALL be treated as immutable in the administration interface.
- When `visibility === 'Published'`, the document metadata fields (title, category, language, attribution, source URL) and the "Save Changes" action SHALL be disabled / read-only, accompanied by an explanatory notice indicating that the document must be hidden before editing metadata.
- When `visibility === 'Published'`, the action bar SHALL present the "Hide from Discover" button and SHALL NOT display an active "Publish to Discover" button.
- When `visibility === 'Draft'` or `visibility === 'Hidden'`, the action bar SHALL display the "Publish to Discover" button (enabled only when publication requirements are satisfied).

#### Scenario: Viewing a published document in catalog administration
- **WHEN** an administrator inspects a document with `visibility === 'Published'`
- **THEN** metadata input fields and the "Save Changes" button are disabled
- **AND** the action bar displays only the "Hide from Discover" button without showing a duplicate "Publish to Discover" button.

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
4. `AdminCatalogDocumentDetail`: Document inspection and direct publish actions without redundant review buttons. Processing status badges (`document.processingStatus`) in the document detail header SHALL render as clean, uniform status pills without leading bullet dot markers (`w-2 h-2 rounded-full`), presenting text-only status badges consistent with visibility and revision pills.
5. `AdminCatalogSlicesReview`: Slice status listing without redundant review confirmation triggers.
6. `AdminCatalogPublishBar`: Contextual action bar enforcing published document immutability.

#### Scenario: Navigating catalog document selection
- **WHEN** an administrator selects a document from the navigation rail
- **THEN** the document detail view mounts with prepopulated metadata and slice review state without re-rendering unrelated rail items.

#### Scenario: Switching between draft creation and document inspection
- **WHEN** an administrator clicks New Draft or selects an existing document
- **THEN** the active view switches cleanly between the ingestion form and the document inspection workspace while preserving filter and search criteria.

#### Scenario: Nav rail renders filters without text clipping
- **WHEN** the catalog administration page is viewed in Vietnamese
- **THEN** the filter select controls display full text ("Tất cả hiển thị", "Tất cả trạng thái") without ellipsis truncation on desktop viewports.

#### Scenario: Clean status pill presentation on document detail header
- **WHEN** an administrator views any catalog document detail in the administration workspace
- **THEN** the processing status badge displays the localized status label (e.g. "Sẵn sàng" / "Ready", "Thất bại" / "Failed", "Đang xử lý" / "Processing")
- **AND** the badge does not render a leading bullet dot or pulse indicator dot.

---

### Requirement: Complete bilingual localization of catalog administration
All user-visible text within the catalog administration interface SHALL use internationalization tokens (`$t` and `t`) resolved against English and Vietnamese locale dictionaries. This includes form labels, placeholders, dropdown filter options, slice review summaries, modal content, and all system toast notifications.
1. **Catalog API Error Localization**:
- All RFC 7807 problem details and domain error codes returned by the Catalog API—including `Catalog.MissingPdfFile`, `Catalog.PublishedImmutable`, `Catalog.NotFound`, `Catalog.Busy`, `Catalog.ConcurrencyConflict`, `Catalog.PublicationIneligible`, `Catalog.PublicationFailed`, and `Catalog.InvalidOperation`—SHALL be translated into natural language through `api_errors` mappings in both `vi.json` and `en.json`.
- The client error formatter (`useApiError.ts`) SHALL parse dotted error codes to resolve corresponding locale translations, preventing raw HTTP status phrases (e.g. `Bad Request: ...`) from leaking into user toasts.
2. **Category Enum Normalization**:
- Category values returned as string enum names (`"SystemDesign"`, `"DatabaseStorage"`, etc.) SHALL be normalized to corresponding category options in `AppSelect`, ensuring the localized category label is displayed rather than falling back to an untranslated placeholder (`"Select option..."`).
3. **AppSelect Default Placeholder Localization**:
- The default fallback placeholder for `AppSelect.vue` SHALL resolve to localized text (`"Chọn một mục..."` in Vietnamese, `"Select an option..."` in English).

#### Scenario: Administrator views catalog in Vietnamese
- **WHEN** an administrator switches language to Vietnamese (`vi`)
- **THEN** all form labels, placeholders, filter options, badges, and feedback toasts render in Vietnamese without English fallbacks or untranslated tokens.

#### Scenario: Administrator triggers validation or async action
- **WHEN** an action triggers a toast notification (such as missing title, upload error, metadata save, or publication success)
- **THEN** the toast message renders in the user's selected language.

#### Scenario: Backend returns Catalog.MissingPdfFile error
- **WHEN** a catalog operation fails due to missing temporary PDF file
- **THEN** the resulting toast displays the localized message in Vietnamese (`"Tệp tải lên tạm thời không còn khả dụng. Vui lòng tải lại tệp PDF để chuẩn bị."`) instead of a raw English Bad Request string.
---

### Requirement: Responsive typography and layout collision prevention
Catalog administration views SHALL adhere to DeepPace typography standards. Body text, form input values, and descriptive summaries SHALL be at least `text-sm` (14px) on mobile and `text-base` (16px) on desktop/tablet viewports. Micro-text (`text-xs` / 12px) SHALL be restricted strictly to compact status pills and timestamps. All buttons, badges, and action bars SHALL enforce `whitespace-nowrap shrink-0` and responsive gap spacing.

#### Scenario: Responsive inspection on mobile and desktop
- **WHEN** an administrator opens catalog administration on mobile (390px) or desktop (1440px) in English and Vietnamese
- **THEN** labels, inputs, and action buttons remain legible without truncation, wrapping collisions, or overflow.

---

### Requirement: Validated catalog source replacement
Catalog administrators SHALL replace missing or expired input for eligible Failed or Interrupted, nonpublished catalog source ingestion through validated source-specific APIs. Replacement SHALL require the current control version and replay protection, preserve target identity, metadata, language and attribution, enforce shared quotas, and record administrative acceptance. Client-supplied backend paths SHALL be rejected.

#### Scenario: Replace missing catalog PDF
- **WHEN** an authorized catalog administrator confirms a validated replacement PDF for an eligible draft
- **THEN** one durable recovery attempt is accepted under the existing catalog identifier
- **AND** adopted personal copies remain unchanged.

#### Scenario: Replace web snapshot
- **WHEN** an administrator explicitly confirms accepted replacement content for an eligible web-source failure
- **THEN** URL validation, sanitization, source-language and bounded-storage rules apply
- **AND** opening a preview alone starts no recovery.

#### Scenario: Legacy temporary-path input
- **WHEN** a client supplies `ReplacementTempFilePath` or another backend path to preparation or source replacement
- **THEN** the API returns 400 with safe migration guidance and reads no caller-selected server file
- **AND** the supported replacement flow accepts validated upload/import input rather than a filesystem location.

#### Scenario: Denied or conflicting replacement
- **WHEN** a learner, stale command or published/active target requests catalog source replacement
- **THEN** the request is rejected without source acceptance, mutation or worker dispatch.

---

### Requirement: Catalog ingestion history integration
Catalog draft admission and source recovery SHALL participate in persisted ingestion history and the existing administrative audit contract. Queued acceptance SHALL remain distinct from terminal processing outcome. Catalogue editing and publication SHALL continue to use their own authoritative concurrency and readiness checks, and source recovery SHALL never republish a document automatically.

#### Scenario: Catalog URL fails after admission
- **WHEN** an explicitly admitted catalog URL import fails during fetch or source slicing
- **THEN** its draft and safe failed attempt remain visible for recovery
- **AND** no publishable or learner-discoverable entry is fabricated.

#### Scenario: Source recovery succeeds
- **WHEN** a catalog source recovery reaches successful processing
- **THEN** its attempt and linked administrative completion are recorded
- **AND** visibility remains nonpublished until an authorized administrator explicitly publishes it.
