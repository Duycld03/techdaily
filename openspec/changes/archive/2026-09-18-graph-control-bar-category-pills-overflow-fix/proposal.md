## Why

In `frontend/components/graph/GraphControlBar.vue`, the Category Pillars filter row uses `flex items-center gap-1.5 overflow-x-auto pb-0.5 no-scrollbar` with `whitespace-nowrap shrink-0` buttons. In the Vietnamese locale (`vi-VN`), text strings are significantly longer (e.g. "TRỤ CỘT KỸ THUẬT:", "Tất Cả Trụ Cột", "Hệ Thống Backend & Runtime"), which pushes the 6th pill ("Engineering Craft") beyond the container boundary. Because `no-scrollbar` hides the scrollbar, the rightmost pill is clipped into `Engineeri...`, cutting off text and reducing discoverability.

Additionally, four category pills currently lack i18n keys (`key: ''` in `categoryPills`), causing raw English strings ("Database & Storage", "Distributed Systems", "Frontend & Web", "Engineering Craft") to render in Vietnamese mode. Adopting responsive `flex-wrap` and completing the translation keys resolves clipping and guarantees full bilingual parity.

## What Changes

- **Responsive Flex-Wrap Layout**: In `frontend/components/graph/GraphControlBar.vue`, replace `flex items-center gap-1.5 overflow-x-auto pb-0.5 no-scrollbar` with `flex flex-wrap items-center gap-1.5` for the Category Pillars, Node Types, and Mastery filter rows. Buttons maintain `whitespace-nowrap shrink-0`, allowing the collection of pills to flow gracefully into multiple lines when horizontal space is constrained rather than being clipped.
- **Complete Category Translation Keys**: Supply explicit translation keys in `categoryPills` for all 5 canonical engineering pillars:
  - `DatabaseStorage` $\rightarrow$ `graph.filters.databaseStorage`
  - `SystemDesign` $\rightarrow$ `graph.filters.systemDesign`
  - `FrontendWeb` $\rightarrow$ `graph.filters.frontendWeb`
  - `EngineeringCraft` $\rightarrow$ `graph.filters.engineeringCraft`
- **Concise Vietnamese Localization**: Optimize filter labels in `frontend/i18n/locales/vi.json` for concise senior developer terminology (e.g. `"allPillars": "Tất Cả"`, `"backendRuntime": "Backend & Runtime"`), saving ~15 characters of horizontal footprint while improving aesthetic balance.
- **Translation Parity in `en.json`**: Ensure all corresponding keys exist in English locale (`en.json`).

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `knowledge-graph`: Update `Requirement: Multi-Dimensional Graph Filtering & Live Search` to mandate responsive wrapping and complete bilingual localization for all category filter pills without horizontal truncation or hidden scrollbar clipping.

## Impact

- **Frontend Components**: `frontend/components/graph/GraphControlBar.vue`
- **Localization Files**: `frontend/i18n/locales/en.json` and `frontend/i18n/locales/vi.json`
- **Testing**: `frontend/tests/components/graph/GraphControlBar.spec.ts`
- **Backend / Database**: Zero impact.
