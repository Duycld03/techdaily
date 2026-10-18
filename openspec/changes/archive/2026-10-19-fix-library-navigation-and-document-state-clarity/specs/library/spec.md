# Spec Delta

## MODIFIED Requirements

### Requirement: Category Filter Bar Layout Stability
The `/library` catalog page SHALL provide a unified, responsive search and filter toolbar layout synchronized across both the "My Library" and "Discover" tabs.

On both "My Library" and "Discover" tabs, the primary toolbar SHALL present:
1. A search input with a dedicated clear trigger (`X`) and search execution on Enter or clear. Category controls SHALL NOT compress the input into an icon-only or otherwise unusable field.
2. An All pill and a bounded quick subset selected by configured category order, not hardcoded subject names. Every category SHALL remain reachable through the advanced filter even when omitted from the quick subset.
3. An Advanced Filter trigger button displaying an active filter badge counter when any non-default filter is applied. The trigger SHALL remain reachable without horizontal page overflow.

Clicking the Advanced Filter trigger button SHALL open an accessible filter dialog modal permitting users to filter documents by all nondeleted categories from shared metadata, including inactive categories for historical filtering, and content language, with dedicated reset and apply actions. Discover SHALL additionally offer its existing document-format filter; My Library SHALL NOT display a format criterion that its personal-books query cannot apply.

Filter pills SHALL maintain `font-semibold` across active and inactive states with `transition-colors duration-150` to guarantee zero layout shift when switching categories.

#### Scenario: User filters library by Mental Models category
- **WHEN** the user selects the stored Mental Models category from the available category controls, opening the advanced filter when it is outside the quick subset
- **THEN** the book catalog updates to display only books and articles categorized under the selected Mental Models category UUID
- **AND** the filter button transitions active styling with zero horizontal layout shift or width jumping.

#### Scenario: User filters library by Habits & Deep Work category
- **WHEN** the user selects the stored Habits category from the available category controls, opening the advanced filter when it is outside the quick subset
- **THEN** the book catalog updates to display only books and articles categorized under the selected Habits category UUID.

#### Scenario: User switches category filter tab in library
- **WHEN** user clicks a category tab (e.g., "Tất Cả", "Frontend & Web", "Backend & Phân Tán", "Thiết Kế Mã", "Mô Hình Tư Duy")
- **THEN** the active tab updates its background, border, and text colors via `transition-colors`
- **AND** the button maintains its exact text width and baseline geometry without any border jitter, horizontal position jumping, or layout shifts across adjacent tabs.

#### Scenario: User clears or toggles category filter
- **WHEN** user clicks "Tất Cả" (All) after viewing a specific category
- **THEN** only the category filter is cleared, supported search and other filter criteria remain, and the active page resets to one
- **AND** the tab switches cleanly with smooth color transitions and zero width shift.

#### Scenario: User filters My Library with quick category chips
- **WHEN** user clicks "Backend & Runtime" in My Library
- **THEN** the book list updates to show only personal books under the selected Backend category UUID
- **AND** the button styling transitions with zero layout shift or width jumping.

#### Scenario: User opens advanced filter modal in My Library
- **WHEN** user clicks the "Bộ Lọc" button in My Library
- **THEN** the advanced filter modal opens, displaying all nondeleted categories and language options supported by the personal-books query
- **AND** the trigger button displays the active filter count badge when filters are applied.

#### Scenario: User applies deep category filter from modal in My Library
- **WHEN** the user selects the Engineering Craft or Mental Models category UUID from the advanced filter and clicks Apply
- **THEN** the modal closes, the catalog updates to display the matching personal books, and the filter counter badge increments.

#### Scenario: User clears or resets filters in My Library
- **WHEN** user clicks Reset in the filter modal
- **THEN** category, language, search, and pagination criteria are cleared and the book list reloads the first unfiltered personal page
- **AND** clicking "Tất Cả" instead clears only the category criterion without silently clearing search or language.

#### Scenario: New category outside the quick subset
- **WHEN** an administrator creates a category that is outside the toolbar's bounded quick subset
- **THEN** it appears in the advanced filter with its localized stored name and filtering sends its `categoryId` UUID.

