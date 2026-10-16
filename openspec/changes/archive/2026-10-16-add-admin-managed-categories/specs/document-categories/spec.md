# Spec Delta

## Purpose

Provide a shared, administrator-managed taxonomy for user-supplied learning documents without imposing a fixed curriculum or changing source content.

## ADDED Requirements

### Requirement: Stable bilingual category identity
Each category SHALL have a stable UUID, immutable slug, required English and Vietnamese names, display order, active status, and version. Names SHALL be trimmed and contain 1–100 characters. Slugs SHALL contain 1–100 lowercase ASCII letters, digits, and single separating hyphens and be unique among nondeleted categories. Display order SHALL be an integer from 0 through 10000. Names SHALL be rendered as text, never executable markup.

#### Scenario: Administrator introduces a subject outside the original taxonomy
- **WHEN** an authorized administrator creates an active category with valid names, slug, and display order
- **THEN** it receives a new UUID and version and becomes available for assignment without a deployment
- **AND** changing either name later retains its UUID, slug, existing assignments, and learning history.

#### Scenario: Invalid or conflicting metadata
- **WHEN** a name is blank or too long, the slug is invalid, or the display order is outside its bounds
- **THEN** creation or update returns HTTP 400 with field-level problem details and persists nothing
- **WHEN** a valid slug already belongs to another nondeleted category
- **THEN** creation returns HTTP 409 with code `Category.SlugConflict`.

#### Scenario: Immutable identity
- **WHEN** an update attempts to change a category UUID or slug
- **THEN** it returns HTTP 400 without changing category metadata or references.

### Requirement: Shared authenticated category metadata
Authenticated clients SHALL retrieve nondeleted category descriptors containing `id`, `slug`, `nameEn`, `nameVi`, `sortOrder`, `isActive`, and `version` from `GET /api/v1/categories`. Results SHALL be ordered by display order and then slug and SHALL include inactive categories for historical display and filtering. This endpoint SHALL disclose no administrator audit data, user identifiers, or private document counts.

#### Scenario: Metadata supports both locales and historical references
- **WHEN** an authenticated learner fetches category metadata
- **THEN** active and inactive descriptors are returned with both names and stable IDs
- **AND** switching UI locale selects the corresponding stored name without a category-name translation dictionary.

#### Scenario: Anonymous metadata access
- **WHEN** a client requests category metadata without a valid session
- **THEN** the server returns HTTP 401.

#### Scenario: Metadata cannot be loaded
- **WHEN** fetching metadata fails
- **THEN** clients display a localized retryable error and preserve entered form data
- **AND** they do not substitute a bundled enum, guessed category, or fabricated selection.

### Requirement: Permission-gated category administration
Category administration SHALL require current database-resolved ManageCategories and expose canManageCategories without changing other capability meanings. The RBAC prerequisite SHALL supply authority; protected Admin SHALL receive an explicit grant and other roles SHALL not gain it implicitly. Admin endpoints SHALL return 401 for invalid authentication, 403 for verified missing permission and 503 for unavailable authority before protected work.

#### Scenario: UI visibility does not authorize an API request
- **WHEN** an authenticated learner or administrator without category permission calls any category administration endpoint directly
- **THEN** the request returns HTTP 403 without category mutation or restricted usage data.

#### Scenario: Authorization dependency is unavailable
- **WHEN** category permission cannot be resolved reliably
- **THEN** administrative requests fail closed with a structured service-unavailable response
- **AND** no default administrator or development bypass is used.

#### Scenario: Role grants remain independent
- **WHEN** an account has catalog, account or role-management permission but lacks ManageCategories
- **THEN** category administration remains forbidden
- **AND** a category-only role can enter Categories without receiving dashboard, catalog, account, role-assignment or audit access.

### Requirement: Versioned category lifecycle
Authorized administrators SHALL create categories, update names/order/status, and soft-delete unused categories through versioned operations. Updates and deletion SHALL require the last observed version; stale versions SHALL return HTTP 409 `Category.ConcurrencyConflict`. A successful mutation SHALL return the new descriptor or deletion confirmation and commit exactly one corresponding administrative audit event atomically.

