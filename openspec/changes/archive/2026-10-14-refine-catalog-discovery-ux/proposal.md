# Proposal

## Why

During manual end-to-end verification of the Curated Document Discovery feature, three distinct user experience flaws were identified:
1. The Discover tab category and metadata filters display as vertically stacked, unconstrained dropdowns showing unlocalized English placeholders ("Select option..."), creating an awkward, cluttered layout compared to the clean pill chips on "My Library".
2. The "Open Book" (`Mở tài liệu`) action on catalog cards and the preview modal routes back to `/library` (the current page) instead of navigating directly to `/read/:bookId`, frustrating learners who have already adopted a document.
3. The Document Preview modal suffers from nested dual scrollbars because the Table of Contents height pushes the modal body past the viewport threshold, even though the Table of Contents already has its own internal scrolling mechanism.

Addressing these issues polishes the discovery experience to match DeepPace's Senior UX and typography standards.

## What Changes

- **Discover Tab Filter Toolbar & Advanced Filter Modal**:
  - Replace the 3 stacked inline dropdowns in the Discover tab with a clean primary filter bar featuring popular category quick-filter pills (mirroring My Library's clean pill aesthetic) and an "Advanced Filters" (`Bộ lọc nâng cao`) modal trigger button.
  - Implement a dedicated `CatalogFilterModal` component (reusing the established `AdvancedFilterModal` design pattern from `/review`) allowing learners to filter by all categories, source types (Web, PDF, Markdown), and languages (English, Vietnamese) with clear active filter counters and reset/apply actions.
- **Direct Reader Navigation for Adopted Books**:
  - Update `CatalogCard` and `CatalogPreviewModal` so clicking "Open Book" (`Mở tài liệu`) routes directly to `/read/${document.existingBookId}` with navigation context, allowing learners to immediately begin deliberate practice.
- **Eliminate Dual Scrollbars in Preview Modal**:
  - Constrain the Table of Contents and First Slice preview scroll heights (`max-h-52`) and compact the summary header padding (`p-3`) in `CatalogPreviewModal`.
  - Ensure the modal content stays fully within viewport bounds (`< 60vh`), completely eliminating the outer modal scrollbar while preserving independent scrolling inside the Table of Contents.

## Capabilities

### Modified Capabilities
- `curated-catalog`: Refines discovery filter interaction, adopted document open action, and preview modal viewport constraints.

## Impact

- **Frontend**:
  - `frontend/app/pages/library.vue`: Refactor Discover tab `#filters` slot to use category pills and an Advanced Filter modal trigger.
  - `frontend/app/components/catalog/CatalogFilterModal.vue`: New accessible filter modal with category, source type, and language options.
  - `frontend/app/components/catalog/CatalogCard.vue`: Fix "Open Book" routing to `/read/:bookId`.
  - `frontend/app/components/catalog/CatalogPreviewModal.vue`: Fix "Open Book" routing and adjust inner heights to remove outer modal scrollbar.
  - `frontend/app/i18n/locales/{en,vi}.json`: Add localized strings for the catalog advanced filter modal and filter counts.
- **Testing**:
  - Vitest component and store tests for filter modal state and reader navigation.
  - Automated headless Chromium verification across Desktop (1440x900) and Mobile (390x844) viewports in both English and Vietnamese.
