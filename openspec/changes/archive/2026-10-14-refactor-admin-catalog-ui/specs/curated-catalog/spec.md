# Curated Catalog Delta

## Purpose

Enable administrators to prepare, inspect, and publish curated technical literature through a modular, fully localized, and responsive administration workspace.

## ADDED Requirements

### Requirement: Modular catalog administration components
The catalog administration surface SHALL be partitioned into modular components with isolated presentation boundaries: header actions (`AdminCatalogHeader`), navigation rail and filters (`AdminCatalogNavRail`), draft ingestion form (`AdminCatalogDraftForm`), document inspection and metadata editor (`AdminCatalogDocumentDetail`), slice review controls (`AdminCatalogSlicesReview`), and slice preview modal (`AdminCatalogSliceModal`).

#### Scenario: Navigating catalog document selection
- **WHEN** an administrator selects a document from the navigation rail
- **THEN** the document detail view mounts with prepopulated metadata and slice review state without re-rendering unrelated rail items.

#### Scenario: Switching between draft creation and document inspection
- **WHEN** an administrator clicks New Draft or selects an existing document
- **THEN** the active view switches cleanly between the ingestion form and the document inspection workspace while preserving filter and search criteria.

### Requirement: Complete bilingual localization of catalog administration
All user-visible text within the catalog administration interface SHALL use internationalization tokens (`$t` and `t`) resolved against English and Vietnamese locale dictionaries. This includes form labels, placeholders, dropdown filter options, slice review summaries, modal content, and all system toast notifications.

#### Scenario: Administrator views catalog in Vietnamese
- **WHEN** an administrator switches language to Vietnamese (`vi`)
- **THEN** all form labels, placeholders, filter options, badges, and feedback toasts render in Vietnamese without English fallbacks or untranslated tokens.

#### Scenario: Administrator triggers validation or async action
- **WHEN** an action triggers a toast notification (such as missing title, upload error, metadata save, or publication success)
- **THEN** the toast message renders in the user's selected language.

### Requirement: Responsive typography and layout collision prevention
Catalog administration views SHALL adhere to DeepPace typography standards. Body text, form input values, and descriptive summaries SHALL be at least `text-sm` (14px) on mobile and `text-base` (16px) on desktop/tablet viewports. Micro-text (`text-xs` / 12px) SHALL be restricted strictly to compact status pills and timestamps. All buttons, badges, and action bars SHALL enforce `whitespace-nowrap shrink-0` and responsive gap spacing.

#### Scenario: Responsive inspection on mobile and desktop
- **WHEN** an administrator opens catalog administration on mobile (390px) or desktop (1440px) in English and Vietnamese
- **THEN** labels, inputs, and action buttons remain legible without truncation, wrapping collisions, or overflow.