#### Scenario: Competing administrator edits
- **WHEN** two administrators submit different updates using the same version
- **THEN** at most one commits and the other receives a concurrency conflict with no lost update
- **AND** the UI preserves the rejected draft and offers to reload current metadata before resubmitting.

#### Scenario: Rename and reorder have no content side effects
- **WHEN** an administrator changes category names or order
- **THEN** only category metadata, its version, and its audit event change
- **AND** document identifiers, source prose, publication revisions, ingestion state, audio, and learning progress remain unchanged
- **AND** no AI request or regeneration is queued.

#### Scenario: Audit persistence fails
- **WHEN** a category mutation cannot commit its audit event
- **THEN** the entire mutation rolls back and returns HTTP 503 rather than reporting success.

### Requirement: Single explicit assignment and inactive retention
Each document SHALL reference exactly one category UUID. New documents, catalog adoptions, reclassification, and standalone category-selected learning generation SHALL require a nondeleted active category. Existing assignments SHALL remain readable, searchable, and usable for source-grounded practice when inactive. Editing unrelated fields SHALL permit retaining the same inactive assignment. Admission and deactivation SHALL have a deterministic atomic order.

#### Scenario: No implicit category on import
- **WHEN** a new document is submitted without a category, with an invalid UUID, or with an unknown or deleted category
- **THEN** it returns HTTP 400 without a document or ingestion job
- **AND** title, URL, or excerpt keywords do not silently select a replacement.

#### Scenario: Deactivation before assignment admission
- **WHEN** deactivation commits before a request admits a new assignment
- **THEN** the request returns HTTP 409 `Category.Inactive` and creates no assignment
- **WHEN** assignment admission commits first
- **THEN** it remains valid after deactivation and already-admitted ingestion can finish.

#### Scenario: Existing inactive assignment remains usable
- **WHEN** a user reads, reviews, explicitly curates, or changes the title of an existing document whose category is inactive
- **THEN** its category remains attached and its existing workflows remain available
- **AND** assigning that category to a different document is rejected until it is reactivated.

#### Scenario: No active categories
- **WHEN** all categories are inactive or none exist
- **THEN** assignment forms show a localized unavailable state and disable submission
- **AND** administrators can restore availability by creating or activating a category without rebuilding the application.

#### Scenario: Selected category becomes inactive while a form is open
- **WHEN** a new-assignment submission is rejected after its selected category becomes inactive
- **THEN** all source inputs and category selection remain available with announced feedback and refreshed choices
- **AND** an active replacement requires explicit selection rather than silently choosing the first option.

### Requirement: Reference-safe deletion
Deletion SHALL soft-delete only categories with no persisted taxonomy references, including soft-deleted content and historical learning artifacts. A referenced category SHALL return HTTP 409 `Category.InUse` with a deactivate-instead message. Deletion SHALL never cascade, reassign documents, or erase history. Concurrent deletion and assignment SHALL not create deleted-category references. Free-text explanation context and append-only audit identifiers SHALL not count as taxonomy foreign keys.

#### Scenario: Existing document prevents deletion
- **WHEN** an administrator deletes a category referenced by a personal document, catalog entry, question, or insight
- **THEN** deletion is rejected even when that reference is soft-deleted or hidden
- **AND** the administrator can deactivate the category instead.

#### Scenario: Unused category is deleted
- **WHEN** an administrator deletes an unreferenced category using its current version
- **THEN** the category is soft-deleted and disappears from shared metadata and assignment choices
- **AND** its UUID is never reused.

### Requirement: Stable-ID category contract cutover
All category-bearing requests, queries, responses, graph identities, and retained client state SHALL use UUID category references rather than enum names or ordinals. Responses SHALL carry the authoritative category descriptor where a labeled reference is needed. The cutover SHALL migrate the original seven categories and documented legacy aliases without changing content identity or progress; unmapped stored values SHALL stop migration rather than default to another category.

