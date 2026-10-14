# Curated Catalog Delta

## Purpose

Refine the curated document discovery interface with quick category pills, advanced modal filters, direct reader navigation for adopted documents, and single-scroll preview modal layout.

## ADDED Requirements

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
