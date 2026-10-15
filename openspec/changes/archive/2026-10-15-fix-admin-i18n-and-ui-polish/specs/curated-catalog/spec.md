# Spec Delta

## MODIFIED Requirements

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