#### Scenario: Known legacy values migrate losslessly
- **WHEN** the migration processes the original categories and the `BackendDotNet` or `DotNet` aliases
- **THEN** every reference resolves to the intended stable category, with both aliases mapped to Backend Runtime
- **AND** document/card identifiers, prose, ownership, publication state, and learning progress are unchanged.

#### Scenario: Unknown legacy category
- **WHEN** a persisted category value cannot be mapped explicitly
- **THEN** preflight reports the affected storage location and value and the destructive cutover does not proceed
- **AND** no fallback assigns it to Backend Runtime or another category.

#### Scenario: Cutover schema is not ready
- **WHEN** category preflight or migration fails, or the required category schema is absent
- **THEN** the API and workers remain unready and admit no UUID-category work
- **AND** a caught migration warning cannot permit consumers to run against enum storage.

#### Scenario: Obsolete client filter state
- **WHEN** a client encounters a stored numeric or enum-name category filter after cutover
- **THEN** it clears only that obsolete filter and preserves unrelated client preferences
- **AND** requests do not send legacy `category` fields or numeric aliases.

### Requirement: Accessible category management workspace
The Categories workspace SHALL support English/Vietnamese list, search, active-status filtering, create/edit, activation/deactivation, and confirmed safe deletion. It SHALL use the existing master-detail layout and accessible custom controls/dialogs, preserve drafts on failure, expose loading/empty/forbidden/conflict states, and remain usable by keyboard and at desktop and 390px mobile widths.

#### Scenario: Category detail editing
- **WHEN** an authorized administrator selects a category
- **THEN** both names, immutable slug, order, status, version, and permitted lifecycle actions are visible
- **AND** referenced categories explain why deletion is unavailable without exposing private document content.

#### Scenario: Lifecycle actions require deliberate interaction
- **WHEN** an administrator deactivates or deletes a category
- **THEN** a localized custom confirmation explains assignment and history effects
- **AND** cancel performs no mutation and successful completion refreshes shared metadata and affected labels.

#### Scenario: Bilingual visual verification
- **WHEN** category management and affected assignment/filter controls are inspected in English and Vietnamese at 1440x900 desktop and 390x844 mobile viewports
- **THEN** labels, errors, badges, and action rows remain legible without clipping or collisions
- **AND** body text is at least 14px on mobile and 16px on larger screens; actions, badges and footers remain nonwrapping with responsive gaps.

#### Scenario: Dialog and teleported list focus
- **WHEN** an administrator opens a category confirmation and its custom listbox using the keyboard
- **THEN** the named dialog contains focus including the listbox and background controls cannot receive focus
- **AND** the first Escape closes only the open list, a later Escape cancels the dialog, and cancellation returns focus to a surviving trigger.

#### Scenario: Lifecycle success removes the initiating row
- **WHEN** successful deletion or an active-status filter removes the initiating category row
- **THEN** focus moves to a surviving list item or the list heading rather than a detached trigger.

#### Scenario: Form and filter semantics are programmatic
- **WHEN** category controls are inspected with assistive technology
- **THEN** the actual combobox exposes its localized name, value, expanded/required/invalid state and associated feedback; chips expose selection and inactive categories announce status without disabling historical filters
- **AND** disabled assignment submission has adjacent explanatory text and an operable recovery action.

#### Scenario: Long duplicate category labels remain distinguishable
- **WHEN** distinct categories have identical localized names or 100-character/unbroken names at effective 320 CSS-pixel width
- **THEN** immutable slugs visibly and accessibly disambiguate them and full names are obtainable by keyboard or click, not hover alone
- **AND** surrounding controls reflow without clipped actions; long names use nonbadge wrapping text or bounded disclosed badges while badges/actions retain no-wrap behavior.

#### Scenario: Version conflict preserves a deliberate draft
- **WHEN** an update returns a version conflict
- **THEN** the draft remains, the conflict is announced and submission is gated until explicit refresh succeeds
- **AND** current values appear without overwriting inputs; deliberate reconfirmation uses the refreshed version and failed refresh leaves the draft recoverable.
