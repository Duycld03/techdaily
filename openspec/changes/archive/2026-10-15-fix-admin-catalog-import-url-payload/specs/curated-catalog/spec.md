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
