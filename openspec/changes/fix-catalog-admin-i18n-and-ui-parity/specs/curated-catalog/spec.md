# Spec Delta

## MODIFIED Requirements

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

### Requirement: Reviewed publication lifecycle
Catalog entries SHALL support Draft, Published and Hidden visibility. Publication SHALL require prepared content, complete attribution, and source language. Missing requirements SHALL block publication with actionable errors.
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
- **WHEN** an administrator publishes an entry with missing permission information or incomplete initial slices
- **THEN** the system rejects publication and identifies the unmet requirements.

#### Scenario: Hide or edit a published entry
- **WHEN** an administrator hides a published entry
- **THEN** it disappears from learner listing, preview and new adoption
- **AND** existing personal copies remain usable.
- **WHEN** the administrator needs to change its source, content or metadata
- **THEN** the entry must be hidden first and reviewed again before republication.

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
