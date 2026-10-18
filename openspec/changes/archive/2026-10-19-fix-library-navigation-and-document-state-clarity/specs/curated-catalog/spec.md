# Spec Delta

## MODIFIED Requirements

### Requirement: Discover tab advanced filtering and quick categories
Discovery SHALL provide search, an All pill, and a bounded quick subset chosen from shared category ordering, not named subject constants. The accessible Advanced Filter modal SHALL expose all nondeleted categories, including inactive categories, plus source type and language criteria, active filter counts, and clear/reset actions. All category queries SHALL send `categoryId` UUIDs. Committed criteria SHALL follow the library navigation contract without retaining invisible cached filters.

#### Scenario: Quick category filtering
- **WHEN** a learner clicks a quick category pill on the discovery tab
- **THEN** the catalog list updates immediately with documents matching that category UUID
- **AND** the active pill is highlighted with zero layout shifting.

#### Scenario: Opening advanced filter modal
- **WHEN** a learner clicks the advanced filter button
- **THEN** a dedicated modal displays all available categories, source types (Web Docs, PDF Books, Markdown), and languages (English, Vietnamese)
- **AND** the learner can select multiple criteria, reset filters, or apply the selection.

#### Scenario: Active filter badge count
- **WHEN** one or more non-default filters are active in discovery
- **THEN** the advanced filter button displays the count of active criteria.

#### Scenario: New category outside quick filters
- **WHEN** an administrator creates a subject outside the bounded quick subset
- **THEN** it is reachable from the advanced filter without a frontend deployment
- **AND** renamed or inactive category filters retain their UUID identity.

#### Scenario: Clear a previously applied search
- **WHEN** a learner searches for a keyword and then activates the search clear control
- **THEN** the displayed input, URL, and subsequent catalog request no longer contain that keyword
- **AND** current category, language and source-type criteria remain applied on page one.

#### Scenario: Reset all discovery criteria
- **WHEN** a learner has committed search, category, language and source-type criteria and activates Reset
- **THEN** every criterion is cleared from controls, active-filter count, URL and the subsequent request
- **AND** the first published-results page is requested without inherited store filters.

#### Scenario: Select All without resetting unrelated criteria
- **WHEN** a learner selects the All category pill while search, language or source type is applied
- **THEN** only category is removed and pagination returns to one
- **AND** the other committed criteria remain visible and continue to match the outgoing request.

#### Scenario: Search and filters remain reachable with long labels
- **WHEN** Discover is viewed in English or Vietnamese at 1440x900 or 390x844 with categories outside the quick subset
- **THEN** the search field, clear control and Advanced Filter remain usable without being pushed beyond the page or reduced to an icon-only field
- **AND** all categories remain reachable through advanced filtering without changing their identity.