#### Scenario: Bilingual toolbar remains usable as category labels grow
- **WHEN** either library tab is viewed in English or Vietnamese at 1440x900 and 390x844 with long category labels and more categories than the quick subset
- **THEN** search entry, its clear action, the tab controls, and Advanced Filter remain visible and operable without collisions or horizontal page overflow
- **AND** omitted categories remain available in the accessible advanced filter.

### Requirement: Library and discovery navigation
The library SHALL expose My Library and Discover tabs with URL-synchronized tab, committed search, supported filters and page state. My Library SHALL remain the default and return only the current user's books. Discovery SHALL use a separate catalog and SHALL NOT merge shared entries into personal results. The visible controls, selected page and outgoing query SHALL agree with the active URL, including after reload, history navigation or a query-driven page remount.

#### Scenario: Navigate and reload discovery
- **WHEN** a learner opens `/library?tab=discover`, applies filters and reloads or navigates back
- **THEN** the selected tab and query state are restored
- **AND** changing filters resets that tab's page to one without mixing results from the other tab.

#### Scenario: Adopt and open a document
- **WHEN** a learner adds a catalog document
- **THEN** My Library shows the owned ready copy and provides the existing reader entry point
- **AND** adoption alone does not advance reading progress or switch the active book pacer.

#### Scenario: Restore all supported criteria from a deep link
- **WHEN** a learner opens a library URL with search, category UUID, language, page, and a Discover source type when applicable
- **THEN** the active tab's controls and initial request restore every supported criterion
- **AND** a previously cached search or filter absent from that URL is not applied invisibly.

#### Scenario: Commit and clear search
- **WHEN** a learner presses Enter after editing search on either tab
- **THEN** the committed search is reflected in the URL and request and pagination resets to one.
- **WHEN** the learner clears search using its clear action
- **THEN** the input becomes empty and search is absent from the URL and outgoing request
- **AND** supported category, language and source-type filters are preserved.

#### Scenario: Change page and restore browser history
- **WHEN** a learner changes the active tab's page and later uses Back or Forward
- **THEN** the URL, selected page, filters and requested result page agree at each history destination
- **AND** pagination does not reload page one merely because the route query changed.

#### Scenario: Switch tabs without hidden filter state
- **WHEN** a learner switches between My Library and Discover
- **THEN** search, category and language explicitly present in the URL remain applied, the destination page resets to one, and requests use only the destination's data source
- **AND** opening My Library removes Discover-only source type from its URL, controls and request rather than retaining an invisible criterion.

#### Scenario: Category navigation does not erase committed search
- **WHEN** a learner searches and subsequently changes category, causing the library view to be recreated from its URL
- **THEN** the search input and request retain the same committed keyword shown in that URL
- **AND** no empty input conceals an active keyword in cached discovery state.

## ADDED Requirements

### Requirement: Personal library language filtering
`GET /api/v1/library/books` SHALL accept optional `language` with normalized values `en` or `vi`. It SHALL combine language with ownership, category and search before counting and pagination. Returned language labels and matching SHALL use the same effective document language. Omitting or clearing language SHALL remove that criterion; unsupported nonempty values SHALL return HTTP 400 problem details.

#### Scenario: Filter owned books and filtered page totals
- **WHEN** an authenticated learner selects Vietnamese and combines it with category or search criteria in My Library
- **THEN** the request sends `language=vi` and only that learner's matching books are returned
- **AND** total count and page count describe that filtered result, excluding other owners and nonmatching languages.

#### Scenario: Clear language and restore the complete matching set
- **WHEN** the learner changes language back to All or resets filters
- **THEN** language is removed from both the library URL and API request
- **AND** books matching the remaining criteria are returned regardless of content language.

#### Scenario: Consistent language for retained and incomplete books
- **WHEN** an owned book has declared content language or language retained on its slices
- **THEN** the language used to match the filter agrees with the returned language label, including when an incomplete import has no slices yet.

#### Scenario: Preserve a learner's edited language
- **WHEN** an owned book's slices carry the learner's updated language and its original language declaration differs
- **THEN** filtering and the returned label use the current slice language rather than reverting to the original declaration
- **AND** books without slices use their declared language when available.

#### Scenario: Invalid language query
- **WHEN** an authenticated client supplies an unsupported nonempty language value
- **THEN** the endpoint returns HTTP 400 problem details using the existing error-contract conventions rather than silently ignoring the criterion.
